This works right now, and it works well.
We need to refactor it.
For example, we need to separate the email sending into different components.

- Building the email body
- Communication with db2 to get the contact info of the email recipients
- Communication with the email server to send the email
- Some higher level component to handle all of these

We want to be able to send the emails, or have that part fail quietly, writing an error log to the Inbound files directory if something goes wrong.

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

