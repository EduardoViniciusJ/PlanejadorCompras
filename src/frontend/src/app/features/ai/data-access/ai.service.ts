import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { buildApiUrl } from '../../../core/api/api-url';
import {
  AiStatusDto,
  QuoteBatchRequestDto,
  QuoteBatchResponseDto,
  QuoteExtractionResponseDto,
  ShoppingListDraftResponseDto,
} from '../dtos/ai.dto';

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly http = inject(HttpClient);

  /** Whether AI is configured on the server and how much of the monthly limit was used. */
  getStatus(): Observable<AiStatusDto> {
    return this.http.get<AiStatusDto>(buildApiUrl('/api/ai/status'));
  }

  /** Reads a supplier's answer. Nothing is saved: the result is only a proposal to review. */
  extractQuote(
    shoppingListId: string,
    text: string,
    file: File | null,
  ): Observable<QuoteExtractionResponseDto> {
    return this.http.post<QuoteExtractionResponseDto>(
      buildApiUrl(`/api/shopping-lists/${shoppingListId}/ai/quote-extraction`),
      buildSourceForm(text, file),
    );
  }

  /** Saves the prices the buyer confirmed in the review. */
  applyQuotes(
    shoppingListId: string,
    request: QuoteBatchRequestDto,
  ): Observable<QuoteBatchResponseDto> {
    return this.http.post<QuoteBatchResponseDto>(
      buildApiUrl(`/api/shopping-lists/${shoppingListId}/quotes/batch`),
      request,
    );
  }

  /** Proposes list items from a description or a photo. Nothing is created. */
  draftShoppingList(text: string, file: File | null): Observable<ShoppingListDraftResponseDto> {
    return this.http.post<ShoppingListDraftResponseDto>(
      buildApiUrl('/api/ai/shopping-list-draft'),
      buildSourceForm(text, file),
    );
  }
}

function buildSourceForm(text: string, file: File | null): FormData {
  const form = new FormData();
  const trimmed = text.trim();
  if (trimmed) form.append('text', trimmed);
  if (file) form.append('file', file, file.name);
  return form;
}
