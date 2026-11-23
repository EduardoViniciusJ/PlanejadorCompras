using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UseCases.ShoppingList;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;
using PlanejadorCompras.Domain.Repositories.ShoppingListSupplier;
using PlanejadorCompras.Domain.Repositories.Supplier;
using PlanejadorCompras.Domain.Rules;
using QuotationRequestEntity = PlanejadorCompras.Domain.Entities.QuotationRequest;
using ShoppingListSupplierEntity = PlanejadorCompras.Domain.Entities.ShoppingListSupplier;
using SupplierEntity = PlanejadorCompras.Domain.Entities.Supplier;

namespace PlanejadorCompras.Application.UseCases.QuotationRequest;

public sealed class CreateQuotationRequestUseCase(
    GetShoppingListDetailUseCase getShoppingListDetailUseCase,
    IQuotationRequestRepository repository,
    ISupplierRepository supplierRepository,
    IShoppingListSupplierRepository shoppingListSupplierRepository,
    IFrontendUrlService frontendUrlService,
    SupplierQuotationEmailSender emailSender,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<QuotationRequestDetailResponseDto> ExecuteAsync(
        Guid shoppingListId,
        QuotationRequestPdfRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(shoppingListId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(request);

        var detail = await getShoppingListDetailUseCase.ExecuteAsync(
            shoppingListId,
            cancellationToken);

        if (detail.Items.Count == 0)
        {
            throw new BadRequestException(
                "Adicione ao menos um item antes de emitir a solicitacao de cotacao.",
                "quotation_request_empty_list");
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(nowUtc);
        if (request.ResponseDeadline.HasValue && request.ResponseDeadline.Value < today)
        {
            throw new BadRequestException(
                "O prazo de resposta nao pode estar no passado.",
                "quotation_request_invalid_deadline");
        }

        var suppliers = await LoadSuppliersAsync(request, cancellationToken);
        if (request.SendEmail && !emailSender.IsAvailable)
        {
            throw new BadRequestException(
                "O envio por e-mail nao esta configurado neste ambiente.",
                "email_delivery_unavailable");
        }

        var buyerName = string.IsNullOrWhiteSpace(currentUser.Name)
            ? currentUser.Email
            : currentUser.Name;
        var entity = QuotationRequestEntity.Create(
            currentUser.UserId,
            shoppingListId,
            detail.Name,
            detail.Description,
            buyerName,
            currentUser.Email,
            request.ResponseDeadline,
            request.DeliveryAddress,
            request.Instructions,
            detail.Items
                .Select(item => new QuotationRequestItemSnapshot(
                    item.Id,
                    item.Name,
                    item.Quantity,
                    item.Unit))
                .ToList(),
            nowUtc);

        // The raw token only lives in this request: the database keeps its hash.
        var linkUrls = new Dictionary<Guid, string>();
        foreach (var supplier in suppliers)
        {
            var (token, hash) = QuotationLinkToken.Create();
            var quotation = entity.AddSupplierQuotation(
                new SupplierQuotationRecipient(
                    supplier.Id,
                    supplier.Name,
                    supplier.Contact?.Email,
                    supplier.Contact?.Phone,
                    hash),
                nowUtc);
            linkUrls[quotation.Id] = frontendUrlService.BuildSupplierQuotationUrl(token);

            if (!await shoppingListSupplierRepository.ExistsAsync(
                    shoppingListId,
                    supplier.Id,
                    cancellationToken))
            {
                await shoppingListSupplierRepository.AddAsync(
                    ShoppingListSupplierEntity.Create(shoppingListId, supplier.Id),
                    cancellationToken);
            }
        }

        await repository.AddAsync(entity, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        var emailDeliveries = new Dictionary<Guid, string>();
        if (request.SendEmail)
        {
            foreach (var quotation in entity.SupplierQuotations)
            {
                emailDeliveries[quotation.Id] = await emailSender.SendAsync(
                    entity,
                    quotation,
                    linkUrls[quotation.Id],
                    nowUtc,
                    cancellationToken);
            }

            if (emailDeliveries.ContainsValue(SupplierQuotationEmailSender.Sent))
            {
                await unitOfWork.CommitAsync(cancellationToken);
            }
        }

        return QuotationRequestResponseMapper.ToDetail(
            entity,
            nowUtc,
            quotation => QuotationRequestResponseMapper.ToSupplier(
                quotation,
                nowUtc,
                linkUrls[quotation.Id],
                emailDeliveries.GetValueOrDefault(quotation.Id)));
    }

    private async Task<List<SupplierEntity>> LoadSuppliersAsync(
        QuotationRequestPdfRequestDto request,
        CancellationToken cancellationToken)
    {
        var supplierIds = (request.SupplierIds ?? []).Distinct().ToList();
        if (supplierIds.Count > SupplierQuotationRules.MaximumSuppliersPerRequest)
        {
            throw new BadRequestException(
                "A solicitacao aceita no maximo "
                + $"{SupplierQuotationRules.MaximumSuppliersPerRequest} fornecedores.",
                "quotation_request_too_many_suppliers");
        }

        var suppliers = new List<SupplierEntity>(supplierIds.Count);
        foreach (var supplierId in supplierIds)
        {
            var supplier = await supplierRepository.GetByIdAsync(supplierId, cancellationToken);
            if (supplier is null || supplier.UserId != currentUser.UserId)
            {
                throw new NotFoundException("Supplier not found.", "supplier_not_found");
            }

            suppliers.Add(supplier);
        }

        return suppliers;
    }
}
