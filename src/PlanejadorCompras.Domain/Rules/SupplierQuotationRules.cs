namespace PlanejadorCompras.Domain.Rules;

public static class SupplierQuotationRules
{
    public const int MaximumSuppliersPerRequest = 30;
    public const int MaximumPricesPerResponse = 2000;
    public const int SupplierNameMaxLength = SupplierRules.NameMaxLength;
    public const int SupplierEmailMaxLength = SupplierRules.EmailMaxLength;
    public const int SupplierPhoneMaxLength = SupplierRules.PhoneMaxLength;

    /// <summary>SHA-256 in hexadecimal.</summary>
    public const int TokenHashLength = 64;

    public const string MinimumUnitPriceText = ItemQuoteRules.MinimumUnitPriceText;
    public const string MaximumUnitPriceText = "999999999.99";
    public const decimal MinimumUnitPrice = ItemQuoteRules.MinimumUnitPrice;
    public const decimal MaximumUnitPrice = 999999999.99m;
    public const int UnitPriceScale = ItemQuoteRules.UnitPriceScale;

    /// <summary>Link lifetime when the request has no response deadline.</summary>
    public static readonly TimeSpan LinkLifetimeWithoutDeadline = TimeSpan.FromDays(30);

    /// <summary>After the deadline the supplier can still read (not change) the answer for this long.</summary>
    public static readonly TimeSpan ReadOnlyPeriodAfterDeadline = TimeSpan.FromDays(7);

    /// <summary>
    /// The response deadline is a calendar date chosen by a buyer in Brazil: it ends at
    /// midnight of Brasília time (UTC-3, no daylight saving time since 2019).
    /// </summary>
    public static readonly TimeSpan DeadlineUtcOffset = TimeSpan.FromHours(-3);
}
