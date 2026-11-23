import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';

import {
  AcceptedInvitationDto,
  InvitationPreviewDto,
  roleLabel,
} from '../../../../core/account/account.models';
import { AccountService } from '../../../../core/account/account.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { BrandMarkComponent } from '../../../../shared/ui/brand-mark/brand-mark.component';

@Component({
  selector: 'app-accept-invitation-page',
  imports: [ReactiveFormsModule, RouterLink, AppIconComponent, BrandMarkComponent],
  templateUrl: './accept-invitation-page.component.html',
})
export class AcceptInvitationPageComponent implements OnInit {
  private readonly accountService = inject(AccountService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly roleLabel = roleLabel;
  protected readonly token = signal('');
  protected readonly preview = signal<InvitationPreviewDto | null>(null);
  protected readonly isSignedIn = signal(false);
  protected readonly isLoading = signal(true);
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly accepted = signal<AcceptedInvitationDto | null>(null);

  protected readonly registrationForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
    acceptedTerms: [false, Validators.requiredTrue],
  });

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token')?.trim() ?? '';
    this.token.set(token);
    if (!token) {
      this.errorMessage.set('Este link de convite está incompleto.');
      this.isLoading.set(false);
      return;
    }

    this.accountService
      .previewInvitation(token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (preview) => {
          this.preview.set(preview);
          this.authService.refreshCurrentUser().subscribe({
            next: () => {
              this.isSignedIn.set(true);
              this.isLoading.set(false);
            },
            error: () => this.isLoading.set(false),
          });
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(this.messageFor(error));
          this.isLoading.set(false);
        },
      });
  }

  protected accept(): void {
    if (this.isSubmitting()) return;
    this.submit(this.accountService.acceptInvitation(this.token()));
  }

  protected acceptWithRegistration(): void {
    if (this.registrationForm.invalid || this.isSubmitting()) {
      this.registrationForm.markAllAsTouched();
      return;
    }
    const { name, password, acceptedTerms } = this.registrationForm.getRawValue();
    this.submit(
      this.accountService.acceptInvitationWithRegistration(
        this.token(),
        name.trim(),
        password,
        acceptedTerms,
      ),
    );
  }

  protected goToApp(): void {
    void this.router.navigateByUrl('/app');
  }

  private submit(operation: ReturnType<AccountService['acceptInvitation']>): void {
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    operation
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => this.accepted.set(result),
        error: (error: HttpErrorResponse) => this.errorMessage.set(this.messageFor(error)),
      });
  }

  private messageFor(error: HttpErrorResponse): string {
    const problem = error.error as { detail?: string } | null;
    return problem?.detail ?? 'Não foi possível concluir o convite. Peça um novo link ao administrador.';
  }
}
