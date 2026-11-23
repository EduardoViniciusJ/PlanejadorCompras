using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;

namespace PlanejadorCompras.Application.UseCases.QuotationRequest;

public sealed class DeleteQuotationRequestUseCase(
    IQuotationRequestRepository repository,
    ICurrentOrganization currentOrganization,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        if (!await repository.DeleteForOrganizationAsync(id, currentOrganization.OrganizationId, cancellationToken))
            throw new NotFoundException("Quotation request not found.", "quotation_request_not_found");
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
