import {
  Component, OnInit, OnDestroy, inject, ChangeDetectorRef, ChangeDetectionStrategy
} from '@angular/core';
import { Router } from '@angular/router';
import { DecimalPipe, CurrencyPipe } from '@angular/common';
import {
  Subject, BehaviorSubject, merge,
  switchMap, takeUntil, debounceTime, distinctUntilChanged, tap, map
} from 'rxjs';

import { ProductService } from '../../services/product.service';
import { ToastService }   from '../../services/toast.service';
import { Product, PagedResult, ProductQuery } from '../../models/models';
import { SearchBarComponent }      from '../search-bar/search-bar.component';
import { CategoryFilterComponent } from '../category-filter/category-filter.component';
import { ConfirmDialogComponent }  from '../confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Default,
  imports: [
    SearchBarComponent,
    CategoryFilterComponent,
    ConfirmDialogComponent,
    DecimalPipe,
    CurrencyPipe
  ],
  templateUrl: './product-list.component.html',
  styleUrls: ['./product-list.component.scss']
})
export class ProductListComponent implements OnInit, OnDestroy {

  private productService = inject(ProductService);
  private toastService   = inject(ToastService);
  private router         = inject(Router);
  private cdr            = inject(ChangeDetectorRef);
  private destroy$       = new Subject<void>();

  products: Product[]           = [];
  totalCount                    = 0;
  totalPages                    = 0;
  loading                       = false;
  error: string | null          = null;
  successMessage: string | null = null;

  showDeleteDialog              = false;
  productToDelete: Product | null = null;

  query: ProductQuery = { page: 1, pageSize: 12, sortBy: 'name' };

  // Query changes — go through distinctUntilChanged to avoid duplicate API calls
  private querySubject   = new BehaviorSubject<ProductQuery>(this.query);

  // Force-refresh — bypasses distinctUntilChanged, used after delete
  private refreshSubject = new Subject<void>();

  get deleteMessage(): string {
    return this.productToDelete
      ? 'Are you sure you want to delete "' + this.productToDelete.name + '"? This cannot be undone.'
      : '';
  }

  ngOnInit(): void {
    // Subscribe to the toast$ stream — show any message that arrives
    this.toastService.toast$
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ message }) => this.showToast(message));

    // Consume any pending cross-route toast set by the product form
    this.toastService.consumePending();

    // Stream 1 — filter/sort/page changes (deduped)
    const query$ = this.querySubject.pipe(
      debounceTime(0),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
      map(q => q)
    );

    // Stream 2 — forced refresh after delete (always fires)
    const refresh$ = this.refreshSubject.pipe(
      map(() => this.query)
    );

    merge(query$, refresh$).pipe(
      tap(() => {
        this.loading = true;
        this.error   = null;
        this.cdr.markForCheck();
      }),
      switchMap(q => this.productService.getProducts(q)),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (result: PagedResult<Product>) => {
        this.products   = result.items;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;

        // If current page is empty but items still exist, go back to page 1
        if (this.products.length === 0 && this.totalCount > 0 && this.query.page! > 1) {
          this.query = { ...this.query, page: 1 };
          this.querySubject.next(this.query);
          return;
        }

        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err: any) => {
        this.error   = err?.friendlyMessage ?? 'Failed to load products.';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private updateQuery(partial: Partial<ProductQuery>): void {
    this.query = { ...this.query, ...partial, page: 1 };
    this.querySubject.next(this.query);
  }

  onSearchChange(search: string): void      { this.updateQuery({ search: search || undefined }); }
  onCategoryChange(id: number | null): void { this.updateQuery({ categoryId: id ?? undefined }); }

  onStockChange(event: Event): void {
    const val = (event.target as HTMLSelectElement).value;
    this.updateQuery({ inStock: val === '' ? undefined : val === 'true' });
  }

  onSortChange(event: Event): void {
    this.updateQuery({ sortBy: (event.target as HTMLSelectElement).value });
  }

  changePage(page: number): void {
    this.query = { ...this.query, page };
    this.querySubject.next(this.query);
  }

  addProduct(): void            { this.router.navigate(['/products/new']); }
  editProduct(p: Product): void { this.router.navigate(['/products', p.id, 'edit']); }
  reload(): void                { this.refreshSubject.next(); }

  promptDelete(product: Product): void {
    this.productToDelete  = product;
    this.showDeleteDialog = true;
    this.cdr.markForCheck();
  }

  cancelDelete(): void {
    this.productToDelete  = null;
    this.showDeleteDialog = false;
    this.cdr.markForCheck();
  }

  confirmDelete(): void {
    if (!this.productToDelete) return;
    const { id, name } = this.productToDelete;

    // Optimistic removal — card disappears instantly
    this.products         = this.products.filter(p => p.id !== id);
    this.totalCount       = Math.max(0, this.totalCount - 1);
    this.showDeleteDialog = false;
    this.productToDelete  = null;
    this.cdr.markForCheck();

    this.productService.deleteProduct(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toastService.show('"' + name + '" deleted successfully.');
          this.refreshSubject.next();
        },
        error: (err: any) => {
          this.error = err?.friendlyMessage ?? 'Failed to delete product.';
          this.refreshSubject.next();
          this.cdr.markForCheck();
        }
      });
  }

  private showToast(message: string): void {
    this.successMessage = message;
    this.cdr.markForCheck();
    setTimeout(() => {
      this.successMessage = null;
      this.cdr.markForCheck();
    }, 3500);
  }
}
