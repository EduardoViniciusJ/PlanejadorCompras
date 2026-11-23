namespace PlanejadorCompras.Application.Services.Interfaces;

public interface IFrontendUrlService
{
    string BuildEmailConfirmationUrl(string token);

    string BuildPasswordResetUrl(string token);

    /// <summary>Public page where a supplier answers a quotation without signing in.</summary>
    string BuildSupplierQuotationUrl(string token);
}
