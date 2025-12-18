using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace FileArchiveDemo.Data;

public sealed class FileArchiveRepository
{
    private readonly string _connString;

    public FileArchiveRepository(string connString)
    {
        _connString = connString;
    }

    public async Task<(string Server, string Db)> WhereAmIAsync()
    {
        // SQLite doesn’t have server/db like SQL Server.
        const string sql = "SELECT sqlite_version() AS ServerName, 'FileArchiveDemo.db' AS DbName;";
        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();

        return (r["ServerName"]?.ToString() ?? "(null)", r["DbName"]?.ToString() ?? "(null)");
    }

    public async Task<long> CountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM FileArchive;";
        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public async Task<long> InsertFileAsync(FileInfo file)
    {
        byte[] bytes = await File.ReadAllBytesAsync(file.FullName);
        byte[] sha256 = SHA256.HashData(bytes);

        const string sql = @"
INSERT INTO FileArchive
(
  OriginalFullPath,
  OriginalFileName,
  OriginalExtension,
  ContentType,
  FileSizeBytes,
  FileCreatedUtc,
  FileModifiedUtc,
  Sha256,
  OriginalBlob,
  PdfStatus
)
VALUES
(
  $OriginalFullPath,
  $OriginalFileName,
  $OriginalExtension,
  $ContentType,
  $FileSizeBytes,
  $FileCreatedUtc,
  $FileModifiedUtc,
  $Sha256,
  $OriginalBlob,
  0
)
ON CONFLICT(Sha256) DO NOTHING;

SELECT Id
FROM FileArchive
WHERE Sha256 = $Sha256
LIMIT 1;
";


        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        cmd.Parameters.AddWithValue("$OriginalFullPath", (object?)file.FullName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$OriginalFileName", file.Name);
        cmd.Parameters.AddWithValue("$OriginalExtension", file.Extension);
        cmd.Parameters.AddWithValue("$ContentType", GuessContentType(file.Extension) ?? (object)DBNull.Value);

        cmd.Parameters.AddWithValue("$FileSizeBytes", file.Length);
        cmd.Parameters.AddWithValue("$FileCreatedUtc", file.CreationTimeUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$FileModifiedUtc", file.LastWriteTimeUtc.ToString("O"));

        cmd.Parameters.Add("$Sha256", SqliteType.Blob).Value = sha256;
        cmd.Parameters.Add("$OriginalBlob", SqliteType.Blob).Value = bytes;

        var idObj = await cmd.ExecuteScalarAsync();
        if (idObj is null)
            throw new InvalidOperationException("Insert/select failed to return an Id.");

        return Convert.ToInt64(idObj);
    }


    private static string? GuessContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => null
        };
    }

    public sealed record PendingPdfRow(long Id, string OriginalFileName, string? OriginalExtension, byte[] OriginalBlob);

    public async Task<List<PendingPdfRow>> GetPendingPdfAsync(int take = 25)
    {
        const string sql = @"
SELECT
  Id,
  OriginalFileName,
  OriginalExtension,
  OriginalBlob
FROM FileArchive
WHERE PdfBlob IS NULL AND PdfStatus = 0
ORDER BY Id
LIMIT $Take;";

        var results = new List<PendingPdfRow>();

        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$Take", take);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var id = r.GetInt64(0);
            var name = r.GetString(1);
            var ext = r.IsDBNull(2) ? null : r.GetString(2);
            var blob = (byte[])r["OriginalBlob"];

            results.Add(new PendingPdfRow(id, name, ext, blob));
        }

        return results;
    }

    public async Task MarkPdfSuccessAsync(long id, byte[] pdfBytes)
    {
        const string sql = @"
UPDATE FileArchive
SET
  PdfBlob = $PdfBlob,
  PdfConvertedUtc = $PdfConvertedUtc,
  PdfStatus = 1,
  PdfError = NULL
WHERE Id = $Id;";

        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        cmd.Parameters.AddWithValue("$Id", id);
        cmd.Parameters.AddWithValue("$PdfConvertedUtc", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.Add("$PdfBlob", SqliteType.Blob).Value = pdfBytes;

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkPdfFailedAsync(long id, string error)
    {
        const string sql = @"
UPDATE FileArchive
SET
  PdfStatus = 2,
  PdfError = $Err
WHERE Id = $Id;";

        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        cmd.Parameters.AddWithValue("$Id", id);
        cmd.Parameters.AddWithValue("$Err", error.Length > 2000 ? error[..2000] : error);

        await cmd.ExecuteNonQueryAsync();
    }
}
