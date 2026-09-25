using System.Globalization;
using ArcaFacturador.Persistence.Models;
using Microsoft.Data.Sqlite;

namespace ArcaFacturador.Persistence.Repositories;

public sealed class InvoiceRepository(SqliteDatabase database)
{
    private const string DateFormat = "yyyy-MM-dd";

    public InvoiceRecord Add(InvoiceRecord invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.Id != 0)
        {
            throw new ArgumentException("Una factura nueva no puede tener un identificador asignado.", nameof(invoice));
        }

        Validate(invoice);

        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO invoices (
                receipt_number,
                issue_date,
                service_from,
                service_to,
                payment_due_date,
                amount_cents,
                status,
                cae,
                cae_expiration_date,
                pdf_path)
            VALUES (
                $receiptNumber,
                $issueDate,
                $serviceFrom,
                $serviceTo,
                $paymentDueDate,
                $amountCents,
                $status,
                $cae,
                $caeExpirationDate,
                $pdfPath);
            SELECT last_insert_rowid();
            """;
        AddParameters(command, invoice);

        var id = (long)(command.ExecuteScalar()
            ?? throw new InvalidOperationException("No se pudo obtener el identificador de la factura."));

        return invoice with { Id = id };
    }

    public InvoiceRecord? GetById(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                id,
                receipt_number,
                issue_date,
                service_from,
                service_to,
                payment_due_date,
                amount_cents,
                status,
                cae,
                cae_expiration_date,
                pdf_path
            FROM invoices
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadInvoice(reader) : null;
    }

    public IReadOnlyList<InvoiceRecord> GetAll()
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                id,
                receipt_number,
                issue_date,
                service_from,
                service_to,
                payment_due_date,
                amount_cents,
                status,
                cae,
                cae_expiration_date,
                pdf_path
            FROM invoices
            ORDER BY id;
            """;

        using var reader = command.ExecuteReader();
        var invoices = new List<InvoiceRecord>();
        while (reader.Read())
        {
            invoices.Add(ReadInvoice(reader));
        }

        return invoices;
    }

    private static void AddParameters(SqliteCommand command, InvoiceRecord invoice)
    {
        command.Parameters.AddWithValue("$receiptNumber", (object?)invoice.ReceiptNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("$issueDate", FormatDate(invoice.IssueDate));
        command.Parameters.AddWithValue("$serviceFrom", FormatDate(invoice.ServiceFrom));
        command.Parameters.AddWithValue("$serviceTo", FormatDate(invoice.ServiceTo));
        command.Parameters.AddWithValue("$paymentDueDate", FormatDate(invoice.PaymentDueDate));
        command.Parameters.AddWithValue("$amountCents", invoice.AmountCents);
        command.Parameters.AddWithValue("$status", invoice.Status.ToString());
        command.Parameters.AddWithValue("$cae", (object?)invoice.Cae ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$caeExpirationDate",
            invoice.CaeExpirationDate is { } caeExpirationDate
                ? FormatDate(caeExpirationDate)
                : DBNull.Value);
        command.Parameters.AddWithValue("$pdfPath", (object?)invoice.PdfPath ?? DBNull.Value);
    }

    private static InvoiceRecord ReadInvoice(SqliteDataReader reader) => new(
        reader.GetInt64(0),
        reader.IsDBNull(1) ? null : reader.GetInt64(1),
        ParseDate(reader.GetString(2)),
        ParseDate(reader.GetString(3)),
        ParseDate(reader.GetString(4)),
        ParseDate(reader.GetString(5)),
        reader.GetInt64(6),
        Enum.Parse<InvoiceStatus>(reader.GetString(7)),
        reader.IsDBNull(8) ? null : reader.GetString(8),
        reader.IsDBNull(9) ? null : ParseDate(reader.GetString(9)),
        reader.IsDBNull(10) ? null : reader.GetString(10));

    private static string FormatDate(DateOnly date) =>
        date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static DateOnly ParseDate(string value) =>
        DateOnly.ParseExact(value, DateFormat, CultureInfo.InvariantCulture);

    private static void Validate(InvoiceRecord invoice)
    {
        if (invoice.ReceiptNumber is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(invoice),
                "El número de comprobante debe ser mayor que cero cuando está informado.");
        }

        if (invoice.ServiceFrom > invoice.ServiceTo)
        {
            throw new ArgumentException(
                "La fecha de inicio del servicio no puede ser posterior a la fecha de finalización.",
                nameof(invoice));
        }

        if (invoice.AmountCents <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(invoice),
                "El importe debe ser mayor que cero.");
        }
    }
}
