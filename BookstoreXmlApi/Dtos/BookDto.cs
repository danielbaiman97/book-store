namespace BookstoreXmlApi.Dtos;

public sealed record BookDto(
    string Isbn,
    string Title,
    string TitleLang,
    List<string> Authors,
    string Category,
    string? Cover,
    int Year,
    decimal Price
);
