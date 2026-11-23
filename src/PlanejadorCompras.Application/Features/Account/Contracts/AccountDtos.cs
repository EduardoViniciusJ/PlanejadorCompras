using System.ComponentModel.DataAnnotations;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Application.Features.Account.Contracts;

public sealed record AccountOrganizationDto(
    Guid Id,
    string Name,
    string Role,
    bool IsActive);

public sealed record AccountDto(
    Guid Id,
    string Email,
    string? Name,
    bool HasPassword,
    bool HasGoogleLogin,
    DateTime CreatedAt,
    DateTime? TermsAcceptedAtUtc,
    string? TermsVersion,
    AccountOrganizationDto ActiveOrganization,
    IReadOnlyCollection<AccountOrganizationDto> Organizations,
    IReadOnlyCollection<string> Permissions);

public sealed record UpdateProfileRequestDto
{
    public UpdateProfileRequestDto(string name) => Name = name;

    [Required, StringLength(OrganizationRules.UserNameMaxLength, MinimumLength = 2)]
    public string Name { get; init; }
}

public sealed record ChangePasswordRequestDto
{
    public ChangePasswordRequestDto(string currentPassword, string newPassword) =>
        (CurrentPassword, NewPassword) = (currentPassword, newPassword);

    [Required, StringLength(128)]
    public string CurrentPassword { get; init; }

    [Required, StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; init; }
}

public sealed record SwitchOrganizationRequestDto(Guid OrganizationId);

public sealed record DeleteAccountRequestDto
{
    public DeleteAccountRequestDto(string confirmation, string? password = null) =>
        (Confirmation, Password) = (confirmation, password);

    /// <summary>Must be the word EXCLUIR, typed by the user.</summary>
    [Required, StringLength(20)]
    public string Confirmation { get; init; }

    /// <summary>Required when the account has a password.</summary>
    [StringLength(128)]
    public string? Password { get; init; }
}

public sealed record AccountDeletionOrganizationDto(Guid Id, string Name);

/// <summary>What deleting the account does to each organization the user belongs to.</summary>
public sealed record AccountDeletionPreviewDto(
    bool CanDelete,
    bool RequiresPassword,
    IReadOnlyCollection<AccountDeletionOrganizationDto> OrganizationsToDelete,
    IReadOnlyCollection<AccountDeletionOrganizationDto> OrganizationsToLeave,
    IReadOnlyCollection<AccountDeletionOrganizationDto> BlockingOrganizations);
