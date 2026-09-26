using System.IO;
using Microsoft.Data.Sqlite;

namespace ArcaFacturador.Persistence;

public sealed class SqliteDatabase
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS settings (
            key TEXT PRIMARY KEY NOT NULL CHECK (length(trim(key)) > 0),
            value TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS products (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            code TEXT NOT NULL CHECK (length(trim(code)) > 0),
            description TEXT NOT NULL CHECK (length(trim(description)) > 0),
            unit TEXT NOT NULL CHECK (length(trim(unit)) > 0),
            unit_price_cents INTEGER NOT NULL CHECK (unit_price_cents > 0)
        );

        CREATE TABLE IF NOT EXISTS invoices (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            receipt_number INTEGER NULL UNIQUE CHECK (receipt_number > 0),
            issue_date TEXT NOT NULL,
            service_from TEXT NOT NULL,
            service_to TEXT NOT NULL CHECK (service_from <= service_to),
            payment_due_date TEXT NOT NULL,
            amount_cents INTEGER NOT NULL CHECK (amount_cents > 0),
            status TEXT NOT NULL CHECK (status IN ('Pending', 'Authorized', 'Rejected')),
            cae TEXT NULL,
            cae_expiration_date TEXT NULL,
            pdf_path TEXT NULL
        );

        PRAGMA user_version = 1;
        """;

    private readonly string _connectionString;

    public SqliteDatabase(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        DatabasePath = Path.GetFullPath(databasePath);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString();
    }

    public string DatabasePath { get; }

    public void Initialize()
    {
        var directoryPath = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = Schema;
        command.ExecuteNonQuery();
    }

    internal SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
        command.ExecuteNonQuery();

        return connection;
    }
}
