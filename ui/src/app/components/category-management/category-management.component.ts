import {
  Component, OnInit, OnDestroy, inject,
  ChangeDetectorRef, ChangeDetectionStrategy
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AsyncPipe, NgTemplateOutlet } from '@angular/common';
import { Observable, Subject, takeUntil } from 'rxjs';
import { CategoryService }    from '../../services/category.service';
import { ToastService }       from '../../services/toast.service';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';
import { Category, CategoryTreeNode } from '../../models/models';

@Component({
  selector: 'app-category-management',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Default,
  imports: [ReactiveFormsModule, AsyncPipe, NgTemplateOutlet, ConfirmDialogComponent],
  templateUrl: './category-management.component.html',
  styleUrls: ['./category-management.component.scss']
})
export class CategoryManagementComponent implements OnInit, OnDestroy {

  private categoryService = inject(CategoryService);
  private toastService    = inject(ToastService);
  private fb              = inject(FormBuilder);
  private cdr             = inject(ChangeDetectorRef);
  private destroy$        = new Subject<void>();

  form!: FormGroup;
  categories$!: Observable<Category[]>;
  treeNodes: CategoryTreeNode[] = [];

  isEditMode       = false;
  editingId: number | null = null;

  treeLoading               = false;
  treeError: string | null  = null;
  submitting                = false;
  submitError: string | null = null;
  toastMessage: string | null = null;

  showDeleteDialog           = false;
  categoryToDelete: Category | null = null;
  deleteError: string | null = null;

  get deleteMessage(): string {
    return this.categoryToDelete
      ? `Are you sure you want to delete "${this.categoryToDelete.name}"? This cannot be undone.`
      : '';
  }

  ngOnInit(): void {
    this.toastService.toast$
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ message }) => {
        this.toastMessage = message;
        this.cdr.detectChanges();
        setTimeout(() => { this.toastMessage = null; this.cdr.detectChanges(); }, 3500);
      });

    this.buildForm();
    this.refreshAll();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  private buildForm(): void {
    this.form = this.fb.group({
      name:             ['', Validators.required],
      description:      [''],
      parentCategoryId: [null]
    });
  }

  /**
   * Refresh both the flat categories list (for the dropdown) and the tree.
   * Always invalidates the cache first so stale data never shows.
   */
  private refreshAll(): void {
    this.categoryService.invalidateCache();
    this.categories$ = this.categoryService.getCategories();
    this.loadTree();
  }

  // ── Tree ───────────────────────────────────────────────────────────────────

  loadTree(): void {
    this.treeLoading = true;
    this.treeError   = null;
    this.cdr.detectChanges(); // show spinner immediately

    this.categoryService.getCategoryTree().subscribe({
      next: (nodes) => {
        this.treeNodes   = [...nodes];
        this.treeLoading = false;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.treeError   = err?.friendlyMessage ?? 'Failed to load categories.';
        this.treeLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ── Submit (add or edit) ───────────────────────────────────────────────────

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.cdr.detectChanges();
      return;
    }

    this.submitting  = true;
    this.submitError = null;
    this.cdr.detectChanges();

    const v = this.form.value;
    const request = {
      name:             v.name,
      description:      v.description || null,
      parentCategoryId: v.parentCategoryId ? Number(v.parentCategoryId) : null
    };

    const op$ = this.isEditMode
      ? this.categoryService.updateCategory(this.editingId!, request)
      : this.categoryService.createCategory(request);

    op$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (cat: Category) => {
        const msg = this.isEditMode
          ? `"${cat.name}" updated successfully.`
          : `"${cat.name}" added successfully.`;
        this.submitting = false;
        this.resetForm();
        this.refreshAll();        // invalidate cache + reload tree & dropdown
        this.toastService.show(msg);
      },
      error: (err: any) => {
        this.submitError = err?.friendlyMessage ?? (
          this.isEditMode ? 'Failed to update category.' : 'Failed to add category.'
        );
        this.submitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ── Edit ───────────────────────────────────────────────────────────────────

  startEdit(node: CategoryTreeNode): void {
    this.isEditMode  = true;
    this.editingId   = node.id;
    this.submitError = null;
    this.form.patchValue({
      name:             node.name,
      description:      node.description ?? '',
      parentCategoryId: node.parentCategoryId
    });
    this.cdr.detectChanges();
    document.querySelector('.panel-form')?.scrollIntoView({ behavior: 'smooth' });
  }

  cancelEdit(): void { this.resetForm(); }

  private resetForm(): void {
    this.isEditMode  = false;
    this.editingId   = null;
    this.submitError = null;
    this.form.reset({ parentCategoryId: null });
    this.cdr.detectChanges();
  }

  // ── Delete ─────────────────────────────────────────────────────────────────

  promptDelete(node: CategoryTreeNode): void {
    this.categoryToDelete = {
      id: node.id, name: node.name,
      description: node.description, parentCategoryId: node.parentCategoryId
    };
    this.showDeleteDialog = true;
    this.deleteError      = null;
    this.cdr.detectChanges();
  }

  cancelDelete(): void {
    this.categoryToDelete = null;
    this.showDeleteDialog = false;
    this.cdr.detectChanges();
  }

  confirmDelete(): void {
    if (!this.categoryToDelete) return;
    const { id, name } = this.categoryToDelete;
    this.showDeleteDialog = false;
    this.categoryToDelete = null;
    this.cdr.detectChanges();

    this.categoryService.deleteCategory(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.refreshAll();
          this.toastService.show(`"${name}" deleted successfully.`);
        },
        error: (err: any) => {
          this.deleteError = err?.friendlyMessage ?? 'Failed to delete category.';
          this.cdr.detectChanges();
        }
      });
  }
}
