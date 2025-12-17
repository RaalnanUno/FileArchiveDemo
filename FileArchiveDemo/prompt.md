BASE DIR = C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\bin\Debug\net8.0\
DB PATH  = C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\bin\Debug\net8.0\Data\FileArchiveDemo.db
SQLite Version: 3.41.2
DB Label:       FileArchiveDemo.db
Database ready. Demo bootstrap complete.
[INGEST] Files found:
  - C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\bin\Debug\net8.0\InboundFiles\Bug_12767_CBC_EDD_1.0.24.docx
  - C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\bin\Debug\net8.0\InboundFiles\EDD_12467-CBC_Responsive_Mission_Scroller.docx
[INGEST] Found 2 file(s) in: C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\bin\Debug\net8.0\InboundFiles
[INGEST] Inserting: Bug_12767_CBC_EDD_1.0.24.docx (318931 bytes)
[INGEST] Inserted row id=3
[INGEST] Kept original. Row Id=3
[INGEST] Inserting: EDD_12467-CBC_Responsive_Mission_Scroller.docx (370135 bytes)
[INGEST] Inserted row id=4
[INGEST] Kept original. Row Id=4
[INGEST] Completed. Success=2/2
[PDF] SofficePath = soffice.exe
[PDF] Pending rows: 2
[PDF] Converting Id=3 File=Bug_12767_CBC_EDD_1.0.24.docx
[PDF] SKIP (converter missing) Id=3: An error occurred trying to start process 'soffice.exe' with working directory 'Temp\FileArchivePdf\43edebd4294c4565a03480ac14dd64bc'. The system cannot find the file specified.
[PDF] Converting Id=4 File=EDD_12467-CBC_Responsive_Mission_Scroller.docx
[PDF] SKIP (converter missing) Id=4: An error occurred trying to start process 'soffice.exe' with working directory 'Temp\FileArchivePdf\a5d39bf1d2284927ba62fc230b6fdf06'. The system cannot find the file specified.
[PDF] Completed. Success=0/2
Done.

C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\bin\Debug\net8.0\FileArchiveDemo.exe (process 29596) exited with code 0 (0x0).
To automatically close the console when debugging stops, enable Tools->Options->Debugging->Automatically close the console when debugging stops.
Press any key to close this window . . 

---
I can see that the database is being populated with the file BLOB, but the PDF BLOB is still null.
---

## Data/FileArchiveRepository.cs

![[Data/FileArchiveRepository.md]]

## Data/SqlBootstrapper.cs

![[Data/SqlBootstrapper.md]]

## Ingest/FileIngestor.cs

![[Ingest/FileIngestor.md]]

## Sql/000_CreateDatabase.sql

![[Sql/000_CreateDatabase.md]]

## Sql/001_SchemaMigrations.sql

![[Sql/001_SchemaMigrations.md]]

## Sql/010_FileArchive.sql

![[Sql/010_FileArchive.md]]

## Sql/020_Indexes.sql

![[Sql/020_Indexes.md]]

