using VillageShop.Common.Utils;
using Xunit;

namespace VillageShop.Tests;

public class WhatsAppPhoneNormalizerTests
{
    [Theory]
    [InlineData("8581896270", "918581896270", "918581896270@c.us")]
    [InlineData("+91 85818 96270", "918581896270", "918581896270@c.us")]
    [InlineData("91-8581896270", "918581896270", "918581896270@c.us")]
    [InlineData("08581896270", "918581896270", "918581896270@c.us")]
    [InlineData("9162886117", "919162886117", "919162886117@c.us")] // 10-digit subscriber starting with 91
    [InlineData("09162886117", "919162886117", "919162886117@c.us")]
    [InlineData("+91 9162886117", "919162886117", "919162886117@c.us")]
    [InlineData("+91 08581896270", "918581896270", "918581896270@c.us")]
    [InlineData("91918581896270", "918581896270", "918581896270@c.us")] // 14-digit duplicate 91
    [InlineData("+1 555 123 4567", "15551234567", "15551234567@c.us")] // International
    public void Normalize_ValidPhoneFormats_NormalizesSuccessfully(string input, string expectedNorm, string expectedChatId)
    {
        var result = WhatsAppPhoneNormalizer.Normalize(input);

        Assert.True(result.IsValid, $"Expected valid for '{input}', but got error: {result.ErrorMessage}");
        Assert.Equal(expectedNorm, result.NormalizedPhone);
        Assert.Equal(expectedChatId, result.ChatId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("0000000000")]
    [InlineData("1111111111")]
    [InlineData("3581896270")] // 10 digits starting with 3 (invalid Indian mobile)
    public void Normalize_InvalidPhoneFormats_ReturnsFalseWithError(string input)
    {
        var result = WhatsAppPhoneNormalizer.Normalize(input);

        Assert.False(result.IsValid, $"Expected invalid for '{input}'");
        Assert.NotEmpty(result.ErrorMessage);
    }
}
