import { Routes } from '@angular/router';
import { BooksPageComponent } from './features/books/pages/books-page/books-page.component';
import { BookFormPageComponent } from './features/books/pages/book-form-page/book-form-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'books' },
  { path: 'books', component: BooksPageComponent },
  { path: 'books/new', component: BookFormPageComponent },
  { path: 'books/:isbn', component: BookFormPageComponent },
  { path: '**', redirectTo: 'books' },
];
