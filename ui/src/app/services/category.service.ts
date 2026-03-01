import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError, shareReplay } from 'rxjs';
import { Category, CategoryTreeNode, CreateCategoryRequest, UpdateCategoryRequest } from '../models/models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/categories`;

  private categories$: Observable<Category[]> | null = null;

  getCategories(): Observable<Category[]> {
    if (!this.categories$) {
      this.categories$ = this.http.get<Category[]>(this.baseUrl).pipe(
        shareReplay(1),
        catchError(err => {
          this.categories$ = null;
          return throwError(() => err);
        })
      );
    }
    return this.categories$;
  }

  getCategoryTree(): Observable<CategoryTreeNode[]> {
    return this.http.get<CategoryTreeNode[]>(`${this.baseUrl}/tree`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  getCategory(id: number): Observable<Category> {
    return this.http.get<Category>(`${this.baseUrl}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  createCategory(request: CreateCategoryRequest): Observable<Category> {
    return this.http.post<Category>(this.baseUrl, request).pipe(
      catchError(err => {
        this.categories$ = null;
        return throwError(() => err);
      })
    );
  }

  updateCategory(id: number, request: UpdateCategoryRequest): Observable<Category> {
    return this.http.put<Category>(`${this.baseUrl}/${id}`, request).pipe(
      catchError(err => {
        this.categories$ = null;
        return throwError(() => err);
      })
    );
  }

  deleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
      catchError(err => {
        this.categories$ = null;
        return throwError(() => err);
      })
    );
  }

  invalidateCache(): void {
    this.categories$ = null;
  }
}
