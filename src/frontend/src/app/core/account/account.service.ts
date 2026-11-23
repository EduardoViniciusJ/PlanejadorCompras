import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { buildApiUrl } from '../api/api-url';
import {
  AcceptedInvitationDto,
  AccountDeletionPreviewDto,
  AccountDto,
  CreatedInvitationDto,
  InvitationDto,
  InvitationPreviewDto,
  OrganizationDetailDto,
  OrganizationMemberDto,
} from './account.models';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);

  getAccount(): Observable<AccountDto> {
    return this.http.get<AccountDto>(buildApiUrl('/api/account'));
  }

  updateProfile(name: string): Observable<void> {
    return this.http.put<void>(buildApiUrl('/api/account/profile'), { name });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(buildApiUrl('/api/account/change-password'), {
      currentPassword,
      newPassword,
    });
  }

  exportData(): Observable<Blob> {
    return this.http.get(buildApiUrl('/api/account/export'), { responseType: 'blob' });
  }

  getDeletionPreview(): Observable<AccountDeletionPreviewDto> {
    return this.http.get<AccountDeletionPreviewDto>(buildApiUrl('/api/account/deletion-preview'));
  }

  deleteAccount(confirmation: string, password: string | null): Observable<void> {
    return this.http.post<void>(buildApiUrl('/api/account/delete'), { confirmation, password });
  }

  getOrganization(): Observable<OrganizationDetailDto> {
    return this.http.get<OrganizationDetailDto>(buildApiUrl('/api/organization'));
  }

  renameOrganization(name: string): Observable<void> {
    return this.http.put<void>(buildApiUrl('/api/organization'), { name });
  }

  getMembers(): Observable<readonly OrganizationMemberDto[]> {
    return this.http.get<readonly OrganizationMemberDto[]>(buildApiUrl('/api/organization/members'));
  }

  changeMemberRole(userId: string, role: string): Observable<void> {
    return this.http.put<void>(buildApiUrl(`/api/organization/members/${userId}/role`), { role });
  }

  removeMember(userId: string): Observable<void> {
    return this.http.delete<void>(buildApiUrl(`/api/organization/members/${userId}`));
  }

  leaveOrganization(): Observable<void> {
    return this.http.post<void>(buildApiUrl('/api/organization/leave'), {});
  }

  getInvitations(): Observable<readonly InvitationDto[]> {
    return this.http.get<readonly InvitationDto[]>(buildApiUrl('/api/organization/invitations'));
  }

  createInvitation(email: string, role: string): Observable<CreatedInvitationDto> {
    return this.http.post<CreatedInvitationDto>(buildApiUrl('/api/organization/invitations'), {
      email,
      role,
    });
  }

  revokeInvitation(id: string): Observable<void> {
    return this.http.delete<void>(buildApiUrl(`/api/organization/invitations/${id}`));
  }

  previewInvitation(token: string): Observable<InvitationPreviewDto> {
    return this.http.post<InvitationPreviewDto>(buildApiUrl('/api/invitations/preview'), { token });
  }

  acceptInvitation(token: string): Observable<AcceptedInvitationDto> {
    return this.http.post<AcceptedInvitationDto>(buildApiUrl('/api/invitations/accept'), { token });
  }

  acceptInvitationWithRegistration(
    token: string,
    name: string,
    password: string,
    acceptedTerms: boolean,
  ): Observable<AcceptedInvitationDto> {
    return this.http.post<AcceptedInvitationDto>(
      buildApiUrl('/api/invitations/accept-with-registration'),
      { token, name, password, acceptedTerms },
    );
  }

  switchOrganization(organizationId: string): Observable<void> {
    return this.http.post<void>(buildApiUrl('/api/account/active-organization'), {
      organizationId,
    });
  }
}
