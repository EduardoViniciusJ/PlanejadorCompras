import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { AccountDeletionPreviewDto, AccountDto } from '../../../../core/account/account.models';
import { AccountService } from '../../../../core/account/account.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { ModalDialogComponent } from '../../../../shared/ui/modal-dialog/modal-dialog.component';

const DELETE_CONFIRMATION_WORD = 'EXCLUIR';

export function problemMessage(error: HttpErrorResponse, fallback: string): string {
  const problem = error.error as { detail?: string; errors?: Record<string, string[]> } | null;
  const validation = problem?.errors ? Object.values(problem.errors).flat().at(0) : undefined;
  return validation ?? problem?.detail ?? fallback;
}

@Component({
  selector: 'app-settings-profile-page',
  imports: [DatePipe, ReactiveFormsModule, AppIconComponent, ModalDialogComponent],
  templateUrl: './settings-profile-page.component.html',
})
export class SettingsProfilePageComponent implements OnInit {
  private readonly accountService = inject(AccountService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);

  protected readonly account = signal<AccountDto | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly profileForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
  });
  protected readonly isSavingProfile = signal(false);
  protected readonly profileFeedback = signal<string | null>(null);
  protected readonly profileError = signal<string | null>(null);

  protected readonly passwordForm = this.formBuilder.group({
    currentPassword: ['', [Validators.required, Validators.maxLength(128)]],
    newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
    confirmation: ['', Validators.required],
  });
  protected readonly isSavingPassword = signal(false);
  protected readonly passwordFeedback = signal<string | null>(null);
  protected readonly passwordError = signal<string | null>(null);

  protected readonly isExporting = signal(false);
  protected readonly exportError = signal<string | null>(null);

  protected readonly deletionPreview = signal<AccountDeletionPreviewDto | null>(null);
  protected readonly isDeleteOpen = signal(false);
  protected readonly isLoadingPreview = signal(false);
  protected readonly isDeleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);
  protected readonly deleteForm = this.formBuilder.group({
    confirmation: ['', Validators.required],
    password: [''],
  });
  protected readonly deleteConfirmationWord = DELETE_CONFIRMATION_WORD;
  private readonly deleteFormVersion = signal(0);
  protected readonly canConfirmDelete = computed(() => {
    this.deleteFormVersion();
    const preview = this.deletionPreview();
    const { confirmation, password } = this.deleteForm.controls;
    return (
      !!preview?.canDelete &&
      confirmation.value.trim() === DELETE_CONFIRMATION_WORD &&
      (!preview.requiresPassword || password.value.length > 0)
    );
  });

  ngOnInit(): void {
    this.accountService
      .getAccount()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (account) => {
          this.account.set(account);
          this.profileForm.setValue({ name: account.name ?? '' });
          this.isLoading.set(false);
        },
        error: () => {
          this.loadError.set('Não foi possível carregar os dados da conta agora.');
          this.isLoading.set(false);
        },
      });
    this.deleteForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.deleteFormVersion.update((value) => value + 1));
  }

  protected passwordMismatch(): boolean {
    const { newPassword, confirmation } = this.passwordForm.controls;
    return confirmation.dirty && newPassword.value !== confirmation.value;
  }

  protected saveProfile(): void {
    if (this.profileForm.invalid || this.isSavingProfile()) {
      this.profileForm.markAllAsTouched();
      return;
    }
    const name = this.profileForm.controls.name.value.trim();
    this.isSavingProfile.set(true);
    this.profileFeedback.set(null);
    this.profileError.set(null);
    this.accountService
      .updateProfile(name)
      .pipe(
        finalize(() => this.isSavingProfile.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.profileFeedback.set('Perfil atualizado com sucesso.');
          this.authService.refreshCurrentUser().subscribe({ error: () => undefined });
        },
        error: (error: HttpErrorResponse) =>
          this.profileError.set(problemMessage(error, 'Não foi possível salvar o perfil agora.')),
      });
  }

  protected changePassword(): void {
    if (this.passwordForm.invalid || this.isSavingPassword()) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    const { currentPassword, newPassword, confirmation } = this.passwordForm.getRawValue();
    if (newPassword !== confirmation) {
      this.passwordError.set('As senhas informadas não coincidem.');
      return;
    }
    this.isSavingPassword.set(true);
    this.passwordFeedback.set(null);
    this.passwordError.set(null);
    this.accountService
      .changePassword(currentPassword, newPassword)
      .pipe(
        finalize(() => this.isSavingPassword.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.passwordForm.reset();
          this.passwordFeedback.set('Senha alterada com sucesso.');
        },
        error: (error: HttpErrorResponse) =>
          this.passwordError.set(problemMessage(error, 'Não foi possível alterar a senha agora.')),
      });
  }

  protected exportData(): void {
    if (this.isExporting()) return;
    this.isExporting.set(true);
    this.exportError.set(null);
    this.accountService
      .exportData()
      .pipe(
        finalize(() => this.isExporting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `planejador-de-compras-meus-dados-${new Date().toISOString().slice(0, 10)}.json`;
          link.click();
          URL.revokeObjectURL(url);
        },
        error: () => this.exportError.set('Não foi possível exportar os seus dados agora.'),
      });
  }

  protected openDelete(): void {
    this.deleteForm.reset();
    this.deleteError.set(null);
    this.deletionPreview.set(null);
    this.isDeleteOpen.set(true);
    this.isLoadingPreview.set(true);
    this.accountService
      .getDeletionPreview()
      .pipe(
        finalize(() => this.isLoadingPreview.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (preview) => this.deletionPreview.set(preview),
        error: () => this.deleteError.set('Não foi possível verificar o que será excluído agora.'),
      });
  }

  protected closeDelete(): void {
    if (this.isDeleting()) return;
    this.isDeleteOpen.set(false);
  }

  protected confirmDelete(): void {
    if (!this.canConfirmDelete() || this.isDeleting()) return;
    const { confirmation, password } = this.deleteForm.getRawValue();
    const preview = this.deletionPreview();
    this.isDeleting.set(true);
    this.deleteError.set(null);
    this.accountService
      .deleteAccount(confirmation.trim(), preview?.requiresPassword ? password : null)
      .pipe(
        finalize(() => this.isDeleting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.isDeleteOpen.set(false);
          void this.router.navigateByUrl('/');
        },
        error: (error: HttpErrorResponse) =>
          this.deleteError.set(problemMessage(error, 'Não foi possível excluir a conta agora.')),
      });
  }
}
