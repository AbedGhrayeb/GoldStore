export interface DashboardStats {
  totalSales: number;
  totalSalesChange: number;
  totalSalesChangeDir: 'up' | 'down';
  inventoryValue: number;
  inventoryValueChange: number;
  inventoryValueChangeDir: 'up' | 'down';
  cashFlow: number;
  cashFlowChange: number;
  cashFlowChangeDir: 'up' | 'down';
  goldPriceToday: number;
  goldPriceChange: number;
  goldPriceChangeDir: 'up' | 'down';
}

export interface SalesChartData {
  month: string;
  sales: number;
  expenses: number;
}

export interface ExpenseChartData {
  category: string;
  amount: number;
}

export interface GoldKaratData {
  karat: string;
  weight: number;
  percentage: number;
}

export interface RecentTransaction {
  id: string;
  customerName: string;
  type: 'sale' | 'purchase' | 'expense';
  amount: number;
  date: string;
  status: 'completed' | 'pending' | 'cancelled';
}

export interface RecentActivity {
  id: string;
  user: string;
  action: string;
  timestamp: string;
}
