import { Injectable, inject, signal } from '@angular/core';
import { forkJoin, map } from 'rxjs';

import { PurchaseOrderService } from '../../purchase-orders/data-access/purchase-order.service';
import { QuotationRequestService } from '../../quotation-requests/data-access/quotation-request.service';
import { SupplierService } from '../../suppliers/data-access/supplier.service';
import { OnboardingRemoteProgress } from '../models/onboarding-step.model';
import { OnboardingInvalidation } from './onboarding-invalidation.interceptor';

interface LoadedProgress {
  readonly userId: string;
  readonly value: OnboardingRemoteProgress;
}

/**
 * Estado dos primeiros passos que não vem da tela de listas: fornecedores,
 * solicitações e pedidos. Carrega uma vez por usuário e só recarrega depois que
 * alguma escrita nesses recursos invalida o estado (ver o interceptor).
 */
@Injectable({ providedIn: 'root' })
export class OnboardingService {
  private readonly supplierService = inject(SupplierService);
  private readonly quotationRequestService = inject(QuotationRequestService);
  private readonly purchaseOrderService = inject(PurchaseOrderService);
  private readonly invalidation = inject(OnboardingInvalidation);

  private readonly loaded = signal<LoadedProgress | null>(null);
  private readonly failedUserId = signal<string | null>(null);
  private readonly loadingUserId = signal<string | null>(null);
  private readonly loadedRevision = signal(-1);

  readonly progress = this.loaded.asReadonly();
  readonly failedFor = this.failedUserId.asReadonly();

  ensureLoaded(userId: string): void {
    if (this.loadingUserId() === userId) {
      return;
    }

    const hasResultForUser =
      this.loaded()?.userId === userId || this.failedUserId() === userId;
    if (hasResultForUser && this.loadedRevision() === this.invalidation.revision()) {
      return;
    }

    const revision = this.invalidation.revision();
    this.loadingUserId.set(userId);

    forkJoin({
      hasSupplier: this.supplierService.getAll().pipe(map((items) => items.length > 0)),
      hasQuotationRequest: this.quotationRequestService
        .getAll()
        .pipe(map((items) => items.length > 0)),
      hasPurchaseOrder: this.purchaseOrderService.getAll().pipe(map((items) => items.length > 0)),
    }).subscribe({
      next: (value) => {
        if (this.loadingUserId() !== userId) return;
        this.loaded.set({ userId, value });
        this.failedUserId.set(null);
        this.finish(revision);
      },
      error: () => {
        if (this.loadingUserId() !== userId) return;
        this.loaded.set(null);
        this.failedUserId.set(userId);
        this.finish(revision);
      },
    });
  }

  /** Marca o estado como desatualizado; a próxima leitura busca de novo. */
  invalidate(): void {
    this.invalidation.invalidate();
  }

  private finish(revision: number): void {
    this.loadedRevision.set(revision);
    this.loadingUserId.set(null);
  }
}
