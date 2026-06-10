import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardStats, SalesChartData, ExpenseChartData, GoldKaratData, RecentTransaction, RecentActivity } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/dashboard';

  getStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.apiUrl}/stats`);
  }
  getSalesChart(): Observable<SalesChartData[]> {
    return this.http.get<SalesChartData[]>(`${this.apiUrl}/sales-chart`);
  }
  getExpenseChart(): Observable<ExpenseChartData[]> {
    return this.http.get<ExpenseChartData[]>(`${this.apiUrl}/expense-chart`);
  }
  getGoldKarat(): Observable<GoldKaratData[]> {
    return this.http.get<GoldKaratData[]>(`${this.apiUrl}/gold-karat`);
  }
  getRecentTransactions(): Observable<RecentTransaction[]> {
    return this.http.get<RecentTransaction[]>(`${this.apiUrl}/recent-transactions`);
  }
  getRecentActivities(): Observable<RecentActivity[]> {
    return this.http.get<RecentActivity[]>(`${this.apiUrl}/recent-activities`);
  }
}
