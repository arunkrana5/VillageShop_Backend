using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MiniExcelLibs;
using VillageShop.Infrastructure.Uploader;
using Xunit;

namespace VillageShop.Tests;

public class ExcelUploaderServiceTests
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
    private readonly UploadProcessingChannel _channel;
    private readonly ExcelUploaderService _service;

    public ExcelUploaderServiceTests()
    {
        var myConfiguration = new System.Collections.Generic.Dictionary<string, string?>
        {
            {"ConnectionStrings:DefaultConnection", "Server=185.100.212.57,1433;Database=dev.inhouse;User Id=sa;Password=Thrivera@1701;TrustServerCertificate=True;Encrypt=False;"}
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();

        _channel = new UploadProcessingChannel();
        _service = new ExcelUploaderService(_config, _channel, NullLogger<ExcelUploaderService>.Instance);
    }

    [Fact]
    public async Task GetActiveTypesAsync_Returns_Transaction_And_Master_Types()
    {
        var types = (await _service.GetActiveTypesAsync()).ToList();

        Assert.NotEmpty(types);
        
        // Assert Transaction group exists
        var trans = types.FirstOrDefault(t => t.Code == "Transaction");
        Assert.NotNull(trans);
        Assert.Equal("Transaction", trans.GroupType);

        // Assert Master types exist
        var itemType = types.FirstOrDefault(t => t.Code == "Item");
        Assert.NotNull(itemType);
        Assert.Equal("Master", itemType.GroupType);
    }

    [Fact]
    public async Task GenerateTemplateAsync_For_Transaction_Produces_Valid_Excel_With_Sample_Rows()
    {
        var bytes = await _service.GenerateTemplateAsync("Transaction", tenantId: 1);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        using var ms = new MemoryStream(bytes);
        var rows = ms.Query(useHeaderRow: true).Cast<System.Collections.Generic.IDictionary<string, object>>().ToList();
        
        Assert.NotEmpty(rows);
        var firstRow = rows.First();
        
        // Check common transaction columns exist (with or without required asterisk *)
        Assert.True(firstRow.Keys.Any(k => k.StartsWith("TransactionType")));
        Assert.True(firstRow.Keys.Any(k => k.StartsWith("ItemCode")));
        Assert.True(firstRow.Keys.Any(k => k.StartsWith("Quantity")));
        Assert.True(firstRow.Keys.Any(k => k.StartsWith("Rate")));
    }

    [Fact]
    public async Task GenerateTemplateAsync_For_Item_Produces_Valid_Excel()
    {
        var bytes = await _service.GenerateTemplateAsync("Item", tenantId: 1);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        using var ms = new MemoryStream(bytes);
        var rows = ms.Query(useHeaderRow: true).Cast<System.Collections.Generic.IDictionary<string, object>>().ToList();
        
        Assert.NotEmpty(rows);
        var firstRow = rows.First();
        
        Assert.True(firstRow.Keys.Any(k => k.StartsWith("ItemCode")));
        Assert.True(firstRow.Keys.Any(k => k.Contains("Name") || k.Contains("Product")));
    }
}
