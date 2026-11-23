import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { buildApiUrl } from '../../../core/api/api-url';
import {
  PublicSupplierQuotationDto,
  SubmitSupplierQuotationDto,
} from '../dtos/supplier-portal.dto';

/** Acesso do fornecedor à própria cotação. O token do link é a única credencial. */
@Injectable({ providedIn: 'root' })
export class SupplierPortalService {
  private readonly http = inject(HttpClient);

  /** Abrir a cotação também registra que o fornecedor a viu. */
  get(token: string): Observable<PublicSupplierQuotationDto> {
    return this.http.get<PublicSupplierQuotationDto>(
      buildApiUrl(`/api/public/quotations/${encodeURIComponent(token)}`),
    );
  }

  respond(
    token: string,
    response: SubmitSupplierQuotationDto,
  ): Observable<PublicSupplierQuotationDto> {
    return this.http.post<PublicSupplierQuotationDto>(
      buildApiUrl(`/api/public/quotations/${encodeURIComponent(token)}/response`),
      response,
    );
  }
}
