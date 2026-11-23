namespace PlanejadorCompras.Domain.Rules;

public static class CommercialTermsRules
{
    public const int MinimumDeliveryDays = 0;
    public const int MaximumDeliveryDays = 365;
    public const int PaymentTermsMaxLength = 200;
    public const int NotesMaxLength = 1000;
    public const int AmountPrecision = 18;
    public const int AmountScale = 2;
    public const string MinimumAmountText = "0";
    public const string MaximumAmountText = "999999999.99";
    public const decimal MinimumAmount = 0m;
    public const decimal MaximumAmount = 999999999.99m;
}
