import { Component, ChangeDetectionStrategy, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { shareReplay } from 'rxjs/operators';
import { Observable, of } from 'rxjs';
import { Book } from '../../models/book.model';
import { BooksService } from '../../services/books.service';
import { BookFormComponent, BookFormValue } from '../../components/book-form/book-form.component';

@Component({
  standalone: true,
  selector: 'app-book-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatCardModule, RouterLink, BookFormComponent],
  styleUrls: ['./book-form-page.component.css'],
  templateUrl: './book-form-page.component.html',
})
export class BookFormPageComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private svc = inject(BooksService);

  editMode = false;
  book$!: Observable<Book | null>;

  ngOnInit(): void {
    const isbn = this.route.snapshot.paramMap.get('isbn');
    if (isbn && isbn !== 'new') {
      this.editMode = true;
      this.book$ = this.svc.get(isbn).pipe(shareReplay(1));
    } else {
      this.book$ = of(null);
    }
  }

  onSubmit(val: BookFormValue) {
    const payload = { ...val, authors: val.authors.map(a => a.trim()).filter(Boolean) };
    if (this.editMode) {
      this.svc.update(payload.isbn, payload).subscribe(() => this.router.navigate(['/books']));
    } else {
      this.svc.create(payload).subscribe(() => this.router.navigate(['/books']));
    }
  }
}
