using System.ComponentModel.DataAnnotations;

namespace BookstoreXmlApi.Dtos;

public sealed class BookCreateDto
{
    [Required, RegularExpression(@"^\d{13}$")]
    public string Isbn { get; set; } = default!;

    [Required, MinLength(1)]
    public string Title { get; set; } = default!;

    public string TitleLang { get; set; } = "en";

    [Required, MinLength(1)]
    public List<string> Authors { get; set; } = new();

    [Required]
    public string Category { get; set; } = default!;

    public string? Cover { get; set; }

    [Range(0, 9999)]
    public int Year { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }
}
