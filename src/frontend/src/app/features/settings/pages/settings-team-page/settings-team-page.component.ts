import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { finalize } from 'rxjs/operators';

import {
  CreatedInvitationDto,
  InvitationDto,
  ORGANIZATION_ROLES,
  OrganizationDetailDto,
  OrganizationMemberDto,
  roleLabel,
} from '../../../../core/account/account.models';
import { AccountService } from '../../../../core/account/account.service';
import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { ModalDialogComponent } from '../../../../shared/ui/modal-dialog/modal-dialog.component';

function problemMessage(error: HttpErrorResponse, fallback: string): string {
  const problem = error.error as { detail?: string; errors?: Record<string, string[]> } | null;
  const validation = problem?.errors ? Object.values(problem.errors).flat().at(0) : undefined;
  return validation ?? problem?.detail ?? fallback;
}

@Component({
  selector: 'app-settings-team-page',
  imports: [DatePipe, ReactiveFormsModule, AppIconComponent, ModalDialogComponent],
  templateUrl: './settings-team-page.component.html',
})
export class SettingsTeamPageComponent implements OnInit {
  private readonly accountService = inject(AccountService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);

  protected readonly roles = ORGANIZATION_ROLES;
  protected readonly roleLabel = roleLabel;
  protected readonly organization = signal<OrganizationDetailDto | null>(null);
  protected readonly members = signal<readonly OrganizationMemberDto[]>([]);
  protected readonly invitations = signal<readonly InvitationDto[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly feedback = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);
  protected readonly busyId = signal<string | null>(null);

  protected readonly canManage = computed(
    () => this.organization()?.permissions.includes('manageTeam') ?? false,
  );
  protected readonly adminCount = computed(
    () => this.members().filter((member) => member.role === 'admin').length,
  );

  protected readonly nameForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
  });
  protected readonly isRenaming = signal(false);

  protected readonly isInviteOpen = signal(false);
  protected readonly isInviting = signal(false);
  protected readonly inviteError = signal<string | null>(null);
  protected readonly createdInvitation = signal<CreatedInvitationDto | null>(null);
  protected readonly inviteForm = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    role: ['buyer', Validators.required],
  });

  protected readonly removingMember = signal<OrganizationMemberDto | null>(null);
  protected readonly isLeaving = signal(false);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.accountService
      .getOrganization()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (organization) => {
          this.organization.set(organization);
          this.nameForm.setValue({ name: organization.name });
          const canManage = organization.permissions.includes('manageTeam');
          forkJoin([
            this.accountService.getMembers(),
            canManage ? this.accountService.getInvitations() : of([] as readonly InvitationDto[]),
          ])
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: ([members, invitations]) => {
                this.members.set(members);
                this.invitations.set(invitations);
                this.isLoading.set(false);
              },
              error: () => this.failLoad(),
            });
        },
        error: () => this.failLoad(),
      });
  }

  protected renameOrganization(): void {
    if (this.nameForm.invalid || this.isRenaming()) {
      this.nameForm.markAllAsTouched();
      return;
    }
    const name = this.nameForm.controls.name.value.trim();
    this.isRenaming.set(true);
    this.resetMessages();
    this.accountService
      .renameOrganization(name)
      .pipe(
        finalize(() => this.isRenaming.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.organization.update((value) => (value ? { ...value, name } : value));
          this.feedback.set('Nome da empresa atualizado.');
        },
        error: (error: HttpErrorResponse) =>
          this.actionError.set(problemMessage(error, 'Não foi possível renomear a empresa agora.')),
      });
  }

  protected changeRole(member: OrganizationMemberDto, event: Event): void {
    const role = (event.target as HTMLSelectElement).value;
    if (role === member.role) return;
    this.busyId.set(member.userId);
    this.resetMessages();
    this.accountService
      .changeMemberRole(member.userId, role)
      .pipe(
        finalize(() => this.busyId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.members.update((list) =>
            list.map((item) => (item.userId === member.userId ? { ...item, role } : item)),
          );
          this.feedback.set(`Papel de ${member.name || member.email} alterado.`);
        },
        error: (error: HttpErrorResponse) => {
          // Devolve o seletor ao valor anterior.
          this.members.update((list) => [...list]);
          this.actionError.set(problemMessage(error, 'Não foi possível alterar o papel agora.'));
        },
      });
  }

  protected isLastAdmin(member: OrganizationMemberDto): boolean {
    return member.role === 'admin' && this.adminCount() <= 1;
  }

  protected askRemove(member: OrganizationMemberDto): void {
    this.resetMessages();
    this.removingMember.set(member);
  }

  protected cancelRemove(): void {
    if (this.isLeaving()) return;
    this.removingMember.set(null);
  }

  protected confirmRemove(): void {
    const member = this.removingMember();
    if (!member || this.isLeaving()) return;
    this.isLeaving.set(true);
    const operation = member.isCurrentUser
      ? this.accountService.leaveOrganization()
      : this.accountService.removeMember(member.userId);
    operation
      .pipe(
        finalize(() => this.isLeaving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.removingMember.set(null);
          if (member.isCurrentUser) {
            void this.router.navigateByUrl('/app');
            return;
          }
          this.members.update((list) => list.filter((item) => item.userId !== member.userId));
          this.feedback.set(`${member.name || member.email} foi removido da equipe.`);
        },
        error: (error: HttpErrorResponse) => {
          this.removingMember.set(null);
          this.actionError.set(problemMessage(error, 'Não foi possível remover o membro agora.'));
        },
      });
  }

  protected openInvite(): void {
    this.resetMessages();
    this.inviteForm.reset({ email: '', role: 'buyer' });
    this.inviteError.set(null);
    this.createdInvitation.set(null);
    this.isInviteOpen.set(true);
  }

  protected closeInvite(): void {
    if (this.isInviting()) return;
    this.isInviteOpen.set(false);
  }

  protected sendInvite(): void {
    if (this.inviteForm.invalid || this.isInviting()) {
      this.inviteForm.markAllAsTouched();
      return;
    }
    const { email, role } = this.inviteForm.getRawValue();
    this.isInviting.set(true);
    this.inviteError.set(null);
    this.accountService
      .createInvitation(email.trim(), role)
      .pipe(
        finalize(() => this.isInviting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (created) => {
          this.createdInvitation.set(created);
          this.accountService
            .getInvitations()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({ next: (list) => this.invitations.set(list), error: () => undefined });
        },
        error: (error: HttpErrorResponse) =>
          this.inviteError.set(problemMessage(error, 'Não foi possível enviar o convite agora.')),
      });
  }

  protected copyLink(url: string): void {
    void navigator.clipboard?.writeText(url).then(
      () => this.feedback.set('Link do convite copiado.'),
      () => undefined,
    );
  }

  protected revoke(invitation: InvitationDto): void {
    this.busyId.set(invitation.id);
    this.resetMessages();
    this.accountService
      .revokeInvitation(invitation.id)
      .pipe(
        finalize(() => this.busyId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.invitations.update((list) => list.filter((item) => item.id !== invitation.id));
          this.feedback.set(`Convite para ${invitation.email} cancelado.`);
        },
        error: (error: HttpErrorResponse) =>
          this.actionError.set(problemMessage(error, 'Não foi possível cancelar o convite agora.')),
      });
  }

  private resetMessages(): void {
    this.feedback.set(null);
    this.actionError.set(null);
  }

  private failLoad(): void {
    this.loadError.set('Não foi possível carregar a equipe agora.');
    this.isLoading.set(false);
  }
}
