using System.ComponentModel.DataAnnotations;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Application.Features.Organizations.Contracts;

public sealed record OrganizationDetailDto(
    Guid Id,
    string Name,
    string Role,
    int MemberCount,
    IReadOnlyCollection<string> Permissions);

public sealed record UpdateOrganizationRequestDto
{
    public UpdateOrganizationRequestDto(string name) => Name = name;

    [Required, StringLength(OrganizationRules.NameMaxLength, MinimumLength = 2)]
    public string Name { get; init; }
}

public sealed record OrganizationMemberDto(
    Guid UserId,
    string Email,
    string? Name,
    string Role,
    DateTime JoinedAtUtc,
    bool IsCurrentUser);

public sealed record ChangeMemberRoleRequestDto
{
    public ChangeMemberRoleRequestDto(string role) => Role = role;

    [Required, StringLength(20)]
    public string Role { get; init; }
}

public sealed record InvitationDto(
    Guid Id,
    string Email,
    string Role,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    string InvitedBy);

public sealed record CreateInvitationRequestDto
{
    public CreateInvitationRequestDto(string email, string role) =>
        (Email, Role) = (email, role);

    [Required, EmailAddress, StringLength(OrganizationRules.EmailMaxLength)]
    public string Email { get; init; }

    [Required, StringLength(20)]
    public string Role { get; init; }
}

/// <summary>
/// The accept link is returned to the administrator who created the
/// invitation so it can be shared by hand when the e-mail was not delivered.
/// It is the only moment the raw token exists outside the e-mail.
/// </summary>
public sealed record CreatedInvitationDto(
    Guid Id,
    string Email,
    string Role,
    DateTime ExpiresAtUtc,
    bool EmailSent,
    string AcceptUrl);

public sealed record InvitationPreviewDto(
    string OrganizationName,
    string Email,
    string Role,
    DateTime ExpiresAtUtc,
    bool AccountExists);

public sealed record AcceptInvitationRequestDto
{
    public AcceptInvitationRequestDto(string token) => Token = token;

    [Required, StringLength(128)]
    public string Token { get; init; }
}

public sealed record AcceptInvitationWithRegistrationRequestDto
{
    public AcceptInvitationWithRegistrationRequestDto(
        string token,
        string name,
        string password,
        bool acceptedTerms) =>
        (Token, Name, Password, AcceptedTerms) = (token, name, password, acceptedTerms);

    [Required, StringLength(128)]
    public string Token { get; init; }

    [Required, StringLength(OrganizationRules.UserNameMaxLength, MinimumLength = 2)]
    public string Name { get; init; }

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; }

    public bool AcceptedTerms { get; init; }
}

public sealed record AcceptedInvitationDto(
    Guid OrganizationId,
    string OrganizationName,
    string Role);
