export interface Book {
  isbn: string;
  title: string;
  titleLang: string;
  authors: string[];
  category: string;
  cover?: string | null;
  year: number;
  price: number;
}
