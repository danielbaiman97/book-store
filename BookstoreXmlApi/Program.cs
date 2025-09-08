using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using BookstoreXmlApi.Repositories;
using BookstoreXmlApi.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration["Bookstore:XmlPath"] = builder.Configuration["Bookstore:XmlPath"]
    ?? builder.Configuration.GetValue<string>("BOOKSTORE_XML_PATH")
    ?? builder.Configuration.GetValue<string>("Bookstore__XmlPath")
    ?? Path.Combine(AppContext.BaseDirectory, "data", "bookstore.xml");


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(o =>
{
    o.AddPolicy("dev", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});


builder.Services.AddProblemDetails();

//  Rate Limiter
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                AutoReplenishment = true
            }));
});

// DI
builder.Services.AddSingleton<IXmlBookstoreRepository, XmlBookstoreRepository>();
builder.Services.AddSingleton<IBookstoreService, BookstoreService>();

var app = builder.Build();

// Middleware
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("dev");
app.UseRateLimiter();

app.MapControllers();
app.Run();