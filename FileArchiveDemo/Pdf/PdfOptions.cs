namespace FileArchiveDemo.Pdf;

public sealed class PdfOptions
{
    public string TempPath { get; set; } = @"C:\Temp\FileArchivePdf";
    public string SofficePath { get; set; } = "soffice.exe";
    public int TimeoutSeconds { get; set; } = 60;
}
