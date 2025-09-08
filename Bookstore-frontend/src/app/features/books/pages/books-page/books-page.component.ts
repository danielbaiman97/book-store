import { Component, ChangeDetectionStrategy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatDialogModule } from '@angular/material/dialog';
import { Book } from '../../models/book.model';
import { BooksService } from '../../services/books.service';
import { BooksFiltersComponent } from '../../components/books-filters/books-filters.component';
import { BooksTableComponent, SortEvent } from '../../components/books-table/books-table.component';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog.component';

@Component({
  standalone: true,
  selector: 'app-books-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, RouterLink, MatButtonModule, MatIconModule, MatPaginatorModule, MatDialogModule,
    BooksFiltersComponent, BooksTableComponent
  ],
  styleUrls: ['./books-page.component.css'],
  templateUrl: './books-page.component.html',
})
export class BooksPageComponent implements OnInit {
  private svc = inject(BooksService);
  private dialog = inject(MatDialog);

  items = signal<Book[]>([]);
  total = signal(0);
  categories = signal<string[]>([]);

  search = signal('');
  category = signal('');
  sortBy = signal<'title'|'author'|'year'|'price'|'isbn'>('title');
  order = signal<'asc'|'desc'>('asc');
  pageIndex = signal(0);
  pageSize = signal(10);

  ngOnInit() {
    this.load();
    this.svc.categories().subscribe(cs => this.categories.set(cs));
  }

  private params() {
    return {
      search: this.search() || undefined,
      category: this.category() || undefined,
      sortBy: this.sortBy(),
      order: this.order(),
      page: this.pageIndex() + 1,
      pageSize: this.pageSize(),
    } as const;
  }

  load() {
    this.svc.list(this.params()).subscribe(res => {
      this.items.set(res.items);
      this.total.set(res.total);
    });
  }

  onFilters(e: { search: string; category: string }) {
    this.search.set(e.search);
    this.category.set(e.category);
    this.pageIndex.set(0);
    this.load();
  }

  onSort(e: SortEvent) {
    this.sortBy.set(e.sortBy);
    this.order.set(e.direction);
    this.pageIndex.set(0);
    this.load();
  }

  onPage(e: PageEvent) {
    this.pageIndex.set(e.pageIndex);
    this.pageSize.set(e.pageSize);
    this.load();
  }

  onDelete(isbn: string) {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Book', message: `Are you sure you want to delete ISBN ${isbn}?` }
    }).afterClosed().subscribe(ok => {
      if (!ok) return;
      this.svc.remove(isbn).subscribe(() => this.load());
    });
  }

  openHtmlReport() { this.svc.openHtmlReport(); }
  downloadCsv() { this.svc.downloadCsv(); }
}
