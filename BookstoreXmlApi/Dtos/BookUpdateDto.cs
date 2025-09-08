using System.ComponentModel.DataAnnotations;

namespace BookstoreXmlApi.Dtos;

public sealed class BookUpdateDto
{
    [MinLength(1)]
    public string? Title { get; set; }

    public string? TitleLang { get; set; }

    public List<string>? Authors { get; set; }

    public string? Category { get; set; }

    public string? Cover { get; set; }

    [Range(0, 9999)]
    public int? Year { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? Price { get; set; }
}
