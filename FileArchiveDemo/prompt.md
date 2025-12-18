This works right now, and it works well.
We need to refactor it as a new application

For the new application, I want low level components (.cs) to handle individual actions, and higher level components (.cs) to coordinate the low level components.

Ideally, I want each component to be responsible for one thing.
For example, if one component reads the list of files from the directory, another component is responsible for doing the thing with each individual file.

I want you to give me the logical and conceptual breakdown of the various components that will be needed.

Let's make it an outline.

## Program.md

![[Program.md]]

## Data/Db2RecipientRepository.cs

![[Data/Db2RecipientRepository.md]]

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

## Sql/030_UniqueSha256.sql

![[Sql/030_UniqueSha256.md]]

