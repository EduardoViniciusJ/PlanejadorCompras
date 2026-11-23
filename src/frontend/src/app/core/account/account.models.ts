export type OrganizationRole = 'admin' | 'buyer' | 'requester' | 'approver';

export const ORGANIZATION_ROLES: readonly { readonly value: OrganizationRole; readonly label: string; readonly description: string }[] = [
  { value: 'admin', label: 'Administrador', description: 'Acesso total, inclusive à equipe da empresa.' },
  { value: 'buyer', label: 'Comprador', description: 'Cadastra fornecedores, preços, cotações e pedidos.' },
  { value: 'requester', label: 'Solicitante', description: 'Monta e edita listas de compras.' },
  { value: 'approver', label: 'Aprovador', description: 'Conclui ou cancela pedidos emitidos.' },
];

export function roleLabel(role: string): string {
  return ORGANIZATION_ROLES.find((item) => item.value === role)?.label ?? role;
}

export interface AccountOrganizationDto {
  readonly id: string;
  readonly name: string;
  readonly role: string;
  readonly isActive: boolean;
}

export interface AccountDto {
  readonly id: string;
  readonly email: string;
  readonly name: string | null;
  readonly hasPassword: boolean;
  readonly hasGoogleLogin: boolean;
  readonly createdAt: string;
  readonly termsAcceptedAtUtc: string | null;
  readonly termsVersion: string | null;
  readonly activeOrganization: AccountOrganizationDto;
  readonly organizations: readonly AccountOrganizationDto[];
  readonly permissions: readonly string[];
}

export interface AccountDeletionOrganizationDto {
  readonly id: string;
  readonly name: string;
}

export interface AccountDeletionPreviewDto {
  readonly canDelete: boolean;
  readonly requiresPassword: boolean;
  readonly organizationsToDelete: readonly AccountDeletionOrganizationDto[];
  readonly organizationsToLeave: readonly AccountDeletionOrganizationDto[];
  readonly blockingOrganizations: readonly AccountDeletionOrganizationDto[];
}

export interface OrganizationDetailDto {
  readonly id: string;
  readonly name: string;
  readonly role: string;
  readonly memberCount: number;
  readonly permissions: readonly string[];
}

export interface OrganizationMemberDto {
  readonly userId: string;
  readonly email: string;
  readonly name: string | null;
  readonly role: string;
  readonly joinedAtUtc: string;
  readonly isCurrentUser: boolean;
}

export interface InvitationDto {
  readonly id: string;
  readonly email: string;
  readonly role: string;
  readonly createdAtUtc: string;
  readonly expiresAtUtc: string;
  readonly invitedBy: string;
}

export interface CreatedInvitationDto {
  readonly id: string;
  readonly email: string;
  readonly role: string;
  readonly expiresAtUtc: string;
  readonly emailSent: boolean;
  readonly acceptUrl: string;
}

export interface InvitationPreviewDto {
  readonly organizationName: string;
  readonly email: string;
  readonly role: string;
  readonly expiresAtUtc: string;
  readonly accountExists: boolean;
}

export interface AcceptedInvitationDto {
  readonly organizationId: string;
  readonly organizationName: string;
  readonly role: string;
}
