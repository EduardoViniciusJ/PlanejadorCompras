namespace PlanejadorCompras.Domain.Rules;

public static class OrganizationRules
{
    public const int NameMaxLength = 120;
    public const int UserNameMaxLength = 150;
    public const int EmailMaxLength = 320;
    public const int InvitationTokenHashLength = 64;
    public const int InvitationValidDays = 7;
    public const int MaxPendingInvitations = 50;
    public const int TermsVersionMaxLength = 20;

    /// <summary>Version of the terms of use and privacy policy currently published.</summary>
    public const string CurrentTermsVersion = "2026-11";
}
