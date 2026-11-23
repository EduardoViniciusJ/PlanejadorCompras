using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Application.Features.Account.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.Infrastructure.Queries;

/// <summary>
/// Reads across every organization of one user, so the tenant filter is
/// bypassed on purpose. Each query is restricted to rows created by that user
/// (<c>UserId</c>) inside organizations the user is still a member of.
/// </summary>
public sealed class AccountDataExportQuery(PlanejadorComprasDbContext context)
    : IAccountDataExportQuery
{
    public async Task<AccountDataExportDto?> GetAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return null;
        }

        var memberships = await (
                from membership in context.Memberships.AsNoTracking()
                join organization in context.Organizations.AsNoTracking()
                    on membership.OrganizationId equals organization.Id
                where membership.UserId == userId
                orderby membership.CreatedAtUtc
                select new
                {
                    organization.Id,
                    organization.Name,
                    membership.Role,
                    membership.CreatedAtUtc
                })
            .ToListAsync(cancellationToken);

        // A List, not an array: see the note in ShoppingListOverviewQuery.
        var organizationIds = memberships.Select(membership => membership.Id).ToList();

        var lists = await context.ShoppingLists.IgnoreQueryFilters().AsNoTracking()
            .Where(list => list.UserId == userId && organizationIds.Contains(list.OrganizationId))
            .OrderBy(list => list.CreatedAt)
            .ToListAsync(cancellationToken);
        var listIds = lists.Select(list => list.Id).ToList();

        var items = await context.ShoppingItems.IgnoreQueryFilters().AsNoTracking()
            .Where(item => listIds.Contains(item.ShoppingListId))
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        var itemIds = items.Select(item => item.Id).ToList();

        var quotes = await (
                from quote in context.ItemQuotes.IgnoreQueryFilters().AsNoTracking()
                join supplier in context.Suppliers.IgnoreQueryFilters().AsNoTracking()
                    on quote.SupplierId equals supplier.Id
                where itemIds.Contains(quote.ShoppingItemId)
                orderby quote.CreatedAt
                select new
                {
                    quote.ShoppingItemId,
                    SupplierName = supplier.Name,
                    quote.UnitPrice,
                    quote.CreatedAt
                })
            .ToListAsync(cancellationToken);

        var suppliers = await context.Suppliers.IgnoreQueryFilters().AsNoTracking()
            .Where(supplier => supplier.UserId == userId
                               && organizationIds.Contains(supplier.OrganizationId))
            .OrderBy(supplier => supplier.Name)
            .ToListAsync(cancellationToken);

        var quotationRequests = await context.QuotationRequests.IgnoreQueryFilters().AsNoTracking()
            .Include(request => request.Items)
            .Where(request => request.UserId == userId
                              && organizationIds.Contains(request.OrganizationId))
            .OrderBy(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var purchaseOrders = await context.PurchaseOrders.IgnoreQueryFilters().AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.UserId == userId
                            && organizationIds.Contains(order.OrganizationId))
            .OrderBy(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var equalizations = await context.SavedEqualizations.IgnoreQueryFilters().AsNoTracking()
            .Where(equalization => equalization.UserId == userId
                                   && organizationIds.Contains(equalization.OrganizationId))
            .OrderBy(equalization => equalization.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var quotesByItem = quotes.ToLookup(quote => quote.ShoppingItemId);
        var itemsByList = items.ToLookup(item => item.ShoppingListId);

        var organizations = memberships
            .Select(membership => new AccountExportOrganizationDto(
                membership.Id,
                membership.Name,
                membership.Role.ToName(),
                membership.CreatedAtUtc,
                lists
                    .Where(list => list.OrganizationId == membership.Id)
                    .Select(list => new AccountExportShoppingListDto(
                        list.Id,
                        list.Name,
                        list.Description,
                        list.CreatedAt,
                        itemsByList[list.Id]
                            .Select(item => new AccountExportShoppingItemDto(
                                item.Name,
                                item.Quantity,
                                item.Unit,
                                quotesByItem[item.Id]
                                    .Select(quote => new AccountExportItemQuoteDto(
                                        quote.SupplierName,
                                        quote.UnitPrice,
                                        quote.CreatedAt))
                                    .ToList()))
                            .ToList()))
                    .ToList(),
                suppliers
                    .Where(supplier => supplier.OrganizationId == membership.Id)
                    .Select(supplier => new AccountExportSupplierDto(
                        supplier.Id,
                        supplier.Name,
                        supplier.Cnpj,
                        supplier.Address?.Street,
                        supplier.Address?.City,
                        supplier.Address?.PostalCode,
                        supplier.Contact?.Email,
                        supplier.Contact?.Phone,
                        supplier.CreatedAt))
                    .ToList(),
                quotationRequests
                    .Where(request => request.OrganizationId == membership.Id)
                    .Select(request => new AccountExportQuotationRequestDto(
                        request.Code,
                        request.ShoppingListName,
                        request.BuyerName,
                        request.BuyerEmail,
                        request.ResponseDeadline,
                        request.DeliveryAddress,
                        request.Instructions,
                        request.CreatedAtUtc,
                        request.Items
                            .OrderBy(item => item.Position)
                            .Select(item => new AccountExportDocumentItemDto(
                                item.Name,
                                item.Quantity,
                                item.Unit,
                                null))
                            .ToList()))
                    .ToList(),
                purchaseOrders
                    .Where(order => order.OrganizationId == membership.Id)
                    .Select(order => new AccountExportPurchaseOrderDto(
                        order.Code,
                        order.ShoppingListName,
                        order.SupplierName,
                        order.BuyerName,
                        order.BuyerEmail,
                        order.Status.ToString(),
                        order.ExpectedDeliveryDate,
                        order.DeliveryAddress,
                        order.PaymentTerms,
                        order.Notes,
                        order.TotalPrice,
                        order.CreatedAtUtc,
                        order.Items
                            .Select(item => new AccountExportDocumentItemDto(
                                item.Name,
                                item.Quantity,
                                item.Unit,
                                item.UnitPrice))
                            .ToList()))
                    .ToList(),
                equalizations
                    .Where(equalization => equalization.OrganizationId == membership.Id)
                    .Select(equalization => new AccountExportEqualizationDto(
                        equalization.Code,
                        equalization.ShoppingListName,
                        equalization.CreatedByName,
                        equalization.CreatedByEmail,
                        equalization.BestChoiceTotal,
                        equalization.BestCompleteSupplierName,
                        equalization.BestCompleteSupplierTotal,
                        equalization.EstimatedEconomy,
                        equalization.CreatedAtUtc))
                    .ToList()))
            .ToList();

        return new AccountDataExportDto(
            nowUtc,
            "Dados da sua conta e tudo o que você criou nas organizações de que participa.",
            new AccountExportProfileDto(
                user.Id,
                user.Email,
                user.Name,
                user.CreatedAt,
                user.IsEmailConfirmed,
                user.PasswordHash is not null,
                user.GoogleId is not null,
                user.TermsAcceptedAtUtc,
                user.TermsVersion),
            organizations);
    }
}
