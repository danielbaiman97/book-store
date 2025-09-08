using BookstoreXmlApi.Dtos;
using BookstoreXmlApi.Models;

namespace BookstoreXmlApi.Services;

public interface IBookstoreService
{
    IReadOnlyList<Book> GetAll();
    (IReadOnlyList<Book> items, int total) List(string? search, string? category,string? sortBy, string? order,int page, int pageSize);
    IReadOnlyList<string> GetCategories();
    Book? GetByIsbn(string isbn);
    Book Create(BookCreateDto dto);
    Book Update(string isbn, BookUpdateDto dto);
    void Delete(string isbn);
    string BuildHtmlReport();
    string ExportCsv();
    void ImportCsv(System.IO.Stream csvStream);
    string ComputeEtag();
}
