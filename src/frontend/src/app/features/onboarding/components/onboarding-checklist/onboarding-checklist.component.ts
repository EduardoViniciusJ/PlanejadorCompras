import { Component, computed, effect, inject, input, linkedSignal, output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../../../core/auth/auth.service';
import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { ShoppingList } from '../../../shopping-lists/models/shopping-list.model';
import {
  OnboardingClosedReason,
  readOnboardingClosedReason,
  saveOnboardingClosedReason,
} from '../../data-access/onboarding-preferences';
import { OnboardingService } from '../../data-access/onboarding.service';
import { buildOnboardingSteps, OnboardingStep } from '../../models/onboarding-step.model';

type ChecklistView = 'hidden' | 'loading' | 'ready';

/**
 * Lista de primeiros passos da tela inicial. O estado de cada passo vem dos dados
 * reais: as listas chegam pela página (que já as carrega) e o resto do serviço.
 * Some quando o usuário dispensa, quando tudo está concluído ou se o carregamento falhar.
 */
@Component({
  selector: 'app-onboarding-checklist',
  imports: [RouterLink, AppIconComponent],
  templateUrl: './onboarding-checklist.component.html',
  styleUrl: './onboarding-checklist.component.scss',
})
export class OnboardingChecklistComponent {
  private readonly authService = inject(AuthService);
  private readonly onboardingService = inject(OnboardingService);

  readonly lists = input.required<readonly ShoppingList[]>();
  readonly createListRequested = output<void>();
  readonly quotationRequested = output<ShoppingList>();

  private readonly userId = computed(() => this.authService.currentUser()?.id ?? null);
  private readonly closedReason = linkedSignal<OnboardingClosedReason | null>(() => {
    const userId = this.userId();
    return userId ? readOnboardingClosedReason(userId) : null;
  });

  private readonly remoteProgress = computed(() => {
    const progress = this.onboardingService.progress();
    return progress && progress.userId === this.userId() ? progress.value : null;
  });

  protected readonly steps = computed<readonly OnboardingStep[]>(() => {
    const remote = this.remoteProgress();
    return remote ? buildOnboardingSteps(this.lists(), remote) : [];
  });
  protected readonly completedCount = computed(
    () => this.steps().filter((step) => step.done).length,
  );
  protected readonly nextStepId = computed(
    () => this.steps().find((step) => !step.done)?.id ?? null,
  );
  private readonly isComplete = computed(
    () => this.steps().length > 0 && this.completedCount() === this.steps().length,
  );

  protected readonly view = computed<ChecklistView>(() => {
    const userId = this.userId();
    if (!userId || this.closedReason() || this.isComplete()) return 'hidden';
    if (this.onboardingService.failedFor() === userId) return 'hidden';
    return this.remoteProgress() ? 'ready' : 'loading';
  });

  constructor() {
    effect(() => {
      const userId = this.userId();
      if (userId && !this.closedReason()) {
        this.onboardingService.ensureLoaded(userId);
      }
    });

    effect(() => {
      const userId = this.userId();
      if (userId && this.isComplete() && !this.closedReason()) {
        saveOnboardingClosedReason(userId, 'completed');
        this.closedReason.set('completed');
      }
    });
  }

  protected dismiss(): void {
    const userId = this.userId();
    if (!userId) return;
    saveOnboardingClosedReason(userId, 'dismissed');
    this.closedReason.set('dismissed');
  }

  protected runAction(step: OnboardingStep): void {
    if (step.action.kind === 'create-list') {
      this.createListRequested.emit();
    } else if (step.action.kind === 'request-quotation') {
      this.quotationRequested.emit(step.action.list);
    }
  }
}
