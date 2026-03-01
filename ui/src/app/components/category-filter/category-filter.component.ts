import {
  Component, EventEmitter, Input, Output, OnInit,
  inject, ChangeDetectorRef, ChangeDetectionStrategy
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AsyncPipe } from '@angular/common';
import { Observable } from 'rxjs';
import { Category } from '../../models/models';
import { CategoryService } from '../../services/category.service';

@Component({
  selector: 'app-category-filter',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Default,
  imports: [FormsModule, AsyncPipe],
  templateUrl: './category-filter.component.html',
  styleUrls: ['./category-filter.component.scss']
})
export class CategoryFilterComponent implements OnInit {

  @Input() selectedCategoryId: number | null = null;
  @Output() categoryChange = new EventEmitter<number | null>();

  selectedId: number | null = null;
  categories$!: Observable<Category[]>;

  private categoryService = inject(CategoryService);
  private cdr             = inject(ChangeDetectorRef);

  ngOnInit(): void {
    this.selectedId  = this.selectedCategoryId;
    this.categories$ = this.categoryService.getCategories();
    this.cdr.markForCheck();
  }

  onChange(value: number | null): void {
    const emitVal = value ? Number(value) : null;
    this.categoryChange.emit(emitVal);
    this.cdr.markForCheck();
  }
}
