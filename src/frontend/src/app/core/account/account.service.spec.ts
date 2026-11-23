import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { buildApiUrl } from '../api/api-url';
import { AccountService } from './account.service';

describe('AccountService', () => {
  let service: AccountService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AccountService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('should send the invitation with e-mail and role', () => {
    service.createInvitation('pessoa@empresa.com', 'buyer').subscribe();

    const request = httpTesting.expectOne(buildApiUrl('/api/organization/invitations'));
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'pessoa@empresa.com', role: 'buyer' });
    request.flush({});
  });

  it('should change a member role and remove a member', () => {
    service.changeMemberRole('user-1', 'approver').subscribe();
    const roleRequest = httpTesting.expectOne(buildApiUrl('/api/organization/members/user-1/role'));
    expect(roleRequest.request.method).toBe('PUT');
    expect(roleRequest.request.body).toEqual({ role: 'approver' });
    roleRequest.flush(null);

    service.removeMember('user-1').subscribe();
    const removeRequest = httpTesting.expectOne(buildApiUrl('/api/organization/members/user-1'));
    expect(removeRequest.request.method).toBe('DELETE');
    removeRequest.flush(null);
  });

  it('should request the data export as a blob and delete the account with confirmation', () => {
    service.exportData().subscribe();
    const exportRequest = httpTesting.expectOne(buildApiUrl('/api/account/export'));
    expect(exportRequest.request.responseType).toBe('blob');
    exportRequest.flush(new Blob(['{}']));

    service.deleteAccount('EXCLUIR', 'senha-segura').subscribe();
    const deleteRequest = httpTesting.expectOne(buildApiUrl('/api/account/delete'));
    expect(deleteRequest.request.body).toEqual({
      confirmation: 'EXCLUIR',
      password: 'senha-segura',
    });
    deleteRequest.flush(null);
  });
});
