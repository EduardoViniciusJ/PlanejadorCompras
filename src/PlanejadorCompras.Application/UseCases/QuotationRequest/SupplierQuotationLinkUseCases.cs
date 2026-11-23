using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;
using PlanejadorCompras.Domain.Repositories.Supplier;
using QuotationRequestEntity = PlanejadorCompras.Domain.Entities.QuotationRequest;

namespace PlanejadorCompras.Application.UseCases.QuotationRequest;

/// <summary>
/// Issues a new public link for one supplier (the previous one stops working) and can
/// send it by e-mail. It is the only way to get the address again after the creation.
/// </summary>
public sealed class RotateSupplierQuotationLinkUseCase(
    IQuotationRequestRepository repository,
    ISupplierRepository supplierRepository,
    IFrontendUrlService frontendUrlService,
    SupplierQuotationEmailSender emailSender,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SupplierQuotationResponseDto> ExecuteAsync(
        Guid quotationRequestId,
        Guid supplierQuotationId,
        SupplierQuotationLinkRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (quotationRequest, quotation) = await SupplierQuotationLookup.FindAsync(
            repository,
            currentUser,
            quotationRequestId,
            supplierQuotationId,
            cancellationToken);

        // The contact may have been completed after the request was issued.
        var supplier = await supplierRepository.GetByIdAsync(quotation.SupplierId, cancellationToken);
        if (supplier is not null && supplier.UserId == currentUser.UserId)
        {
            quotation.UpdateContact(supplier.Contact?.Email, supplier.Contact?.Phone);
        }

        if (request.SendEmail)
        {
            if (!emailSender.IsAvailable)
            {
                throw new BadRequestException(
                    "O envio por e-mail nao esta configurado neste ambiente.",
                    "email_delivery_unavailable");
            }

            if (string.IsNullOrWhiteSpace(quotation.SupplierEmail))
            {
                throw new BadRequestException(
                    "O fornecedor nao tem e-mail cadastrado.",
                    "supplier_without_email");
            }
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var (token, hash) = QuotationLinkToken.Create();
        quotation.RotateToken(hash, quotationRequest.CalculateLinkExpiration(nowUtc), nowUtc);
        await unitOfWork.CommitAsync(cancellationToken);

        var linkUrl = frontendUrlService.BuildSupplierQuotationUrl(token);
        string? emailDelivery = null;
        if (request.SendEmail)
        {
            emailDelivery = await emailSender.SendAsync(
                quotationRequest,
                quotation,
                linkUrl,
                nowUtc,
                cancellationToken);
            if (emailDelivery == SupplierQuotationEmailSender.Sent)
            {
                await unitOfWork.CommitAsync(cancellationToken);
            }
        }

        return QuotationRequestResponseMapper.ToSupplier(quotation, nowUtc, linkUrl, emailDelivery);
    }
}

public sealed class RevokeSupplierQuotationLinkUseCase(
    IQuotationRequestRepository repository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SupplierQuotationResponseDto> ExecuteAsync(
        Guid quotationRequestId,
        Guid supplierQuotationId,
        CancellationToken cancellationToken = default)
    {
        var (_, quotation) = await SupplierQuotationLookup.FindAsync(
            repository,
            currentUser,
            quotationRequestId,
            supplierQuotationId,
            cancellationToken);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        quotation.Revoke(nowUtc);
        await unitOfWork.CommitAsync(cancellationToken);

        return QuotationRequestResponseMapper.ToSupplier(quotation, nowUtc);
    }
}

internal static class SupplierQuotationLookup
{
    internal static async Task<(QuotationRequestEntity Request, SupplierQuotation Quotation)> FindAsync(
        IQuotationRequestRepository repository,
        ICurrentUser currentUser,
        Guid quotationRequestId,
        Guid supplierQuotationId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(quotationRequestId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(supplierQuotationId, Guid.Empty);

        var request = await repository.GetTrackedByIdForUserAsync(
            quotationRequestId,
            currentUser.UserId,
            cancellationToken);
        if (request is null)
        {
            throw new NotFoundException(
                "Quotation request not found.",
                "quotation_request_not_found");
        }

        var quotation = request.SupplierQuotations
            .FirstOrDefault(candidate => candidate.Id == supplierQuotationId);

        return quotation is null
            ? throw new NotFoundException(
                "Supplier quotation not found.",
                "supplier_quotation_not_found")
            : (request, quotation);
    }
}
