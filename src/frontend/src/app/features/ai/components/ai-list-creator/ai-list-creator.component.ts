import { Component, DestroyRef, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, map, of, switchMap, tap } from 'rxjs';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { ModalDialogComponent } from '../../../../shared/ui/modal-dialog/modal-dialog.component';
import {
  ShoppingItemBatchFormComponent,
  ShoppingItemBatchRow,
} from '../../../shopping-items/components/shopping-item-batch-form/shopping-item-batch-form.component';
import { ShoppingItemService } from '../../../shopping-items/data-access/shopping-item.service';
import { ShoppingItemRequestDto } from '../../../shopping-items/dtos/shopping-item.dto';
import { ShoppingListService } from '../../../shopping-lists/data-access/shopping-list.service';
import { AiService } from '../../data-access/ai.service';
import { AiStatusDto } from '../../dtos/ai.dto';
import { aiErrorMessage, aiUnavailableReason } from '../../models/ai-support';
import { AiSourceInputComponent } from '../ai-source-input/ai-source-input.component';

export interface AiListCreated {
  readonly listId: string;
  readonly name: string;
  readonly itemCount: number;
}

const LIST_NAME_MAX_LENGTH = 100;

/**
 * "Criar lista com IA": the user describes what is needed (or sends a photo of a list),
 * the AI proposes the items and the user reviews them in the same grid used to add items
 * by hand. The list is created only when the user confirms.
 */
@Component({
  selector: 'app-ai-list-creator',
  imports: [
    ModalDialogComponent,
    AppIconComponent,
    AiSourceInputComponent,
    ShoppingItemBatchFormComponent,
  ],
  templateUrl: './ai-list-creator.component.html',
  styleUrl: './ai-list-creator.component.scss',
})
export class AiListCreatorComponent implements OnInit {
  private readonly aiService = inject(AiService);
  private readonly listService = inject(ShoppingListService);
  private readonly itemService = inject(ShoppingItemService);
  private readonly destroyRef = inject(DestroyRef);

  readonly disabled = input(false);

  /** Emitted after the reviewed list and its items were created. */
  readonly created = output<AiListCreated>();

  protected readonly status = signal<AiStatusDto | null>(null);
  protected readonly statusFailed = signal(false);
  protected readonly step = signal<'closed' | 'input' | 'review'>('closed');
  protected readonly text = signal('');
  protected readonly file = signal<File | null>(null);
  protected readonly isReading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly listName = signal('');
  protected readonly proposedRows = signal<readonly ShoppingItemBatchRow[]>([]);
  protected readonly discardedItems = signal(0);
  /** Kept when the list was created but adding the items failed, so a retry does not duplicate it. */
  private readonly createdListId = signal<string | null>(null);

  protected readonly nameMaxLength = LIST_NAME_MAX_LENGTH;
  protected readonly unavailableReason = computed(() =>
    aiUnavailableReason(this.status(), this.statusFailed()),
  );
  protected readonly nameError = computed(() => {
    const name = this.listName().trim();
    if (!name) return 'Informe um nome para a lista.';
    if (name.length > LIST_NAME_MAX_LENGTH) {
      return `O nome deve ter no máximo ${LIST_NAME_MAX_LENGTH} caracteres.`;
    }
    return null;
  });

  ngOnInit(): void {
    this.loadStatus();
  }

  protected open(): void {
    if (this.unavailableReason() || this.disabled()) return;
    this.text.set('');
    this.file.set(null);
    this.error.set(null);
    this.createdListId.set(null);
    this.step.set('input');
  }

  protected close(): void {
    if (this.isReading() || this.isSaving()) return;
    this.step.set('closed');
  }

  protected propose(): void {
    if (this.isReading()) return;
    this.isReading.set(true);
    this.error.set(null);

    this.aiService
      .draftShoppingList(this.text(), this.file())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.isReading.set(false);
          this.status.set(response.usage);
          if (response.items.length === 0) {
            this.error.set(
              'A IA não encontrou itens nesse conteúdo. Descreva os produtos e as quantidades e tente de novo.',
            );
            return;
          }

          this.listName.set(response.suggestedName.slice(0, LIST_NAME_MAX_LENGTH));
          this.proposedRows.set(
            response.items.map((item) => ({
              name: item.name,
              quantity: String(item.quantity).replace('.', ','),
              unit: item.unit,
            })),
          );
          this.discardedItems.set(response.discardedItems);
          this.createdListId.set(null);
          this.step.set('review');
        },
        error: (error: unknown) => {
          this.isReading.set(false);
          this.error.set(aiErrorMessage(error));
          this.loadStatus();
        },
      });
  }

  protected updateName(event: Event): void {
    this.listName.set((event.target as HTMLInputElement).value);
  }

  protected createList(items: readonly ShoppingItemRequestDto[]): void {
    if (this.isSaving() || this.nameError() || items.length === 0) {
      if (this.nameError()) this.error.set(this.nameError());
      return;
    }

    const name = this.listName().trim();
    this.isSaving.set(true);
    this.error.set(null);

    this.ensureList(name)
      .pipe(
        switchMap((listId) =>
          this.itemService
            .createBatch(items.map((item) => ({ ...item, shoppingListId: listId })))
            .pipe(map(() => listId)),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (listId) => {
          this.isSaving.set(false);
          this.step.set('closed');
          this.created.emit({ listId, name, itemCount: items.length });
        },
        error: () => {
          this.isSaving.set(false);
          this.error.set(
            this.createdListId()
              ? 'A lista foi criada, mas não foi possível adicionar os itens. Revise as linhas e tente de novo.'
              : 'Não foi possível criar a lista agora. Tente novamente.',
          );
        },
      });
  }

  /** Creates the list once; a retry after an item error reuses the list already created. */
  private ensureList(name: string): Observable<string> {
    const existing = this.createdListId();
    if (existing) return of(existing);

    return this.listService
      .createWithId({ name, description: null })
      .pipe(tap((listId) => this.createdListId.set(listId)));
  }

  private loadStatus(): void {
    this.aiService
      .getStatus()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (status) => {
          this.status.set(status);
          this.statusFailed.set(false);
        },
        error: () => {
          if (!this.status()) this.statusFailed.set(true);
        },
      });
  }
}
