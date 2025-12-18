using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

using FileArchiveDemo.Data;
using FileArchiveDemo.Email;
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

static void EnsureDb2ClientEnv(IConfiguration config)
{
    // Configurable so you don't recode per deployment
    var sqlLib = config["Db2:SqlLibDir"] ?? @"C:\Program Files\IBM\SQLLIB";
    var sqlLibBin = Path.Combine(sqlLib, "BIN");

    // PATH (native DLLs)
    var path = Environment.GetEnvironmentVariable("PATH") ?? "";
    if (!path.Contains(sqlLibBin, StringComparison.OrdinalIgnoreCase))
        Environment.SetEnvironmentVariable("PATH", sqlLibBin + ";" + path);

    // Helpful hints for DB2 native components
    Environment.SetEnvironmentVariable("DB2DIR", sqlLib);

    // Optional: only set if you want to force it (or leave blank in config)
    var instance = config["Db2:InstanceName"];
    if (!string.IsNullOrWhiteSpace(instance))
        Environment.SetEnvironmentVariable("DB2INSTANCE", instance);
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
// DB2 client env (must be early)
// ---------------------------
EnsureDb2ClientEnv(config);

// ---------------------------
// Email config
// ---------------------------
var emailOpt = new EmailOptions();
config.GetSection("Email").Bind(emailOpt);

if (string.IsNullOrWhiteSpace(emailOpt.SmtpHost) || string.IsNullOrWhiteSpace(emailOpt.FromAddress))
    throw new InvalidOperationException("Missing Email settings in appsettings.json (Email:SmtpHost / Email:FromAddress).");

var emailSender = new SmtpEmailSender(emailOpt);

// ---------------------------
// DB2 (Recipients) smoke test + SMTP test
// ---------------------------
var db2ConnString = config.GetConnectionString("Db2")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Db2");

Console.WriteLine("[DB2] ConnString (redacted) = " +
    Regex.Replace(db2ConnString, @"PWD=[^;]*", "PWD=***"));

var db2Schema = config["Db2:Schema"] ?? "FILEARCHIVE";
var db2Table = config["Db2:RecipientsTable"] ?? "EMAIL_RECIPIENT";

var db2Repo = new Db2RecipientRepository(db2ConnString, db2Schema, db2Table);

try
{
    var emails = await db2Repo.GetActiveEmailsAsync();
    var startedUtc = DateTime.UtcNow;

    // ... run ingest + pdf conversion here ...

    var finishedUtc = DateTime.UtcNow;

    var body = FileArchiveDemo.Email.ReportBuilder.BuildPdfRunReport(
        startedUtc: startedUtc,
        finishedUtc: finishedUtc,
        ingestedCount: 0,          // wire these up next
        pendingBefore: 0,          // wire these up next
        convertedSuccess: 0,       // wire these up next
        convertedFailed: 0,        // wire these up next
        convertedFiles: new[] { "Alpha.doc -> Alpha.pdf" }, // demo sample
        failedFiles: Array.Empty<string>()
    );

    await emailSender.SendAsync(
        toEmails: emails,
        subject: "FileArchiveDemo - PDF Conversion Report",
        bodyText: body
    );
    Console.WriteLine("[EMAIL] Sent SMTP test email to: " + string.Join(", ", emails));
}
catch (IBM.Data.Db2.DB2Exception ex)
{
    Console.WriteLine("[DB2] DB2Exception: " + ex);
    foreach (IBM.Data.Db2.DB2Error err in ex.Errors)
        Console.WriteLine($"[DB2] SQLSTATE={err.SQLState} SQLCODE={err.NativeError} MSG={err.Message}");

    throw;
}

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
Console.WriteLine($"DB Label: {where.Db}");
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
