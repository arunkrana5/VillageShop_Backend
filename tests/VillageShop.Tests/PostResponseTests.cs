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

    [Fact]
    public void GenericMasterRequest_Deserialization_HandlesMultipleAliasesWithoutCollision()
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Test 1: camelCase masterName
        var json1 = "{\"masterType\":\"ItemCategory\",\"masterName\":\"Groceries\",\"masterCode\":\"CAT-01\"}";
        var req1 = System.Text.Json.JsonSerializer.Deserialize<VillageShop.Api.Controllers.GenericMasterRequest>(json1, options);
        Assert.NotNull(req1);
        Assert.Equal("ItemCategory", req1.GetEffectiveMasterType());
        Assert.Equal("Groceries", req1.GetEffectiveMasterName());
        Assert.Equal("CAT-01", req1.GetEffectiveMasterCode());

        // Test 2: PascalCase MasterName
        var json2 = "{\"MasterType\":\"ItemCategory\",\"MasterName\":\"Snacks\",\"MasterCode\":\"CAT-02\"}";
        var req2 = System.Text.Json.JsonSerializer.Deserialize<VillageShop.Api.Controllers.GenericMasterRequest>(json2, options);
        Assert.NotNull(req2);
        Assert.Equal("ItemCategory", req2.GetEffectiveMasterType());
        Assert.Equal("Snacks", req2.GetEffectiveMasterName());
        Assert.Equal("CAT-02", req2.GetEffectiveMasterCode());

        // Test 3: Legacy domain-specific aliases (categoryName, symbol)
        var json3 = "{\"categoryName\":\"Bakery\",\"symbol\":\"BKR\"}";
        var req3 = System.Text.Json.JsonSerializer.Deserialize<VillageShop.Api.Controllers.GenericMasterRequest>(json3, options);
        Assert.NotNull(req3);
        Assert.Equal("Bakery", req3.GetEffectiveMasterName());
        Assert.Equal("BKR", req3.GetEffectiveMasterCode());

        // Test 4: Generic name and code
        var json4 = "{\"name\":\"Cash\",\"code\":\"PAY-CASH\"}";
        var req4 = System.Text.Json.JsonSerializer.Deserialize<VillageShop.Api.Controllers.GenericMasterRequest>(json4, options);
        Assert.NotNull(req4);
        Assert.Equal("Cash", req4.GetEffectiveMasterName());
        Assert.Equal("PAY-CASH", req4.GetEffectiveMasterCode());
    }
}

