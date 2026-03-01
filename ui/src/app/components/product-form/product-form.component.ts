import {
  Component, OnInit, inject, DestroyRef, ChangeDetectorRef, ChangeDetectionStrategy
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AsyncPipe } from '@angular/common';
import { Observable, tap, finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ProductService }  from '../../services/product.service';
import { CategoryService } from '../../services/category.service';
import { ToastService }    from '../../services/toast.service';
import { Product, Category } from '../../models/models';

@Component({
  selector: 'app-product-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Default,
  imports: [ReactiveFormsModule, AsyncPipe],
  templateUrl: './product-form.component.html',
  styleUrls: ['./product-form.component.scss']
})
export class ProductFormComponent implements OnInit {

  private fb             = inject(FormBuilder);
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private productService = inject(ProductService);
  private categoryService= inject(CategoryService);
  private toastService   = inject(ToastService);
  private destroyRef     = inject(DestroyRef);
  private cdr            = inject(ChangeDetectorRef);

  form = this.fb.nonNullable.group({
    name:        ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    sku:         ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9\-]+$/)]],
    price:       [0,  [Validators.required, Validators.min(0)]],
    quantity:    [0,  [Validators.required, Validators.min(0)]],
    categoryId:  ['', Validators.required]
  });

  categories$: Observable<Category[]> = this.categoryService.getCategories();

  isEditMode              = false;
  productId: number | null = null;
  loading                 = false;
  submitting              = false;
  loadError: string | null   = null;
  submitError: string | null = null;

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        tap(params => {
          const id = params.get('id');
          if (!id) return;
          this.isEditMode = true;
          this.productId  = Number(id);
          this.loadProduct(this.productId);
        })
      )
      .subscribe();
  }

  // ── Load ───────────────────────────────────────────────────────────────────

  private loadProduct(id: number): void {
    this.loading = true;
    this.form.disable();
    this.loadError = null;
    this.cdr.markForCheck();

    this.productService.getProduct(id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.loading = false;
          this.form.enable();
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next:  (p: Product) => { this.patchForm(p); this.cdr.markForCheck(); },
        error: (err: any) => {
          this.loadError = err?.friendlyMessage ?? 'Failed to load product.';
          this.cdr.markForCheck();
        }
      });
  }

  private patchForm(product: Product): void {
    this.form.patchValue({
      name:        product.name,
      description: product.description ?? '',
      sku:         product.sku,
      price:       product.price,
      quantity:    product.quantity,
      categoryId:  String(product.categoryId)
    });
  }

  // ── Submit ─────────────────────────────────────────────────────────────────

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }

    this.submitting  = true;
    this.submitError = null;
    this.cdr.markForCheck();

    const payload  = this.buildPayload();
    const request$ = this.isEditMode
      ? this.productService.updateProduct(this.productId!, payload)
      : this.productService.createProduct(payload);

    request$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => { this.submitting = false; this.cdr.markForCheck(); })
      )
      .subscribe({
        next: (saved: Product) => {
          // Queue toast for the product list page, then navigate immediately
          const msg = this.isEditMode
            ? '"' + saved.name + '" updated successfully.'
            : '"' + saved.name + '" added successfully.';
          this.toastService.setPending(msg);
          this.router.navigate(['/products']);
        },
        error: (err: any) => {
          this.submitError = err?.friendlyMessage ?? 'Failed to save product.';
          this.cdr.markForCheck();
        }
      });
  }

  private buildPayload() {
    const v = this.form.getRawValue();
    return {
      name:        v.name.trim(),
      description: v.description?.trim() || null,
      sku:         v.sku.trim(),
      price:       Number(v.price),
      quantity:    Number(v.quantity),
      categoryId:  Number(v.categoryId)
    };
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  getError(controlName: keyof typeof this.form.controls, error: string): boolean {
    const c = this.form.controls[controlName];
    return !!(c.touched && c.errors?.[error]);
  }

  goBack(): void { this.router.navigate(['/products']); }
}
