using BookstoreXmlApi.Models;

namespace BookstoreXmlApi.Repositories;

public interface IXmlBookstoreRepository
{
    IReadOnlyList<Book> GetAll();
    Book? GetByIsbn(string isbn);
    void Add(Book book);
    void Update(string isbn, Book updated);
    void Delete(string isbn);
    string ExportCsv();
    void ImportCsv(Stream csvStream);
    string ComputeEtag();
}
