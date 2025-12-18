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
