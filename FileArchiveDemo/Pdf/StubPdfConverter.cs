namespace FileArchiveDemo.Pdf;

public sealed class StubPdfConverter : IPdfConverter
{
    public Task<byte[]> ConvertToPdfAsync(string originalFileName, string? extension, byte[] originalBytes)
    {
        throw new NotSupportedException("PDF conversion not wired yet (stub converter).");
    }
}
