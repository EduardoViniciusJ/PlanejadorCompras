using PlanejadorCompras.Application.Common.Pagination;
using PlanejadorCompras.Application.Features.Suppliers.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.Supplier;

namespace PlanejadorCompras.Application.UseCases.Supplier;

public sealed class GetSuppliersUseCase(
    ISupplierRepository supplierRepository,
    ICurrentOrganization currentOrganization)
{
    public async Task<PagedResponseDto<SupplierResponseDto>> ExecuteAsync(
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PageRequest.Validate(page, pageSize);

        var result = await supplierRepository.GetPageByOrganizationIdAsync(
            currentOrganization.OrganizationId,
            page,
            pageSize,
            cancellationToken);

        return new PagedResponseDto<SupplierResponseDto>(
            result.Items.Select(SupplierResponseMapper.Map).ToList(),
            page,
            pageSize,
            result.TotalCount);
    }
}
