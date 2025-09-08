using System.IO;
using System;
using Microsoft.Extensions.Configuration;
using BookstoreXmlApi.Dtos;
using BookstoreXmlApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookstoreXmlApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookstoreService _svc;

    public BooksController(IBookstoreService svc) => _svc = svc;
    [HttpGet]
    public IActionResult GetAll([FromQuery] string? search, [FromQuery] string? category,
                            [FromQuery] string? sortBy, [FromQuery] string? order = "asc",
                            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var etag = _svc.ComputeEtag();
        if (Request.Headers.IfNoneMatch.Contains(etag))
            return StatusCode(StatusCodes.Status304NotModified);
        var (items, total) = _svc.List(search, category, sortBy, order, page, pageSize);
        Response.Headers.ETag = etag;
        return Ok(new { total, items });
    }
    /// <summary>Get a single book by ISBN</summary>
    [HttpGet("{isbn}")]
    public IActionResult GetByIsbn(string isbn)
    {
        var b = _svc.GetByIsbn(isbn);
        return b is null ? NotFound() : Ok(b);
    }
    [HttpGet("categories")]
    public IActionResult GetCategories() => Ok(_svc.GetCategories());
    /// <summary>Create a new book (ISBN must be unique)</summary>
    [HttpPost]
    public IActionResult Create([FromBody] BookCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var created = _svc.Create(dto);
            return CreatedAtAction(nameof(GetByIsbn), new { isbn = created.Isbn }, created);
        }
        catch (InvalidOperationException ex) { return Problem(title: ex.Message, statusCode: StatusCodes.Status409Conflict); } // { return Conflict(new { message = ex.Message }); }
        catch (ArgumentException ex) { return Problem(title: ex.Message, statusCode: StatusCodes.Status400BadRequest); } // { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Update an existing book</summary>
    [HttpPut("{isbn}")]
    public IActionResult Update(string isbn, [FromBody] BookUpdateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var updated = _svc.Update(isbn, dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException) { return Problem(title: $"Book with ISBN {isbn} not found.", statusCode: StatusCodes.Status404NotFound); }
        catch (ArgumentException ex) { return Problem(title: ex.Message, statusCode: StatusCodes.Status400BadRequest); }
    }

    /// <summary>Delete a book</summary>
    [HttpDelete("{isbn}")]
    public IActionResult Delete(string isbn)
    {
        try
        {
            _svc.Delete(isbn);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(new { message = $"Book with ISBN {isbn} not found." }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Get an HTML report (table) of current books</summary>
    [HttpGet("report/html")]
    public IActionResult GetHtmlReport()
    {
        var html = _svc.BuildHtmlReport();
        return Content(html, "text/html");
    }

    /// <summary>Export all books to CSV (authors joined by comma)</summary>
    [HttpGet("export/csv")]
    public IActionResult ExportCsv()
    {
        var csv = _svc.ExportCsv();
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "books.csv");
    }
}
