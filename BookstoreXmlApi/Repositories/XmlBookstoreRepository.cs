using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Threading;
using System.Globalization;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

using BookstoreXmlApi.Models;
using BookstoreXmlApi.Utils;

namespace BookstoreXmlApi.Repositories;

public sealed class XmlBookstoreRepository : IXmlBookstoreRepository
{
    private readonly string _xmlPath;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    private readonly IConfiguration _config;

    public XmlBookstoreRepository(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _xmlPath = config["Bookstore:XmlPath"] ?? Path.Combine(AppContext.BaseDirectory, "data", "bookstore.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(_xmlPath)!);
        if (!File.Exists(_xmlPath))
        {
            new XDocument(new XElement("bookstore")).Save(_xmlPath);
        }
    }

    private XDocument LoadSecureValidated()
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var reader = XmlReader.Create(_xmlPath, settings);
        var doc = XDocument.Load(reader, LoadOptions.PreserveWhitespace);

        var xsdPath = _config["Bookstore:XsdPath"];
        if (!string.IsNullOrWhiteSpace(xsdPath) && File.Exists(xsdPath))
        {
            var schemas = new XmlSchemaSet();
            using var xsdReader = XmlReader.Create(xsdPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            schemas.Add(null, xsdReader);

            string? err = null;
            doc.Validate(schemas, (o, e) => err ??= e.Message);
            if (err != null) throw new InvalidOperationException($"XML validation failed: {err}");
        }
        return doc;
    }

    private void SaveAtomic(XDocument doc)
    {
        var tmp = Path.GetTempFileName();
        doc.Save(tmp);
        File.Replace(tmp, _xmlPath, null);
    }

    public IReadOnlyList<Book> GetAll()
    {
        var doc = LoadSecureValidated();
        return doc.Root!.Elements("book").Select(Book.FromXElement).ToList();
    }

    public Book? GetByIsbn(string isbn)
    {
        var doc = LoadSecureValidated();
        var el = doc.Root!.Elements("book").FirstOrDefault(b => (string?)b.Element("isbn") == isbn);
        return el is null ? null : Book.FromXElement(el);
    }

    public void Add(Book book)
    {
        _lock.EnterWriteLock();
        try
        {
            var doc = LoadSecureValidated();
            if (doc.Root!.Elements("book").Any(b => (string?)b.Element("isbn") == book.Isbn))
                throw new InvalidOperationException($"Book with ISBN {book.Isbn} already exists.");

            doc.Root!.Add(book.ToXElement());
            SaveAtomic(doc);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void Update(string isbn, Book updated)
    {
        _lock.EnterWriteLock();
        try
        {
            var doc = LoadSecureValidated();
            var el = doc.Root!.Elements("book").FirstOrDefault(b => (string?)b.Element("isbn") == isbn)
                     ?? throw new KeyNotFoundException($"Book with ISBN {isbn} not found.");
            el.ReplaceWith(updated.ToXElement());
            SaveAtomic(doc);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void Delete(string isbn)
    {
        _lock.EnterWriteLock();
        try
        {
            var doc = LoadSecureValidated();
            var el = doc.Root!.Elements("book").FirstOrDefault(b => (string?)b.Element("isbn") == isbn)
                     ?? throw new KeyNotFoundException($"Book with ISBN {isbn} not found.");
            el.Remove();
            SaveAtomic(doc);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public string ExportCsv()
    {
        var books = GetAll();
        var sb = new StringBuilder();
        sb.AppendLine("isbn,title,titleLang,authors,category,cover,year,price");
        foreach (var b in books)
        {
            var authors = string.Join(", ", b.Authors);
            string esc(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
            sb.AppendLine(string.Join(",", new[] {
                esc(b.Isbn),
                esc(b.Title),
                esc(b.TitleLang),
                esc(authors),
                esc(b.Category),
                esc(b.Cover ?? ""),
                b.Year.ToString(CultureInfo.InvariantCulture),
                b.Price.ToString(CultureInfo.InvariantCulture)
            }));
        }
        return sb.ToString();
    }

    public string ComputeEtag()
    {
        using var fs = File.OpenRead(_xmlPath);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(fs);
        return $"W/\"{Convert.ToHexString(hash)}\"";
    }

    public void ImportCsv(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        string? line;
        int lineNum = 0;

        var all = GetAll().ToDictionary(b => b.Isbn);

        while ((line = reader.ReadLine()) != null)
        {
            lineNum++;
            if (lineNum == 1 && line.StartsWith("ISBN", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = ParseCsvLine(line);
            if (parts.Length < 8) continue;

            var isbn = parts[0].Trim();
            var title = parts[1].Trim();
            var lang  = parts[2].Trim();
            var authorsCsv = parts[3].Trim();
            var category = parts[4].Trim();
            var cover = string.IsNullOrWhiteSpace(parts[5]) ? null : parts[5].Trim();

            int year = 0;
            int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out year);

            decimal price = 0m;
            if (!decimal.TryParse(parts[7], NumberStyles.Number, CultureInfo.InvariantCulture, out price))
                decimal.TryParse(parts[7], out price);

            var authors = authorsCsv.Split(',').Select(a => a.Trim()).Where(a => a.Length > 0).ToList();

            var book = new Book
            {
                Isbn = isbn,
                Title = title,
                TitleLang = string.IsNullOrWhiteSpace(lang) ? "en" : lang,
                Authors = authors,
                Category = category,
                Cover = cover,
                Year = year,
                Price = price
            };

            if (all.ContainsKey(isbn))
                Update(isbn, book);
            else
                Add(book);
        }
    }

    private static string[] ParseCsvLine(string line)
    {
        var res = new List<string>();
        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cur.Append('"'); i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                res.Add(cur.ToString()); cur.Clear();
            }
            else
            {
                cur.Append(c);
            }
        }
        res.Add(cur.ToString());
        return res.ToArray();
    }
}