using FileArchiveDemo.Data;

namespace FileArchiveDemo.Ingest;

public sealed class FileIngestor
{
    private readonly FileArchiveRepository _repo;

    public FileIngestor(FileArchiveRepository repo)
    {
        _repo = repo;
    }

    public async Task<int> IngestDirectoryAsync(string watchPath, bool deleteAfterInsert)
    {
        if (!Directory.Exists(watchPath))
        {
            Console.WriteLine($"[INGEST] WatchPath does not exist: {watchPath}");
            return 0;
        }

        var files = Directory.EnumerateFiles(watchPath, "*", SearchOption.TopDirectoryOnly)
            .Select(p => new FileInfo(p))
            .ToList();

        Console.WriteLine("[INGEST] Files found:");
        foreach (var f in files)
            Console.WriteLine("  - " + f.FullName);


        if (files.Count == 0)
        {
            Console.WriteLine("[INGEST] No files found.");
            return 0;
        }

        Console.WriteLine($"[INGEST] Found {files.Count} file(s) in: {watchPath}");

        int success = 0;

        foreach (var file in files)
        {
            try
            {
                Console.WriteLine($"[INGEST] Inserting: {file.Name} ({file.Length} bytes)");
                var id = await _repo.InsertFileAsync(file);
                Console.WriteLine($"[INGEST] Inserted row id={id}");

                if (deleteAfterInsert)
                {
                    File.Delete(file.FullName);
                    Console.WriteLine($"[INGEST] Deleted original. Row Id={id}");
                }
                else
                {
                    Console.WriteLine($"[INGEST] Kept original. Row Id={id}");
                }

                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[INGEST] ERROR on {file.FullName}");
                Console.WriteLine(ex);
            }
        }

        Console.WriteLine($"[INGEST] Completed. Success={success}/{files.Count}");
        return success;
    }
}
