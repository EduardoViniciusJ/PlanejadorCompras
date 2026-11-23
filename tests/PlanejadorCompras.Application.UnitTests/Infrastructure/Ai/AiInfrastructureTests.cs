using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Infrastructure.Services.Ai;

namespace PlanejadorCompras.Application.UnitTests.Infrastructure.Ai;

public sealed class AiInfrastructureTests
{
    private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => pair.Value))
            .Build();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnthropicAiClient_WithoutApiKey_StaysUnconfiguredAndAnswers503(string? apiKey)
    {
        // Building the client must not throw: the API has to start without a key.
        var client = new AnthropicAiClient(apiKey, new AiSettings(), NullLogger<AnthropicAiClient>.Instance);

        Assert.False(client.IsConfigured);
        var exception = await Assert.ThrowsAsync<AiNotConfiguredException>(() =>
            client.CompleteJsonAsync(new AiJsonRequest("system", "user", null, "{}")));
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    [Fact]
    public void AiSettingsFactory_WithoutConfiguration_UsesSafeDefaults()
    {
        var settings = AiSettingsFactory.Create(Configuration());

        Assert.False(settings.IsConfigured);
        Assert.Equal("claude-haiku-5-5", settings.Model);
        Assert.Equal(20, settings.MonthlyLimitPerUser);
        Assert.Equal(60, settings.TimeoutSeconds);
        Assert.Equal(5 * 1024 * 1024, settings.MaxFileBytes);
        Assert.Equal(20_000, settings.MaxInputCharacters);
    }

    [Fact]
    public void AiSettingsFactory_ReadsConfiguredValues()
    {
        var settings = AiSettingsFactory.Create(Configuration(
            ("Ai:ApiKey", "test-key-not-real"),
            ("Ai:Model", "claude-sonnet-5-5"),
            ("Ai:Effort", ""),
            ("Ai:MonthlyLimitPerUser", "50"),
            ("Ai:MaxFileBytes", "1048576")));

        Assert.True(settings.IsConfigured);
        Assert.Equal("claude-sonnet-5-5", settings.Model);
        Assert.Equal(string.Empty, settings.Effort);
        Assert.Equal(50, settings.MonthlyLimitPerUser);
        Assert.Equal(1_048_576, settings.MaxFileBytes);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("99999999999")]
    public void AiSettingsFactory_InvalidLimit_FallsBackToTheDefaultInsteadOfRemovingTheLimit(string value)
    {
        var settings = AiSettingsFactory.Create(Configuration(
            ("Ai:MonthlyLimitPerUser", value),
            ("Ai:MaxFileBytes", value),
            ("Ai:MaxInputCharacters", value)));

        Assert.Equal(AiSettings.DefaultMonthlyLimitPerUser, settings.MonthlyLimitPerUser);
        Assert.Equal(AiSettings.DefaultMaxFileBytes, settings.MaxFileBytes);
        Assert.Equal(AiSettings.DefaultMaxInputCharacters, settings.MaxInputCharacters);
    }

    [Fact]
    public void AiDocumentReader_ReadsSpreadsheetAsTabSeparatedText()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Cotação");
        sheet.Cell(1, 1).Value = "Produto";
        sheet.Cell(1, 2).Value = "Preço";
        sheet.Cell(2, 1).Value = "Arroz T1 5kg";
        sheet.Cell(2, 2).Value = 24.9;
        sheet.Cell(4, 1).Value = "Leite integral cx c/ 12";
        sheet.Cell(4, 2).Value = 59.88;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var read = new AiDocumentReader().TryReadSpreadsheetText(stream.ToArray(), 10_000, out var text);

        Assert.True(read);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Produto\tPreço", lines[0]);
        Assert.StartsWith("Arroz T1 5kg\t24", lines[1]);
        Assert.StartsWith("Leite integral cx c/ 12\t59", lines[2]);
    }

    [Fact]
    public void AiDocumentReader_StopsReadingSpreadsheetSoonAfterTheLimit()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Grande");
        for (var row = 1; row <= 500; row++)
        {
            sheet.Cell(row, 1).Value = new string('a', 50);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var read = new AiDocumentReader().TryReadSpreadsheetText(stream.ToArray(), 200, out var text);

        Assert.True(read);
        Assert.InRange(text.Length, 201, 300);
    }

    [Fact]
    public void AiDocumentReader_RejectsContentThatIsNotASpreadsheet()
    {
        byte[] fakeZip = [0x50, 0x4B, 0x03, 0x04, 0, 1, 2, 3, 4, 5];

        Assert.False(new AiDocumentReader().TryReadSpreadsheetText(fakeZip, 1000, out _));
    }

    [Fact]
    public void AiDocumentReader_ReturnsNullPageCount_WhenPdfCannotBeOpened()
    {
        var notReallyPdf = "%PDF-1.7 broken"u8.ToArray();

        Assert.Null(new AiDocumentReader().TryCountPdfPages(notReallyPdf));
    }
}
