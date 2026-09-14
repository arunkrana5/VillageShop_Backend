using BCrypt.Net;
using VillageShop.Common.Models;
using Xunit;
using Xunit.Abstractions;

namespace VillageShop.Tests;

public class PostResponseTests
{
    private readonly ITestOutputHelper _output;

    public PostResponseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateBcryptHashForAdmin()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("admin123");
        _output.WriteLine($"VALID_HASH:{hash}");
        Assert.True(BCrypt.Net.BCrypt.Verify("admin123", hash));
    }
}
