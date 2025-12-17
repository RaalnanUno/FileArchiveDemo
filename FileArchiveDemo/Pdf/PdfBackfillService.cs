using FileArchiveDemo.Data;

namespace FileArchiveDemo.Pdf;

public sealed class PdfBackfillService
{
    private readonly FileArchiveRepository _repo;
    private readonly IPdfConverter _converter;

    public PdfBackfillService(FileArchiveRepository repo, IPdfConverter converter)
    {
        _repo = repo;
        _converter = converter;
    }

    public async Task<int> RunAsync(int take = 25)
    {
        var pending = await _repo.GetPendingPdfAsync(take);

        Console.WriteLine($"[PDF] Pending rows: {pending.Count}");

        int success = 0;

        foreach (var row in pending)
        {
            try
            {
                Console.WriteLine($"[PDF] Converting Id={row.Id} File={row.OriginalFileName}");
                var pdf = await _converter.ConvertToPdfAsync(row.OriginalFileName, row.OriginalExtension, row.OriginalBlob);

                await _repo.MarkPdfSuccessAsync(row.Id, pdf);
                Console.WriteLine($"[PDF] Success Id={row.Id} (pdf bytes={pdf.Length})");
                success++;
            }
            catch (Exception ex)
            {
                var msg = ex.ToString();

                // If LibreOffice isn't installed / soffice missing, keep it pending
                if (msg.Contains("soffice.exe", StringComparison.OrdinalIgnoreCase) &&
                    msg.Contains("cannot find the file", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[PDF] SKIP (converter missing) Id={row.Id}: {ex.Message}");
                    // optional: record a soft warning somewhere, but do NOT mark failed
                    continue;
                }

                await _repo.MarkPdfFailedAsync(row.Id, ex.Message);
                Console.WriteLine($"[PDF] FAILED Id={row.Id}: {ex.Message}");
            }

        }

        Console.WriteLine($"[PDF] Completed. Success={success}/{pending.Count}");
        return success;
    }
}
