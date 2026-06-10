export interface SaleInvoice {
  id: string;
  invoiceNumber: string;
  customerName: string;
  customerPhone?: string;
  items: SaleItem[];
  totalAmount: number;
  discount: number;
  tax: number;
  finalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  status: 'completed' | 'pending' | 'cancelled';
  paymentMethod: 'cash' | 'card' | 'bank_transfer' | 'credit';
  notes?: string;
  createdAt: string;
}

export interface SaleItem {
  productId: string;
  productName: string;
  karat: string;
  weight: number;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface CreateSaleRequest {
  customerName: string;
  customerPhone?: string;
  items: SaleItem[];
  discount: number;
  paymentMethod: string;
  paidAmount: number;
  notes?: string;
}

export interface SalesFilterParams {
  search?: string;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}
