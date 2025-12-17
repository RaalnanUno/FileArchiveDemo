namespace FileArchiveDemo.Pdf;

public interface IPdfConverter
{
    Task<byte[]> ConvertToPdfAsync(string originalFileName, string? extension, byte[] originalBytes);
}
