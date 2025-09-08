import { Component, ChangeDetectionStrategy, EventEmitter, Input, Output, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, startWith, distinctUntilChanged } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  standalone: true,
  selector: 'app-books-filters',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  styleUrls: ['./books-filters.component.css'],
  templateUrl: './books-filters.component.html',
})
export class BooksFiltersComponent implements OnInit {
  @Input() categories: string[] = [];
  @Input() search = '';
  @Input() category = '';
  @Output() changed = new EventEmitter<{ search: string; category: string }>();

  private destroyRef = inject(DestroyRef);

  searchCtrl = new FormControl<string>('', { nonNullable: true });
  categoryCtrl = new FormControl<string>('', { nonNullable: true });

  ngOnInit(): void {
    this.searchCtrl.setValue(this.search ?? '');
    this.categoryCtrl.setValue(this.category ?? '');

    this.searchCtrl.valueChanges
      .pipe(
        startWith(this.searchCtrl.value),
        debounceTime(250),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(v => this.changed.emit({ search: v, category: this.categoryCtrl.value }));

    this.categoryCtrl.valueChanges
      .pipe(
        startWith(this.categoryCtrl.value),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(v => this.changed.emit({ search: this.searchCtrl.value, category: v }));
  }
}