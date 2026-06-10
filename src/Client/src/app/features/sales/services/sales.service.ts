import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SaleInvoice, CreateSaleRequest, SalesFilterParams } from '../models/sales.models';

@Injectable({ providedIn: 'root' })
export class SalesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/sales';

  getInvoices(params?: SalesFilterParams): Observable<{ invoices: SaleInvoice[]; total: number }> {
    return this.http.get<{ invoices: SaleInvoice[]; total: number }>(this.apiUrl, { params: params as any });
  }
  getInvoice(id: string): Observable<SaleInvoice> {
    return this.http.get<SaleInvoice>(`${this.apiUrl}/${id}`);
  }
  createInvoice(data: CreateSaleRequest): Observable<SaleInvoice> {
    return this.http.post<SaleInvoice>(this.apiUrl, data);
  }
  deleteInvoice(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
