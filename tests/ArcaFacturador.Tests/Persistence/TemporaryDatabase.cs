using ArcaFacturador.Persistence;

namespace ArcaFacturador.Tests.Persistence;

internal sealed class TemporaryDatabase : IDisposable
{
    public TemporaryDatabase(bool initialize = true)
    {
        DirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "ArcaFacturador.Tests",
            Guid.NewGuid().ToString("N"));
        Database = new SqliteDatabase(Path.Combine(DirectoryPath, "test.db"));

        if (initialize)
        {
            Database.Initialize();
        }
    }

    public string DirectoryPath { get; }

    public SqliteDatabase Database { get; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
