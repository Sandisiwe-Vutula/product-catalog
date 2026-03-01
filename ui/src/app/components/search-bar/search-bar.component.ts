import {
  Component, EventEmitter, Input, Output,
  OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Default,
  imports: [CommonModule, FormsModule],
  templateUrl: './search-bar.component.html',
  styleUrls: ['./search-bar.component.scss']
})
export class SearchBarComponent implements OnInit, OnDestroy {

  @Input() placeholder  = 'Search products...';
  @Input() initialValue = '';
  @Output() searchChange = new EventEmitter<string>();

  searchValue = '';

  private cdr      = inject(ChangeDetectorRef);
  private subject$ = new Subject<string>();
  private destroy$ = new Subject<void>();

  ngOnInit(): void {
    this.searchValue = this.initialValue;

    // Single debounce here - 300ms is fast enough to feel responsive
    this.subject$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(value => {
      this.searchChange.emit(value);
      this.cdr.markForCheck();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onInputChange(value: string): void {
    this.searchValue = value;
    this.subject$.next(value);
    this.cdr.markForCheck();
  }

  clear(): void {
    this.searchValue = '';
    this.subject$.next('');
    this.cdr.markForCheck();
  }
}
