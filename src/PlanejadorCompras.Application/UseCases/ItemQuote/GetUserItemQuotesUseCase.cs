using PlanejadorCompras.Application.Features.ItemQuotes.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Application.UseCases.ItemQuote;

public sealed class GetUserItemQuotesUseCase
{
    private readonly ICurrentOrganization _currentOrganization;
    private readonly IUserItemQuotesQuery _userItemQuotesQuery;

    public GetUserItemQuotesUseCase(
        ICurrentOrganization currentOrganization,
        IUserItemQuotesQuery userItemQuotesQuery)
    {
        _currentOrganization = currentOrganization;
        _userItemQuotesQuery = userItemQuotesQuery;
    }

    public async Task<UserItemQuotesResponseDto> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var quotes = await _userItemQuotesQuery.GetByOrganizationIdAsync(
            _currentOrganization.OrganizationId,
            cancellationToken);

        return new UserItemQuotesResponseDto(quotes);
    }
}
