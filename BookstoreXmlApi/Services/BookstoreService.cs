using BookstoreXmlApi.Dtos;
using BookstoreXmlApi.Models;
using BookstoreXmlApi.Repositories;
using BookstoreXmlApi.Utils;
using System.Linq;
namespace BookstoreXmlApi.Services;

public sealed class BookstoreService : IBookstoreService
{
    private readonly IXmlBookstoreRepository _repo;

    public BookstoreService(IXmlBookstoreRepository repo)
    {
        _repo = repo;
    }
    [Obsolete("Use List(search, category, sortBy, order, page, pageSize) instead.")]
    public IReadOnlyList<Book> GetAll() => _repo.GetAll();
    public (IReadOnlyList<Book> items, int total) List(
        string? search, string? category, string? sortBy, string? order, int page, int pageSize)
    {
        IEnumerable<Book> q = _repo.GetAll();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim();
            static bool ContainsCI(string? h, string n) =>
                !string.IsNullOrEmpty(h) && h.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0;

            q = q.Where(b =>
                ContainsCI(b.Title, s) ||
                b.Authors.Any(a => a?.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) ||
                b.Isbn.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(b => string.Equals(b.Category, category, StringComparison.OrdinalIgnoreCase));

        bool desc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        string key = (sortBy ?? "title").ToLowerInvariant();

        q = key switch
        {
            "isbn"   => desc ? q.OrderByDescending(b => b.Isbn)   : q.OrderBy(b => b.Isbn),
            "title"  => desc ? q.OrderByDescending(b => b.Title)  : q.OrderBy(b => b.Title),
            "author" => desc ? q.OrderByDescending(b => b.Authors.FirstOrDefault() ?? "")
                            : q.OrderBy(b => b.Authors.FirstOrDefault() ?? ""),
            "year"   => desc ? q.OrderByDescending(b => b.Year)   : q.OrderBy(b => b.Year),
            "price"  => desc ? q.OrderByDescending(b => b.Price)  : q.OrderBy(b => b.Price),
            _        => desc ? q.OrderByDescending(b => b.Title)  : q.OrderBy(b => b.Title),
        };

        int total = q.Count();

        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 200) pageSize = 200;
        if (page <= 0) page = 1;

        var items = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, total);
    }
    public IReadOnlyList<string> GetCategories() =>
        _repo.GetAll()
            .Select(b => b.Category)                             
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())                        
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    public Book? GetByIsbn(string isbn) => _repo.GetByIsbn(isbn);

    public Book Create(BookCreateDto dto)
    {
        ValidateIsbn(dto.Isbn);
        var book = new Book
        {
            Isbn = dto.Isbn,
            Title = dto.Title,
            TitleLang = dto.TitleLang ?? "en",
            Authors = dto.Authors.Where(a => !string.IsNullOrWhiteSpace(a)).ToList(),
            Category = dto.Category,
            Cover = dto.Cover,
            Year = dto.Year,
            Price = dto.Price
        };
        ValidateBusiness(book);
        _repo.Add(book);
        return book;
    }

    public Book Update(string isbn, BookUpdateDto dto)
    {
        ValidateIsbn(isbn);
        var existing = _repo.GetByIsbn(isbn) ?? throw new KeyNotFoundException($"Book with ISBN {isbn} not found.");
        var updated = existing with
        {
            Title = dto.Title ?? existing.Title,
            TitleLang = dto.TitleLang ?? existing.TitleLang,
            Authors = dto.Authors?.Where(a => !string.IsNullOrWhiteSpace(a)).ToList() ?? existing.Authors,
            Category = dto.Category ?? existing.Category,
            Cover = dto.Cover ?? existing.Cover,
            Year = dto.Year ?? existing.Year,
            Price = dto.Price ?? existing.Price
        };
        ValidateBusiness(updated);
        _repo.Update(isbn, updated);
        return updated;
    }

    public void Delete(string isbn)
    {
        ValidateIsbn(isbn);
        _repo.Delete(isbn);
    }

    public string BuildHtmlReport()
        => HtmlReportBuilder.Build(_repo.GetAll(), "Bookstore Inventory");

    public string ExportCsv() => _repo.ExportCsv();

    public void ImportCsv(System.IO.Stream csvStream) => _repo.ImportCsv(csvStream);

    public string ComputeEtag() => _repo.ComputeEtag();


    private static void ValidateIsbn(string isbn)
    {
        // 13 digits strictly
        if (string.IsNullOrWhiteSpace(isbn) ||
            !System.Text.RegularExpressions.Regex.IsMatch(isbn, @"^\d{13}$"))
            throw new ArgumentException("ISBN must be 13 digits.", nameof(isbn));

        // ISBN-13 checksum
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int d = isbn[i] - '0';
            sum += (i % 2 == 0) ? d : d * 3;
        }
        int check = (10 - (sum % 10)) % 10;
        if (check != isbn[12] - '0')
            throw new ArgumentException("Invalid ISBN-13 checksum.", nameof(isbn));
    }
    private static void ValidateBusiness(Book book)
    {
        var maxYear = DateTime.UtcNow.Year + 1;
        if (book.Year < 1450 || book.Year > maxYear)
            throw new ArgumentException($"Year must be between 1450 and {maxYear}.");

        if (book.Price <= 0)
            throw new ArgumentException("Price must be greater than 0.");

        if (string.IsNullOrWhiteSpace(book.Title))
            throw new ArgumentException("Title is required.");

        if (string.IsNullOrWhiteSpace(book.Category))
            throw new ArgumentException("Category is required.");
    }
}