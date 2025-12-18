```cs
using IBM.Data.Db2;

namespace FileArchiveDemo.Data;

public sealed class Db2RecipientRepository
{
    private readonly string _connString;
    private readonly string _schema;
    private readonly string _table;

    public Db2RecipientRepository(string connString, string schema, string table)
    {
        _connString = connString;
        _schema = schema;
        _table = table;
    }

    public async Task<List<string>> GetActiveEmailsAsync()
    {
        var sql = $@"
SELECT EMAIL
FROM {_schema}.{_table}
WHERE IS_ACTIVE = 1
ORDER BY ID
";

        var results = new List<string>();

        await using var conn = new DB2Connection(_connString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            results.Add(r.GetString(0));

        return results;
    }
}
```

IBM.Data.Db2.DB2Exception
  HResult=0x80004005
  Message=ERROR [08001] [IBM] SQL30081N  A communication error has been detected. Communication protocol being used: "TCP/IP".  Communication API being used: "SOCKETS".  Location where the error was detected: "127.0.0.1".  Communication function detecting the error: "recv".  Protocol specific error code(s): "*", "*", "0".  SQLSTATE=08001

  Source=IBM.Data.Db2
  StackTrace:
   at IBM.Data.Db2.DB2ConnPool.Open(DB2Connection connection, String& szConnectionString, DB2ConnSettings& ppSettings, Object& ppConn)
   at IBM.Data.Db2.DB2Connection.Open()
   at System.Data.Common.DbConnection.OpenAsync(CancellationToken cancellationToken)
--- End of stack trace from previous location ---
   at FileArchiveDemo.Data.Db2RecipientRepository.<GetActiveEmailsAsync>d__4.MoveNext() in C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\Data\Db2RecipientRepository.cs:line 30
   at FileArchiveDemo.Data.Db2RecipientRepository.<GetActiveEmailsAsync>d__4.MoveNext() in C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\Data\Db2RecipientRepository.cs:line 39
   at Program.<<Main>$>d__0.MoveNext() in C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\Program.cs:line 133


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

---


C:\Program Files\IBM\SQLLIB\BIN>db2set -all | findstr /I DB2COMM

C:\Program Files\IBM\SQLLIB\BIN>db2set DB2COMM=TCPIP

C:\Program Files\IBM\SQLLIB\BIN>db2stop force
SQL1064N  DB2STOP processing was successful.

C:\Program Files\IBM\SQLLIB\BIN>db2start
SQL1063N  DB2START processing was successful.

C:\Program Files\IBM\SQLLIB\BIN>db2 get dbm cfg | findstr /I SVCENAME
 TCP/IP Service name                          (SVCENAME) = db2c_DB2
 SSL service name                         (SSL_SVCENAME) =

C:\Program Files\IBM\SQLLIB\BIN>db2 update dbm cfg using SVCENAME db2c_DB2
DB20000I  The UPDATE DATABASE MANAGER CONFIGURATION command completed
successfully.
SQL1362W  One or more of the parameters submitted for immediate modification
were not changed dynamically. Client changes will not be effective until the
next time the application is started or the TERMINATE command has been issued.
Server changes will not be effective until the next DB2START command.

C:\Program Files\IBM\SQLLIB\BIN>db2stop force
SQL1064N  DB2STOP processing was successful.

C:\Program Files\IBM\SQLLIB\BIN>db2start
SQL1063N  DB2START processing was successful.

C:\Program Files\IBM\SQLLIB\BIN>db2 update dbm cfg using SVCENAME db2c_DB2
DB20000I  The UPDATE DATABASE MANAGER CONFIGURATION command completed
successfully.
SQL1362W  One or more of the parameters submitted for immediate modification
were not changed dynamically. Client changes will not be effective until the
next time the application is started or the TERMINATE command has been issued.
Server changes will not be effective until the next DB2START command.

C:\Program Files\IBM\SQLLIB\BIN>db2stop force
SQL1064N  DB2STOP processing was successful.

C:\Program Files\IBM\SQLLIB\BIN>db2start
SQL1063N  DB2START processing was successful.

C:\Program Files\IBM\SQLLIB\BIN>

---

IBM.Data.Db2.DB2Exception
  HResult=0x80004005
  Message=ERROR [42501] [IBM][DB2/NT64] SQL0551N  The statement failed because the authorization ID does not have the required authorization or privilege to perform the operation.  Authorization ID: "RAALNAN5".  Operation: "SELECT". Object: "FILEARCHIVE.EMAIL_RECIPIENT".
  Source=IBM.Data.Db2
  StackTrace:
   at IBM.Data.Db2.DB2Command.ExecuteReaderObject(CommandBehavior behavior, String method, DB2CursorType reqCursorType, Boolean abortOnOptValueChg, Boolean skipDeleted, Boolean isResultSet, Int32 maxRows, Boolean skipInitialValidation)
   at IBM.Data.Db2.DB2Command.ExecuteReaderObject(CommandBehavior behavior, String method)
   at IBM.Data.Db2.DB2Command.ExecuteReader(CommandBehavior behavior)
   at IBM.Data.Db2.DB2Command.ExecuteDbDataReader(CommandBehavior behavior)
   at System.Data.Common.DbCommand.ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
--- End of stack trace from previous location ---
   at FileArchiveDemo.Data.Db2RecipientRepository.<GetActiveEmailsAsync>d__4.MoveNext() in C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\Data\Db2RecipientRepository.cs:line 35
   at FileArchiveDemo.Data.Db2RecipientRepository.<GetActiveEmailsAsync>d__4.MoveNext() in C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\Data\Db2RecipientRepository.cs:line 39
   at FileArchiveDemo.Data.Db2RecipientRepository.<GetActiveEmailsAsync>d__4.MoveNext() in C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\Data\Db2RecipientRepository.cs:line 39
   at Program.<<Main>$>d__0.MoveNext() in C:\source\repos\RaalnanUno\FileArchiveDemo\FileArchiveDemo\Program.cs:line 133
