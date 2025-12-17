# FileArchiveDemo

**FileArchiveDemo** is a self-contained .NET console application that ingests files into a local SQLite database, stores their binary contents (BLOBs), and optionally converts supported documents (e.g. DOCX) into PDFs using **LibreOffice in headless mode**.

The project is designed to be:
- ✅ Easy to clone and run
- ✅ Free of external database dependencies
- ✅ Deterministic and GitHub-friendly
- ✅ A clean foundation for production upgrades (e.g. Aspose, cloud storage)

---

## ✨ Features

- 📁 **File ingestion**
  - Reads files from a configurable folder
  - Stores original file BLOBs in SQLite
  - Computes and stores SHA-256 hashes

- 🗄️ **Embedded SQLite database**
  - Database file auto-created on first run
  - Embedded SQL migrations
  - No SQL Server, Docker, or LocalDB required

- 📄 **PDF backfill pipeline**
  - Converts pending documents to PDF via LibreOffice (`soffice.exe`)
  - Stores generated PDFs as BLOBs
  - Gracefully skips conversion if LibreOffice is not installed

- 🧭 **Deterministic paths**
  - Database, temp folders, and ingest folders resolve relative to the executable
  - Works consistently in Visual Studio, `dotnet run`, and CI

---

## 📦 Requirements

- .NET 8 SDK
- (Optional) **LibreOffice** for PDF conversion  
  - https://www.libreoffice.org/download/download/

> If LibreOffice is not installed, the app still runs — files are ingested and PDF conversion is skipped until LibreOffice becomes available.

---

## 🚀 Getting Started

### 1. Clone the repo

```bash
git clone https://github.com/your-org/FileArchiveDemo.git
cd FileArchiveDemo
````

### 2. Restore & build

```bash
dotnet restore
dotnet build
```

### 3. Prepare input files

Copy files you want to ingest into:

```
FileArchiveDemo/bin/Debug/net8.0/InboundFiles
```

(You can change this path in `appsettings.json`.)

### 4. Run the app

```bash
dotnet run
```

---

## ⚙️ Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "FileArchiveDb": "Data/FileArchiveDemo.db"
  },
  "Ingest": {
    "WatchPath": "InboundFiles",
    "DeleteAfterInsert": false
  },
  "Pdf": {
    "TempPath": "Temp/FileArchivePdf",
    "SofficePath": "C:\\Program Files\\LibreOffice\\program\\soffice.exe",
    "TimeoutSeconds": 60
  }
}
```

### Notes

* Paths may be **relative** (resolved against the executable directory)
* `SofficePath` is optional if LibreOffice is already on `PATH`
* The SQLite DB file is created automatically

---

## 🗂️ Database Location

On first run, the SQLite database is created at:

```
bin/Debug/net8.0/Data/FileArchiveDemo.db
```

You can inspect it using:

* **DB Browser for SQLite**
* **VS Code SQLite extension**
* **Azure Data Studio (SQLite extension)**

> This database file is intentionally **not committed** to Git.

---

## 🔄 PDF Conversion Behavior

* Only rows with `PdfStatus = 0` and `PdfBlob IS NULL` are processed
* Successful conversions:

  * Store PDF BLOB
  * Set `PdfStatus = 1`
* If LibreOffice is missing:

  * Conversion is skipped
  * Rows remain pending (safe to retry later)

---

## 🧱 Project Structure

```
FileArchiveDemo/
│
├─ Data/
│  ├─ FileArchiveRepository.cs
│  ├─ SqlBootstrapper.cs
│
├─ Ingest/
│  └─ FileIngestor.cs
│
├─ Pdf/
│  ├─ LibreOfficeHeadlessPdfConverter.cs
│  ├─ LibreOfficeLocator.cs
│  └─ PdfBackfillService.cs
│
├─ Sql/
│  ├─ 001_SchemaMigrations.sql
│  ├─ 010_FileArchive.sql
│  └─ 020_Indexes.sql
│
├─ Program.cs
├─ appsettings.json
└─ README.md
```

---

## 🔮 Future Enhancements

* Streaming BLOB ingestion for very large files
* Aspose PDF converter swap-in for production
* File deduplication by hash
* Background worker / queue processing
* API wrapper (ASP.NET / Minimal API)
* External object storage (S3 / Azure Blob)

---

## 📜 License

MIT — use it, fork it, break it, improve it.

---

## 💬 Final Note

This project intentionally prioritizes **developer experience and portability**.
If it runs on your machine after a `git clone`, the architecture is doing its job.
