using Microsoft.Data.Sqlite;

namespace ArcaFacturador.Tests.Persistence;

public class SqliteDatabaseTests
{
    [Fact]
    public void Initialize_CreatesOnlyTheRequiredApplicationTables()
    {
        using var temporaryDatabase = new TemporaryDatabase(initialize: false);

        temporaryDatabase.Database.Initialize();

        Assert.True(File.Exists(temporaryDatabase.Database.DatabasePath));

        using var connection = new SqliteConnection(
            $"Data Source={temporaryDatabase.Database.DatabasePath};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name;
            """;
        using var reader = command.ExecuteReader();
        var tableNames = new List<string>();
        while (reader.Read())
        {
            tableNames.Add(reader.GetString(0));
        }

        Assert.Equal(["invoices", "products", "settings"], tableNames);
    }

    [Fact]
    public void Initialize_CanBeCalledMoreThanOnce()
    {
        using var temporaryDatabase = new TemporaryDatabase();

        var exception = Record.Exception(temporaryDatabase.Database.Initialize);

        Assert.Null(exception);
    }
}
