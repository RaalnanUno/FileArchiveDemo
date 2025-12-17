using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

using FileArchiveDemo.Data;
using FileArchiveDemo.Ingest;
using FileArchiveDemo.Pdf;

static string ResolvePath(string? pathFromConfig, string defaultRelativeToExe)
{
    var p = string.IsNullOrWhiteSpace(pathFromConfig) ? defaultRelativeToExe : pathFromConfig;

    // If relative, make it relative to the exe folder (stable)
    if (!Path.IsPathRooted(p))
        p = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, p));

    // Ensure directory exists
    Directory.CreateDirectory(p);

    return p;
}

static string ResolveSqliteConnectionString(string rawConnString)
{
    var csb = new SqliteConnectionStringBuilder(rawConnString);

    // Default if someone left it blank
    if (string.IsNullOrWhiteSpace(csb.DataSource))
        csb.DataSource = "Data\\FileArchiveDemo.db";

    // Make relative paths relative to the executable folder (stable for dotnet run + VS)
    if (!Path.IsPathRooted(csb.DataSource))
        csb.DataSource = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, csb.DataSource));

    // Ensure parent directory exists so SQLite can create the db + wal/shm files
    var dir = Path.GetDirectoryName(csb.DataSource);
    if (!string.IsNullOrWhiteSpace(dir))
        Directory.CreateDirectory(dir);

    // Ensure we can create if missing
    if (csb.Mode == SqliteOpenMode.ReadOnly)
        csb.Mode = SqliteOpenMode.ReadWriteCreate;

    // Helps avoid some locking pain in demos (safe default)
    csb.Cache = SqliteCacheMode.Shared;

    return csb.ToString();
}

static string ResolveSofficePath(IConfiguration config)
{
    // 1) Config override wins
    var configured = config["Pdf:SofficePath"];
    if (!string.IsNullOrWhiteSpace(configured))
    {
        // If they provided a relative path, make it relative to exe
        if (!Path.IsPathRooted(configured))
            configured = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));

        return configured;
    }

    // 2) Try discovery (registry/common paths/env var)
    var discovered = LibreOfficeLocator.FindSofficeExe();
    if (!string.IsNullOrWhiteSpace(discovered))
        return discovered;

    // 3) Fallback: rely on PATH (may fail, but we’ll log it)
    return "soffice.exe";
}

// ---------------------------
// Build configuration
// ---------------------------
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)            // read from output folder
    .AddJsonFile("appsettings.json", optional: false) // must be copied to output
    .AddEnvironmentVariables()
    .Build();

// ---------------------------
// Resolve SQLite connection string
// ---------------------------
var rawConnString = config.GetConnectionString("FileArchiveDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:FileArchiveDb in appsettings.json");

var connString = ResolveSqliteConnectionString(rawConnString);

var dbg = new SqliteConnectionStringBuilder(connString);
Console.WriteLine("BASE DIR = " + AppContext.BaseDirectory);
Console.WriteLine("DB PATH  = " + dbg.DataSource);

// ---------------------------
// Bootstrap DB (migrations)
// ---------------------------
await SqlBootstrapper.EnsureDatabaseAndMigrateAsync(connString, "FileArchiveDemo");

var repo = new FileArchiveRepository(connString);

// Where-am-I sanity check
var where = await repo.WhereAmIAsync();
Console.WriteLine($"SQLite Version: {where.Server}");
Console.WriteLine($"DB Label:       {where.Db}");
Console.WriteLine("Database ready. Demo bootstrap complete.");

// ---------------------------
// Ingest
// ---------------------------
var watchPath = ResolvePath(config["Ingest:WatchPath"], "InboundFiles");
var deleteAfterInsert = config.GetValue("Ingest:DeleteAfterInsert", false);

Console.WriteLine("[INGEST] WatchPath = " + watchPath);

var ingestor = new FileIngestor(repo);
await ingestor.IngestDirectoryAsync(watchPath, deleteAfterInsert);

Console.WriteLine($"Rows in FileArchive: {await repo.CountAsync()}");

// ---------------------------
// PDF Backfill
// ---------------------------
var tempPath = ResolvePath(config["Pdf:TempPath"], Path.Combine("Temp", "FileArchivePdf"));

var sofficePath = ResolveSofficePath(config);
Console.WriteLine("[PDF] SofficePath   = " + sofficePath);
Console.WriteLine("[PDF] SofficeExists = " + (File.Exists(sofficePath) ? "True" : "False"));
Console.WriteLine("[PDF] TempPath      = " + tempPath);

if (!File.Exists(sofficePath) && !string.Equals(sofficePath, "soffice.exe", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("[PDF] WARNING: Pdf:SofficePath was set, but the file does not exist.");
    Console.WriteLine("[PDF]          Fix appsettings.json or set SOFFICE_PATH to a valid file.");
}
else if (string.Equals(sofficePath, "soffice.exe", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("[PDF] NOTE: Using 'soffice.exe' fallback. This requires LibreOffice to be on PATH.");
    Console.WriteLine("[PDF]       If conversion fails, set Pdf:SofficePath to the full path.");
}

var pdfOptions = new PdfOptions
{
    TempPath = tempPath,
    SofficePath = sofficePath,
    TimeoutSeconds = int.TryParse(config["Pdf:TimeoutSeconds"], out var t) ? t : 60
};

var pdfConverter = new LibreOfficeHeadlessPdfConverter(pdfOptions);
var pdfService = new PdfBackfillService(repo, pdfConverter);

await pdfService.RunAsync(take: 50);

Console.WriteLine("Done.");
