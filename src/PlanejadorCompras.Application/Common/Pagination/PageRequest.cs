using PlanejadorCompras.Application.Exceptions;

namespace PlanejadorCompras.Application.Common.Pagination;

/// <summary>
/// Page parameters shared by the list endpoints. The page size has a hard
/// ceiling so a single request can never load an organization's whole history.
/// </summary>
public static class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static void Validate(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new BadRequestException(
                $"Informe uma paginação válida (página a partir de 1 e no máximo {MaxPageSize} itens por página).",
                "invalid_pagination");
        }
    }
}
