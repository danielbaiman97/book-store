using BookstoreXmlApi.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;
namespace BookstoreXmlApi.Tests;

public class EnvStub : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Test";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

public class SmokeTests
{
    [Fact]
    public void Repository_Creates_File_When_Missing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "store.xml");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string,string?> { ["Bookstore:XmlPath"] = path })
            .Build();

        var env = new EnvStub();
        var repo = new XmlBookstoreRepository(config, env);
        Assert.True(File.Exists(path));
    }
}
