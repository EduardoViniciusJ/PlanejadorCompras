import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { buildApiUrl } from '../../../core/api/api-url';
import { ShoppingListReportFile } from '../../reports/models/shopping-list-report-file.model';
import { mapReportFileResponse } from '../../reports/models/report-file-response.mapper';
import {
  CreateQuotationRequestDto,
  QuotationRequestDetailDto,
  QuotationDeliveryOptionsDto,
  QuotationRequestSummaryDto,
  SupplierQuotationDto,
} from '../dtos/quotation-request.dto';

@Injectable({ providedIn: 'root' })
export class QuotationRequestService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<readonly QuotationRequestSummaryDto[]> {
    return this.http.get<readonly QuotationRequestSummaryDto[]>(
      buildApiUrl('/api/quotation-requests'),
    );
  }

  getById(id: string): Observable<QuotationRequestDetailDto> {
    return this.http.get<QuotationRequestDetailDto>(
      buildApiUrl(`/api/quotation-requests/${id}`),
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(buildApiUrl(`/api/quotation-requests/${id}`));
  }

  create(
    shoppingListId: string,
    request: CreateQuotationRequestDto,
  ): Observable<QuotationRequestDetailDto> {
    return this.http.post<QuotationRequestDetailDto>(
      buildApiUrl(`/api/shopping-lists/${shoppingListId}/quotation-requests`),
      request,
    );
  }

  /** Diz se o envio por e-mail está configurado no servidor. */
  getDeliveryOptions(): Observable<QuotationDeliveryOptionsDto> {
    return this.http.get<QuotationDeliveryOptionsDto>(
      buildApiUrl('/api/quotation-requests/delivery-options'),
    );
  }

  /** Gera um link novo para o fornecedor (o anterior para de funcionar). */
  renewSupplierLink(
    requestId: string,
    supplierQuotationId: string,
    sendEmail = false,
  ): Observable<SupplierQuotationDto> {
    return this.http.post<SupplierQuotationDto>(
      buildApiUrl(
        `/api/quotation-requests/${requestId}/supplier-quotations/${supplierQuotationId}/link`,
      ),
      { sendEmail },
    );
  }

  revokeSupplierLink(
    requestId: string,
    supplierQuotationId: string,
  ): Observable<SupplierQuotationDto> {
    return this.http.post<SupplierQuotationDto>(
      buildApiUrl(
        `/api/quotation-requests/${requestId}/supplier-quotations/${supplierQuotationId}/revoke`,
      ),
      null,
    );
  }

  downloadPdf(id: string): Observable<ShoppingListReportFile> {
    return this.http
      .get(buildApiUrl(`/api/quotation-requests/${id}/pdf`), {
        observe: 'response',
        responseType: 'blob',
      })
      .pipe(
        map((response) =>
          mapReportFileResponse(response, `solicitacao-cotacao-${id}.pdf`),
        ),
      );
  }
}
