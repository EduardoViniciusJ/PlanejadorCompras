using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.Infrastructure.Services;

/// <summary>
/// Sequential numbering per organization, document kind and year. The counter
/// is advanced by a single atomic upsert, so two simultaneous requests never
/// receive the same number. A failed save leaves a gap, never a duplicate.
/// </summary>
public sealed class DocumentNumberGenerator(
    PlanejadorComprasDbContext context,
    ICurrentOrganization currentOrganization) : IDocumentNumberGenerator
{
    public async Task<string> NextAsync(
        DocumentKind kind,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var organizationId = currentOrganization.OrganizationId;
        var prefix = GetPrefix(kind);
        var year = nowUtc.Year;

        var numbers = await context.Database
            .SqlQuery<int>($@"
                INSERT INTO ""OrganizationDocumentCounters"" (""OrganizationId"", ""Kind"", ""Year"", ""LastNumber"")
                VALUES ({organizationId}, {prefix}, {year}, 1)
                ON CONFLICT (""OrganizationId"", ""Kind"", ""Year"")
                DO UPDATE SET ""LastNumber"" = ""OrganizationDocumentCounters"".""LastNumber"" + 1
                RETURNING ""LastNumber"" AS ""Value""")
            .ToListAsync(cancellationToken);

        return Format(prefix, year, numbers.Single());
    }

    public static string Format(string prefix, int year, int number) =>
        $"{prefix}-{year}-{number:D5}";

    public static string GetPrefix(DocumentKind kind) => kind switch
    {
        DocumentKind.QuotationRequest => "SC",
        DocumentKind.PurchaseOrder => "PC",
        DocumentKind.Equalization => "EQ",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
