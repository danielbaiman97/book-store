import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Book } from '../models/book.model';
import { environment } from '../../../../environments/environment';

export interface ListParams {
  search?: string;
  category?: string;
  sortBy?: 'title' | 'author' | 'year' | 'price' | 'isbn';
  order?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}
export interface ListResponse<T> { total: number; items: T[]; }

@Injectable({ providedIn: 'root' })
export class BooksService {
  private http = inject(HttpClient);
  private base = `${environment.apiBaseUrl}/books`;

  list(params: ListParams = {}): Observable<ListResponse<Book>> {
    const httpParams = new HttpParams({ fromObject: {
      ...(params.search ? { search: params.search } : {}),
      ...(params.category ? { category: params.category } : {}),
      ...(params.sortBy ? { sortBy: params.sortBy } : {}),
      ...(params.order ? { order: params.order } : {}),
      ...(params.page ? { page: params.page } : {}),
      ...(params.pageSize ? { pageSize: params.pageSize } : {}),
    }});
    return this.http.get<ListResponse<Book>>(this.base, { params: httpParams });
  }

  categories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/categories`);
  }
  checkIsbnExists(isbn: string): Observable<boolean> {
    return this.http.get<{ exists: boolean }>(`/api/books/exists`, { params: { isbn } })
      .pipe(map(r => !!r.exists));
  }
  get(isbn: string): Observable<Book> { return this.http.get<Book>(`${this.base}/${isbn}`); }
  create(payload: Partial<Book>): Observable<Book> { return this.http.post<Book>(this.base, payload); }
  update(isbn: string, payload: Partial<Book>): Observable<Book> { return this.http.put<Book>(`${this.base}/${isbn}`, payload); }
  remove(isbn: string) { return this.http.delete(`${this.base}/${isbn}`); }

  openHtmlReport() { window.open(`${this.base}/report/html`, '_blank'); }
  downloadCsv() { window.open(`${this.base}/export/csv`, '_self'); }
}
