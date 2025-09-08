import { Component, ChangeDetectionStrategy, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSortModule, Sort } from '@angular/material/sort';
import { Book } from '../../models/book.model';

export type SortEvent = { sortBy: 'title'|'author'|'year'|'price'|'isbn'; direction: 'asc'|'desc' };

@Component({
  standalone: true,
  selector: 'app-books-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, MatTableModule, MatIconModule, MatButtonModule, MatSortModule],
  styleUrls: ['./books-table.component.css'],
  templateUrl: './books-table.component.html',
})
export class BooksTableComponent {
  @Input() items: Book[] = [];
  @Input() sortActive: 'title'|'author'|'year'|'price'|'isbn' = 'title';
  @Input() sortDirection: 'asc'|'desc' = 'asc';
  @Output() sortChange = new EventEmitter<SortEvent>();
  @Output() delete = new EventEmitter<string>();

  displayed = ['title','authors','category','year','price','actions'] as const;

  onSort(e: Sort) {
    const active = (e.active === 'authors') ? 'author' : (e.active as any);
    const dir = (e.direction || 'asc') as 'asc'|'desc';
    this.sortChange.emit({ sortBy: active, direction: dir });
  }
}
