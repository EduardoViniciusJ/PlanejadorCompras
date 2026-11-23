using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.User;
using PlanejadorCompras.Domain.Repositories.RefreshToken;
using PlanejadorCompras.Domain.Repositories.ShoppingList;
using PlanejadorCompras.Domain.Repositories.ShoppingItem;
using PlanejadorCompras.Domain.Repositories.ItemQuote;
using PlanejadorCompras.Domain.Repositories.Supplier;
using PlanejadorCompras.Domain.Repositories.ShoppingListSupplier;
using PlanejadorCompras.Domain.Repositories.PurchaseOrder;
using PlanejadorCompras.Domain.Repositories.Equalization;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;
using PlanejadorCompras.Infrastructure.Persistence;
using PlanejadorCompras.Infrastructure.Queries;
using PlanejadorCompras.Infrastructure.Reports.Excel;
using PlanejadorCompras.Infrastructure.Reports.Pdf;
using PlanejadorCompras.Infrastructure.Repositories;
using PlanejadorCompras.Infrastructure.Services;

namespace PlanejadorCompras.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDbContextPostgreSql(services, configuration);
        AddRepositories(services);
        AddUnitOfWork(services);
        AddApplicationServices(services);
        AddReportExporters(services);
        AddExternalServices(services);
        AddSessionSettings(services, configuration);

        return services;
    }

    private static void AddDbContextPostgreSql(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing connection string 'ConnectionStrings:DefaultConnection'.");
        }

        services.AddDbContext<PlanejadorComprasDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    private static void AddSessionSettings(IServiceCollection services, IConfiguration configuration)
    {
        var defaults = SessionSettings.Default;
        var lifetime = int.TryParse(configuration["Authentication:RefreshToken:ExpiresInDays"], out var days) && days > 0
            ? TimeSpan.FromDays(days)
            : defaults.RefreshTokenLifetime;
        var leeway = int.TryParse(configuration["Authentication:RefreshToken:ReuseLeewaySeconds"], out var seconds) && seconds >= 0
            ? TimeSpan.FromSeconds(seconds)
            : defaults.RefreshTokenReuseLeeway;

        services.AddSingleton(new SessionSettings(lifetime, leeway));
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IShoppingItemRepository, ShoppingItemRepository>();
        services.AddScoped<IItemQuoteRepository, ItemQuoteRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IShoppingListSupplierRepository, ShoppingListSupplierRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<ISavedEqualizationRepository, SavedEqualizationRepository>();
        services.AddScoped<IQuotationRequestRepository, QuotationRequestRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    }

    private static void AddUnitOfWork(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IShoppingListOverviewQuery, ShoppingListOverviewQuery>();
        services.AddScoped<IShoppingListDetailQuery, ShoppingListDetailQuery>();
        services.AddScoped<IUserItemQuotesQuery, UserItemQuotesQuery>();
        services.AddScoped<IShoppingListAccessService, ShoppingListAccessService>();
        services.AddScoped<ISupplierAccessService, SupplierAccessService>();
        services.AddScoped<IPurchaseOrderAccessService, PurchaseOrderAccessService>();
        services.AddScoped<ISavedEqualizationAccessService, SavedEqualizationAccessService>();
    }

    private static void AddReportExporters(IServiceCollection services)
    {
        EmbeddedPdfFontResolver.EnsureRegistered();
        services.AddSingleton<ShoppingListPdfDocumentBuilder>();
        services.AddSingleton<PurchaseOrderPdfDocumentBuilder>();
        services.AddSingleton<IShoppingListExcelExporter, ClosedXmlShoppingListReportExporter>();
        services.AddSingleton<IShoppingListPdfExporter, PdfSharpShoppingListReportExporter>();
        services.AddSingleton<QuotationRequestPdfDocumentBuilder>();
        services.AddSingleton<IQuotationRequestPdfExporter, PdfSharpQuotationRequestExporter>();
        services.AddSingleton<IPurchaseOrderPdfExporter, PdfSharpPurchaseOrderExporter>();
        services.AddSingleton<IPurchaseOrderExcelExporter, ClosedXmlPurchaseOrderExporter>();
    }

    private static void AddExternalServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddHttpClient<IGoogleAuthorizationCodeExchanger, GoogleAuthorizationCodeExchanger>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPurchaseOrderCodeGenerator, PurchaseOrderCodeGenerator>();
        services.AddSingleton<ISavedEqualizationCodeGenerator, SavedEqualizationCodeGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ILoginAttemptLimiter, InMemoryLoginAttemptLimiter>();
        services.AddScoped<IEmailService, ResendEmailService>();
        services.AddSingleton<IEmailAvailability, ConfiguredEmailAvailability>();
        // ResendEmailService throws while being built when the key is missing, so flows that
        // can work without e-mail resolve it only when they are really going to send.
        services.AddScoped(provider =>
            new Lazy<IEmailService>(provider.GetRequiredService<IEmailService>));
        services.AddSingleton<IFrontendUrlService, FrontendUrlService>();
    }
}
