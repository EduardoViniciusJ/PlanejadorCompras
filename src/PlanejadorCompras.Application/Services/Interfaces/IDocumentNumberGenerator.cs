namespace PlanejadorCompras.Application.Services.Interfaces;

public enum DocumentKind
{
    QuotationRequest,
    PurchaseOrder,
    Equalization
}

/// <summary>
/// Issues sequential document numbers per organization, kind and year
/// (for example SC-2026-00001). Numbers never repeat inside an organization.
/// </summary>
public interface IDocumentNumberGenerator
{
    Task<string> NextAsync(
        DocumentKind kind,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
}
