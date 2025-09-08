using System.Text;
using BookstoreXmlApi.Models;

namespace BookstoreXmlApi.Utils;

public static class HtmlReportBuilder
{
    public static string Build(IEnumerable<Book> books, string? title = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine($"<title>{System.Net.WebUtility.HtmlEncode(title ?? "Bookstore Report")}</title>");
        sb.AppendLine("""
<style>
body{font-family:system-ui,-apple-system,Segoe UI,Roboto,Ubuntu,'Helvetica Neue',Arial,sans-serif;margin:24px;}
table{border-collapse:collapse;width:100%;}
th,td{border:1px solid #ddd;padding:8px;vertical-align:top;}
th{background:#f5f5f5;text-align:left;}
tbody tr:nth-child(even){background:#fafafa;}
caption{caption-side:top;font-weight:700;font-size:1.25rem;margin-bottom:.5rem;}
.small{color:#666;font-size:.85rem;margin:.25rem 0;}
</style>
""");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<table>");
        sb.AppendLine("<caption>Bookstore Inventory</caption>");
        sb.AppendLine("<thead><tr><th>Title</th><th>Author(s)</th><th>Category</th><th>Year</th><th>Price</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var b in books)
        {
            var authors = string.Join(", ", b.Authors);
            sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(b.Title)}</td><td>{System.Net.WebUtility.HtmlEncode(authors)}</td><td>{System.Net.WebUtility.HtmlEncode(b.Category)}</td><td>{b.Year}</td><td>{b.Price}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");
        sb.AppendLine($"<p class=\"small\">Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
