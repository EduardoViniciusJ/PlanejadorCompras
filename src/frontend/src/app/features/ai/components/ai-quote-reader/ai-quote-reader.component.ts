import { Component, DestroyRef, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { ModalDialogComponent } from '../../../../shared/ui/modal-dialog/modal-dialog.component';
import { AiService } from '../../data-access/ai.service';
import { AiMatchConfidence, AiStatusDto, QuoteExtractionResponseDto } from '../../dtos/ai.dto';
import {
  AI_CONFIDENCE_LABELS,
  aiErrorMessage,
  aiUnavailableReason,
  formatPriceInput,
  parseLocalizedPrice,
} from '../../models/ai-support';
import { AiSourceInputComponent } from '../ai-source-input/ai-source-input.component';

export interface AiQuoteReaderItem {
  readonly id: string;
  readonly name: string;
  readonly quantity: number;
  readonly unit: string;
}

export interface AiQuoteReaderSupplier {
  readonly id: string;
  readonly name: string;
}

export interface AiQuotesApplied {
  readonly supplierName: string;
  readonly created: number;
  readonly updated: number;
}

/** One line of the review grid. Nothing here is saved until the buyer confirms. */
interface ReviewRow {
  readonly lineNumber: number;
  readonly description: string;
  readonly supplierUnit: string | null;
  readonly supplierUnitPrice: number | null;
  readonly confidence: AiMatchConfidence;
  readonly note: string | null;
  /** Item chosen for this line; empty when there is no match yet. */
  itemId: string;
  price: string;
  /** Whether the buyer wants this line saved. */
  include: boolean;
}

const currencyFormatter = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const MAX_UNIT_PRICE = 9999999999999999.99;

/**
 * "Ler cotação com IA": the buyer pastes the supplier's answer or sends a file, the AI
 * proposes prices matched to the list, and the buyer corrects, discards or confirms each
 * line and chooses the supplier. Prices are saved only on confirmation.
 */
@Component({
  selector: 'app-ai-quote-reader',
  imports: [ModalDialogComponent, AppIconComponent, AiSourceInputComponent],
  templateUrl: './ai-quote-reader.component.html',
  styleUrl: './ai-quote-reader.component.scss',
})
export class AiQuoteReaderComponent implements OnInit {
  private readonly aiService = inject(AiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly listId = input.required<string>();
  readonly items = input.required<readonly AiQuoteReaderItem[]>();
  readonly suppliers = input.required<readonly AiQuoteReaderSupplier[]>();
  /** Current prices keyed by "itemId:supplierId", used to warn before replacing one. */
  readonly existingPrices = input<ReadonlyMap<string, { readonly unitPrice: number }>>(new Map());
  readonly disabled = input(false);

  /** Emitted after the confirmed prices were saved. */
  readonly applied = output<AiQuotesApplied>();

  protected readonly status = signal<AiStatusDto | null>(null);
  protected readonly statusFailed = signal(false);
  protected readonly step = signal<'closed' | 'input' | 'review'>('closed');
  protected readonly text = signal('');
  protected readonly file = signal<File | null>(null);
  protected readonly isReading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly rows = signal<readonly ReviewRow[]>([]);
  protected readonly generalConditions = signal<string | null>(null);
  protected readonly discardedLines = signal(0);
  protected readonly supplierId = signal('');
  protected readonly confidenceLabels = AI_CONFIDENCE_LABELS;

  protected readonly unavailableReason = computed(() =>
    aiUnavailableReason(this.status(), this.statusFailed()),
  );
  protected readonly missingData = computed(() => {
    if (this.items().length === 0) return 'Adicione itens à lista antes de ler uma cotação.';
    if (this.suppliers().length === 0) {
      return 'Adicione o fornecedor a esta lista antes de ler a cotação dele.';
    }
    return null;
  });
  protected readonly includedRows = computed(() => this.rows().filter((row) => row.include));
  /** Items picked by more than one included line. */
  protected readonly duplicatedItemIds = computed(() => {
    const seen = new Set<string>();
    const duplicated = new Set<string>();
    for (const row of this.includedRows()) {
      if (!row.itemId) continue;
      if (seen.has(row.itemId)) duplicated.add(row.itemId);
      seen.add(row.itemId);
    }
    return duplicated;
  });
  protected readonly canConfirm = computed(
    () =>
      !this.isSaving() &&
      !!this.supplierId() &&
      this.includedRows().length > 0 &&
      this.includedRows().every((row) => this.rowError(row) === null),
  );

  ngOnInit(): void {
    this.loadStatus();
  }

  protected open(): void {
    if (this.unavailableReason() || this.disabled()) return;
    this.text.set('');
    this.file.set(null);
    this.error.set(null);
    this.rows.set([]);
    this.step.set('input');
  }

  protected close(): void {
    if (this.isReading() || this.isSaving()) return;
    this.step.set('closed');
  }

  protected read(): void {
    if (this.isReading() || this.missingData()) return;
    this.isReading.set(true);
    this.error.set(null);

    this.aiService
      .extractQuote(this.listId(), this.text(), this.file())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.isReading.set(false);
          this.status.set(response.usage);
          this.showReview(response);
        },
        error: (error: unknown) => {
          this.isReading.set(false);
          this.error.set(aiErrorMessage(error));
          // A failed call may still have counted (or the limit may have been reached).
          this.loadStatus();
        },
      });
  }

  protected backToInput(): void {
    if (this.isSaving()) return;
    this.error.set(null);
    this.step.set('input');
  }

  protected setInclude(row: ReviewRow, event: Event): void {
    this.updateRow(row, { include: (event.target as HTMLInputElement).checked });
  }

  protected setItem(row: ReviewRow, event: Event): void {
    const itemId = (event.target as HTMLSelectElement).value;
    // Picking an item is a decision to use the line; clearing it discards the line.
    this.updateRow(row, { itemId, include: itemId !== '' });
  }

  protected setPrice(row: ReviewRow, event: Event): void {
    this.updateRow(row, { price: (event.target as HTMLInputElement).value });
  }

  protected setSupplier(event: Event): void {
    this.supplierId.set((event.target as HTMLSelectElement).value);
  }

  /** Why an included line cannot be saved, or null when it is valid. */
  protected rowError(row: ReviewRow): string | null {
    if (!row.include) return null;
    if (!row.itemId) return 'Escolha o item da lista.';
    const price = parseLocalizedPrice(row.price);
    if (!Number.isFinite(price) || price < 0.01 || price > MAX_UNIT_PRICE) {
      return 'Informe um preço maior que zero.';
    }
    if (this.duplicatedItemIds().has(row.itemId)) return 'Este item está em mais de uma linha.';
    return null;
  }

  /** Price already in the map for this line's item and the chosen supplier. */
  protected currentPrice(row: ReviewRow): string | null {
    if (!row.itemId || !this.supplierId()) return null;
    const existing = this.existingPrices().get(`${row.itemId}:${this.supplierId()}`);
    return existing ? currencyFormatter.format(existing.unitPrice) : null;
  }

  protected formatCurrency(value: number): string {
    return currencyFormatter.format(value);
  }

  protected describeItem(item: AiQuoteReaderItem): string {
    return `${item.name} (${item.unit})`;
  }

  protected confirm(): void {
    if (!this.canConfirm()) return;
    const supplier = this.suppliers().find((candidate) => candidate.id === this.supplierId());
    if (!supplier) return;

    const quotes = this.includedRows().map((row) => ({
      shoppingItemId: row.itemId,
      unitPrice: Math.round(parseLocalizedPrice(row.price) * 100) / 100,
    }));

    this.isSaving.set(true);
    this.error.set(null);
    this.aiService
      .applyQuotes(this.listId(), { supplierId: supplier.id, quotes })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.isSaving.set(false);
          this.step.set('closed');
          this.applied.emit({
            supplierName: supplier.name,
            created: result.created,
            updated: result.updated,
          });
        },
        error: () => {
          this.isSaving.set(false);
          this.error.set(
            'Não foi possível gravar os preços. Nada foi salvo; confira as linhas e tente novamente.',
          );
        },
      });
  }

  private showReview(response: QuoteExtractionResponseDto): void {
    const knownItemIds = new Set(this.items().map((item) => item.id));
    this.rows.set(
      response.lines.map((line) => {
        const itemId =
          line.shoppingItemId && knownItemIds.has(line.shoppingItemId) ? line.shoppingItemId : '';
        return {
          lineNumber: line.lineNumber,
          description: line.supplierDescription,
          supplierUnit: line.supplierUnit,
          supplierUnitPrice: line.supplierUnitPrice,
          confidence: itemId ? line.confidence : 'none',
          note: line.note,
          itemId,
          price: formatPriceInput(line.unitPrice),
          // Low-confidence and unmatched lines start unchecked: the buyer has to opt in.
          include: itemId !== '' && (line.confidence === 'high' || line.confidence === 'medium'),
        };
      }),
    );
    this.generalConditions.set(response.generalConditions);
    this.discardedLines.set(response.discardedLines);
    // With a single supplier there is nothing to choose; otherwise the buyer must pick one.
    this.supplierId.set(this.suppliers().length === 1 ? this.suppliers()[0].id : '');
    this.step.set('review');
  }

  private updateRow(row: ReviewRow, changes: Partial<ReviewRow>): void {
    this.rows.update((rows) =>
      rows.map((candidate) =>
        candidate.lineNumber === row.lineNumber ? { ...candidate, ...changes } : candidate,
      ),
    );
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
