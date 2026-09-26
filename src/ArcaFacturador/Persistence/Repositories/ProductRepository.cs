using ArcaFacturador.Persistence.Models;
using Microsoft.Data.Sqlite;

namespace ArcaFacturador.Persistence.Repositories;

public sealed class ProductRepository(SqliteDatabase database)
{
    public ProductRecord Add(ProductRecord product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (product.Id != 0)
        {
            throw new ArgumentException("Un producto nuevo no puede tener un identificador asignado.", nameof(product));
        }

        Validate(product);

        using var connection = database.OpenConnection();
        EnsureUniquePrice(connection, product.UnitPriceCents, excludedId: null);

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO products (code, description, unit, unit_price_cents)
            VALUES ($code, $description, $unit, $unitPriceCents);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$code", product.Code);
        command.Parameters.AddWithValue("$description", product.Description);
        command.Parameters.AddWithValue("$unit", product.Unit);
        command.Parameters.AddWithValue("$unitPriceCents", product.UnitPriceCents);

        var id = (long)(command.ExecuteScalar()
            ?? throw new InvalidOperationException("No se pudo obtener el identificador del producto."));

        return product with { Id = id };
    }

    public ProductRecord Update(ProductRecord product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (product.Id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(product), "El producto debe tener un identificador válido.");
        }

        Validate(product);

        using var connection = database.OpenConnection();
        EnsureUniquePrice(connection, product.UnitPriceCents, product.Id);

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE products
            SET
                code = $code,
                description = $description,
                unit = $unit,
                unit_price_cents = $unitPriceCents
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", product.Id);
        command.Parameters.AddWithValue("$code", product.Code);
        command.Parameters.AddWithValue("$description", product.Description);
        command.Parameters.AddWithValue("$unit", product.Unit);
        command.Parameters.AddWithValue("$unitPriceCents", product.UnitPriceCents);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("No se encontró el importe frecuente indicado.");
        }

        return product;
    }

    public void Delete(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM products WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("No se encontró el importe frecuente indicado.");
        }
    }

    public ProductRecord? GetById(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, code, description, unit, unit_price_cents
            FROM products
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadProduct(reader) : null;
    }

    public IReadOnlyList<ProductRecord> GetAll()
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, code, description, unit, unit_price_cents
            FROM products
            ORDER BY id;
            """;

        using var reader = command.ExecuteReader();
        var products = new List<ProductRecord>();
        while (reader.Read())
        {
            products.Add(ReadProduct(reader));
        }

        return products;
    }

    private static ProductRecord ReadProduct(Microsoft.Data.Sqlite.SqliteDataReader reader) => new(
        reader.GetInt64(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetInt64(4));

    private static void EnsureUniquePrice(SqliteConnection connection, long unitPriceCents, long? excludedId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM products
            WHERE unit_price_cents = $unitPriceCents
              AND ($excludedId IS NULL OR id <> $excludedId);
            """;
        command.Parameters.AddWithValue("$unitPriceCents", unitPriceCents);
        command.Parameters.AddWithValue("$excludedId", (object?)excludedId ?? DBNull.Value);

        var count = (long)(command.ExecuteScalar() ?? 0L);
        if (count > 0)
        {
            throw new InvalidOperationException("Ya existe un importe frecuente con ese valor.");
        }
    }

    private static void Validate(ProductRecord product)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(product.Code);
        ArgumentException.ThrowIfNullOrWhiteSpace(product.Description);
        ArgumentException.ThrowIfNullOrWhiteSpace(product.Unit);

        if (product.UnitPriceCents <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(product),
                "El precio unitario debe ser mayor que cero.");
        }
    }
}
