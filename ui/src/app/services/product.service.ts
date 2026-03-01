import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import {
  Product,
  CreateProductRequest,
  UpdateProductRequest,
  PagedResult,
  ProductQuery
} from '../models/models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/products`;

  getProducts(query: ProductQuery = {}): Observable<PagedResult<Product>> {
    let params = new HttpParams();

    if (query.search)                  params = params.set('search',     query.search);
    if (query.categoryId != null)      params = params.set('categoryId', query.categoryId.toString());
    if (query.minPrice != null)        params = params.set('minPrice',   query.minPrice.toString());
    if (query.maxPrice != null)        params = params.set('maxPrice',   query.maxPrice.toString());
    if (query.inStock != null)         params = params.set('inStock',    query.inStock.toString());
    if (query.sortBy)                  params = params.set('sortBy',     query.sortBy);
    if (query.sortDesc != null)        params = params.set('sortDesc',   query.sortDesc.toString());
    if (query.page != null)            params = params.set('page',       query.page.toString());
    if (query.pageSize != null)        params = params.set('pageSize',   query.pageSize.toString());

    return this.http.get<PagedResult<Product>>(this.baseUrl, { params }).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getProduct(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  createProduct(request: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, request).pipe(
      catchError(err => throwError(() => err))
    );
  }

  updateProduct(id: number, request: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, request).pipe(
      catchError(err => throwError(() => err))
    );
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }
}
