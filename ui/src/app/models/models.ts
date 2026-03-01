// ---- Product ---------

export interface Product {
  id: number;
  name: string;
  description: string | null;
  sku: string;
  price: number;
  quantity: number;
  categoryId: number;
  categoryName: string | null;
  inStock: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductRequest {
  name: string;
  description: string | null;
  sku: string;
  price: number;
  quantity: number;
  categoryId: number;
}

export interface UpdateProductRequest extends CreateProductRequest {}

// ----- Category ------

export interface Category {
  id: number;
  name: string;
  description: string | null;
  parentCategoryId: number | null;
}

export interface CategoryTreeNode extends Category {
  children: CategoryTreeNode[];
}

export interface CreateCategoryRequest {
  name: string;
  description: string | null;
  parentCategoryId: number | null;
}

export interface UpdateCategoryRequest {
  name: string;
  description: string | null;
  parentCategoryId: number | null;
}

// ---Pagination -----

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// --- Query params ----

export interface ProductQuery {
  search?: string;
  categoryId?: number;
  minPrice?: number;
  maxPrice?: number;
  inStock?: boolean;
  sortBy?: string;
  sortDesc?: boolean;
  page?: number;
  pageSize?: number;
}

// --- API Error ---

export interface ApiError {
  error: string;
  correlationId?: string;
}
