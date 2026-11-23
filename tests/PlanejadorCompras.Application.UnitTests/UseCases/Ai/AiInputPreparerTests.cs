using System.Text;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UseCases.Ai;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Ai;

public sealed class AiInputPreparerTests
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7 fake body");
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04, 1, 2, 3];

    private static AiInputPreparer Create(FakeAiDocumentReader? reader = null, AiSettings? settings = null) =>
        new(settings ?? new AiSettings(), reader ?? new FakeAiDocumentReader());

    private static AiUploadedFile File(string name, byte[] content) => new(name, content);

    [Fact]
    public void Prepare_WithOnlyText_TrimsAndKeepsIt()
    {
        var input = Create().Prepare("  Arroz 10,00  ", null);

        Assert.Equal("Arroz 10,00", input.Text);
        Assert.Null(input.Attachment);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n ")]
    public void Prepare_WithoutTextOrFile_IsRejected(string? text)
    {
        var exception = Assert.Throws<BadRequestException>(() => Create().Prepare(text, File("vazio.pdf", [])));

        Assert.Equal("ai_empty_input", exception.ErrorCode);
    }

    [Fact]
    public void Prepare_WithTextOverTheLimit_IsRejected()
    {
        var preparer = Create(settings: new AiSettings { MaxInputCharacters = 100 });

        var exception = Assert.Throws<BadRequestException>(() => preparer.Prepare(new string('a', 101), null));

        Assert.Equal("ai_text_too_long", exception.ErrorCode);
    }

    [Fact]
    public void Prepare_WithFileOverTheLimit_IsRejected()
    {
        var preparer = Create(settings: new AiSettings { MaxFileBytes = 1024 });
        var content = Pdf.Concat(new byte[2000]).ToArray();

        var exception = Assert.Throws<BadRequestException>(() => preparer.Prepare(null, File("cotacao.pdf", content)));

        Assert.Equal("ai_file_too_large", exception.ErrorCode);
    }

    [Fact]
    public void Prepare_NeverAcceptsMoreThanTheHardFileLimit_EvenIfConfigured()
    {
        var preparer = Create(settings: new AiSettings { MaxFileBytes = long.MaxValue });

        Assert.Equal(AiSettings.HardMaxFileBytes, preparer.MaxFileBytes);
    }

    [Fact]
    public void Prepare_DetectsPdfByContent_IgnoringTheFileName()
    {
        var input = Create(new FakeAiDocumentReader { PdfPages = 2 }).Prepare(null, File("foto.jpg", Pdf));

        Assert.Equal(AiAttachmentKind.Pdf, input.Attachment!.Kind);
        Assert.Equal("application/pdf", input.Attachment.MediaType);
        Assert.Null(input.Text);
    }

    [Fact]
    public void Prepare_DetectsImagesByContent()
    {
        var png = Create().Prepare(null, File("a.bin", Png));
        var jpeg = Create().Prepare("observação", File("a.bin", Jpeg));

        Assert.Equal("image/png", png.Attachment!.MediaType);
        Assert.Equal(AiAttachmentKind.Image, png.Attachment.Kind);
        Assert.Equal("image/jpeg", jpeg.Attachment!.MediaType);
        Assert.Equal("observação", jpeg.Text);
    }

    [Theory]
    [InlineData("cotacao.pdf")]
    [InlineData("foto.png")]
    [InlineData("planilha.xlsx")]
    [InlineData("programa.exe")]
    public void Prepare_RejectsFilesWhoseContentIsNotAnAcceptedType(string fileName)
    {
        // An executable (MZ header) renamed to look like an accepted file.
        byte[] executable = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];

        var exception = Assert.Throws<BadRequestException>(() => Create().Prepare(null, File(fileName, executable)));

        Assert.Equal("ai_unsupported_file", exception.ErrorCode);
    }

    [Fact]
    public void Prepare_RejectsBinaryContentNamedAsCsv()
    {
        byte[] binary = [0x41, 0x00, 0x42, 0x00];

        var exception = Assert.Throws<BadRequestException>(() => Create().Prepare(null, File("precos.csv", binary)));

        Assert.Equal("ai_unsupported_file", exception.ErrorCode);
    }

    [Fact]
    public void Prepare_RejectsPdfWithTooManyPages()
    {
        var reader = new FakeAiDocumentReader { PdfPages = 21 };

        var exception = Assert.Throws<BadRequestException>(() => Create(reader).Prepare(null, File("cotacao.pdf", Pdf)));

        Assert.Equal("ai_pdf_too_long", exception.ErrorCode);
    }

    [Fact]
    public void Prepare_ConvertsSpreadsheetToText_AndJoinsItWithThePastedText()
    {
        var reader = new FakeAiDocumentReader { SpreadsheetText = "Arroz\t24,90\nLeite\t4,99\n" };

        var input = Create(reader).Prepare("Segue a cotação", File("precos.xlsx", Zip));

        Assert.Null(input.Attachment);
        Assert.Equal("Segue a cotação\n\nArroz\t24,90\nLeite\t4,99", input.Text);
    }

    [Fact]
    public void Prepare_RejectsZipThatIsNotAReadableSpreadsheet()
    {
        var exception = Assert.Throws<BadRequestException>(() => Create().Prepare(null, File("arquivo.xlsx", Zip)));

        Assert.Equal("ai_unreadable_file", exception.ErrorCode);
    }

    [Fact]
    public void Prepare_RejectsSpreadsheetLargerThanTheTextLimit()
    {
        var reader = new FakeAiDocumentReader { SpreadsheetText = new string('a', 150) };
        var preparer = Create(reader, new AiSettings { MaxInputCharacters = 100 });

        var exception = Assert.Throws<BadRequestException>(() => preparer.Prepare(null, File("precos.xlsx", Zip)));

        Assert.Equal("ai_text_too_long", exception.ErrorCode);
    }

    [Fact]
    public void Prepare_ReadsCsvAsText_IncludingLatin1Exports()
    {
        var utf8 = Encoding.UTF8.GetBytes("Feijão;8,90");
        var latin1 = Encoding.Latin1.GetBytes("Feijão;8,90");

        Assert.Equal("Feijão;8,90", Create().Prepare(null, File("precos.csv", utf8)).Text);
        Assert.Equal("Feijão;8,90", Create().Prepare(null, File("PRECOS.CSV", latin1)).Text);
    }
}
