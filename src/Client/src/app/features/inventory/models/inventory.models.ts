export interface InventoryItem {
  id: string;
  name: string;
  description: string;
  category: string;
  karat: string;
  weight: number;
  quantity: number;
  costPrice: number;
  sellingPrice: number;
  totalValue: number;
  status: 'in_stock' | 'low_stock' | 'out_of_stock';
  imageUrl?: string;
  createdAt: string;
  updatedAt: string;
}

export interface InventoryFilterParams {
  search?: string;
  category?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}
