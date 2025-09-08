import {
  Component,
  ChangeDetectionStrategy,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  inject,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import {
  FormArray,
  FormControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  AsyncValidatorFn,
} from "@angular/forms";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { Book } from "../../models/book.model";
import { isbn13Validator } from "../../../../shared/validators/isbn13.validator";

import { BooksService } from "../../services/books.service";

import { of } from "rxjs";
import { catchError, map } from "rxjs/operators";

export type BookFormValue = {
  isbn: string;
  title: string;
  titleLang: string;
  authors: string[];
  category: string;
  cover?: string | null;
  year: number;
  price: number;
};

@Component({
  standalone: true,
  selector: "app-book-form",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  styleUrls: ["./book-form.component.css"],
  templateUrl: "./book-form.component.html",
})
export class BookFormComponent implements OnChanges {
  @Input() editMode = false;
  @Input() initial?: Book;
  @Output() submitForm = new EventEmitter<BookFormValue>();

  private fb = inject(NonNullableFormBuilder);
  private books = inject(BooksService);

  isbnCtrl = this.fb.control("", {
    validators: [
      Validators.required,
      Validators.pattern(/^\d{13}$/),
      isbn13Validator(),
    ],
    asyncValidators: [],
    updateOn: "blur" as const,
  });

  form = this.fb.group({
    isbn: this.isbnCtrl,
    title: this.fb.control("", [Validators.required]),
    titleLang: this.fb.control<"en" | string>("en"),
    authors: this.fb.array<FormControl<string>>([]),
    category: this.fb.control("", [Validators.required]),
    cover: this.fb.control<string | null>(null),
    year: this.fb.control(0, [Validators.min(0), Validators.max(9999)]),
    price: this.fb.control(0, [Validators.min(0)]),
  });

  get authors() {
    return this.form.controls.authors as FormArray<FormControl<string>>;
  }

  ngOnChanges(): void {
    if (this.initial) this.patch(this.initial);
    if (!this.initial && this.authors.length === 0) this.addAuthor();

    this.isbnCtrl.setAsyncValidators(this.isbnUniqueValidator());
    this.isbnCtrl.updateValueAndValidity({ emitEvent: false });
  }

  private patch(b: Book) {
    this.form.patchValue({
      isbn: b.isbn,
      title: b.title,
      titleLang: b.titleLang ?? "en",
      category: b.category,
      cover: b.cover ?? null,
      year: b.year ?? 0,
      price: b.price ?? 0,
    });
    this.authors.clear();
    (b.authors ?? []).forEach((a) =>
      this.authors.push(this.fb.control(a || ""))
    );
    if (this.authors.length === 0) this.addAuthor();
  }

  addAuthor() {
    this.authors.push(this.fb.control(""));
  }
  removeAuthor(i: number) {
    this.authors.removeAt(i);
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.submitForm.emit(v as BookFormValue);
  }

  private isbnUniqueValidator(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      const raw = (control.value ?? "").toString().trim();
      if (!/^\d{13}$/.test(raw)) return of(null);
      if (this.initial && raw === this.initial.isbn) return of(null);

      return this.books.checkIsbnExists(raw).pipe(
        map((exists) => (exists ? { isbnTaken: true } : null)),
        catchError(() => of(null))
      );
    };
  }
}
