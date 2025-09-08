using System.Xml.Linq;

namespace BookstoreXmlApi.Models;

public sealed record Book
{
    public string Isbn { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string TitleLang { get; init; } = "en";
    public List<string> Authors { get; init; } = new();
    public string Category { get; init; } = default!;
    public string? Cover { get; init; }
    public int Year { get; init; }
    public decimal Price { get; init; }

    public static Book FromXElement(XElement bookEl)
    {
        var category = (string?)bookEl.Attribute("category") ?? "";
        var cover = (string?)bookEl.Attribute("cover");
        var titleEl = bookEl.Element("title") ?? throw new InvalidDataException("Missing <title>");
        var isbn = (string?)bookEl.Element("isbn") ?? throw new InvalidDataException("Missing <isbn>");
        var authors = bookEl.Elements("author").Select(a => (string)a).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        return new Book
        {
            Category = category,
            Cover = cover,
            Isbn = isbn!,
            Title = (string)titleEl,
            TitleLang = (string?)titleEl.Attribute("lang") ?? "en",
            Authors = authors,
            Year = (int?)bookEl.Element("year") ?? 0,
            Price = (decimal?)bookEl.Element("price") ?? 0m
        };
    }

    public XElement ToXElement()
    {
        var el = new XElement("book",
            new XAttribute("category", Category)
        );
        if (!string.IsNullOrWhiteSpace(Cover))
            el.Add(new XAttribute("cover", Cover));
        el.Add(new XElement("isbn", Isbn));
        el.Add(new XElement("title", new XAttribute("lang", TitleLang), Title));
        foreach (var a in Authors)
            el.Add(new XElement("author", a));
        el.Add(new XElement("year", Year));
        el.Add(new XElement("price", Price));
        return el;
    }
}
