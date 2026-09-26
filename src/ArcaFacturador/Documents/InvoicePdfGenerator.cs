using System.Globalization;
using System.IO;
using System.Text;
using ArcaFacturador.Domain;
using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Documents;

public sealed class InvoicePdfGenerator
{
    private static readonly CultureInfo ArgentineCulture = CultureInfo.GetCultureInfo("es-AR");
    private static readonly Encoding PdfEncoding = Encoding.ASCII;

    public string Generate(InvoiceRecord invoice, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        if (invoice.Id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(invoice), "La factura debe estar guardada antes de generar el PDF.");
        }

        var fullPath = Path.GetFullPath(outputPath);
        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllBytes(fullPath, BuildPdf(BuildContentStream(invoice)));
        return fullPath;
    }

    private static string BuildContentStream(InvoiceRecord invoice)
    {
        var lines = new[]
        {
            new PdfLine(22, "FACTURA C"),
            new PdfLine(10, GetDocumentStatus(invoice)),
            new PdfLine(12, $"Comprobante local: {invoice.Id:00000000}"),
            new PdfLine(12, $"Numero fiscal: {FormatOptionalReceiptNumber(invoice.ReceiptNumber)}"),
            new PdfLine(12, $"Fecha de emision: {FormatDate(invoice.IssueDate)}"),
            new PdfLine(12, $"Periodo de servicio: {FormatDate(invoice.ServiceFrom)} al {FormatDate(invoice.ServiceTo)}"),
            new PdfLine(12, $"Vencimiento de pago: {FormatDate(invoice.PaymentDueDate)}"),
            new PdfLine(12, "CUIT emisor: Pendiente de configuracion local"),
            new PdfLine(12, "Punto de venta: Pendiente de configuracion local"),
            new PdfLine(12, $"Receptor: {InvoiceDefaults.CustomerType}"),
            new PdfLine(12, $"Medio de pago: {InvoiceDefaults.PaymentMethod}"),
            new PdfLine(12, $"Concepto: {InvoiceDefaults.Concept}"),
            new PdfLine(12, $"Codigo: {InvoiceDefaults.ProductCode}"),
            new PdfLine(12, $"Servicio: {InvoiceDefaults.ProductDescription}"),
            new PdfLine(12, $"Cantidad: {InvoiceDefaults.Quantity:N0}"),
            new PdfLine(12, $"Unidad: {InvoiceDefaults.Unit}"),
            new PdfLine(14, $"Importe total: {FormatAmount(invoice.AmountCents)}"),
            new PdfLine(12, $"CAE: {FormatOptionalText(invoice.Cae, "Pendiente de autorizacion")}"),
            new PdfLine(12, $"Vencimiento CAE: {FormatOptionalDate(invoice.CaeExpirationDate)}"),
            new PdfLine(10, GetFooterStatus(invoice)),
        };

        var builder = new StringBuilder();
        builder.AppendLine("BT");

        var y = 790;
        foreach (var line in lines)
        {
            builder.AppendLine($"/F1 {line.FontSize} Tf");
            builder.Append("1 0 0 1 50 ");
            builder.Append(y.ToString(CultureInfo.InvariantCulture));
            builder.Append(" Tm (");
            builder.Append(EscapePdfText(Sanitize(line.Text)));
            builder.AppendLine(") Tj");
            y -= line.FontSize == 22 ? 30 : 22;
        }

        builder.AppendLine("ET");
        return builder.ToString();
    }

    private static byte[] BuildPdf(string contentStream)
    {
        var contentLength = PdfEncoding.GetByteCount(contentStream);
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {contentLength} >>\nstream\n{contentStream}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };

        using var stream = new MemoryStream();
        Write(stream, "%PDF-1.4\n");

        var offsets = new List<long>();
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            Write(stream, $"{index + 1} 0 obj\n");
            Write(stream, objects[index]);
            Write(stream, "\nendobj\n");
        }

        var xrefPosition = stream.Position;
        Write(stream, $"xref\n0 {objects.Length + 1}\n");
        Write(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            Write(stream, $"{offset:0000000000} 00000 n \n");
        }

        Write(stream, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\n");
        Write(stream, $"startxref\n{xrefPosition}\n%%EOF\n");
        return stream.ToArray();
    }

    private static string FormatDate(DateOnly date) =>
        date.ToString("dd/MM/yyyy", ArgentineCulture);

    private static string FormatAmount(long amountCents) =>
        (amountCents / 100m).ToString("C2", ArgentineCulture);

    private static string FormatOptionalReceiptNumber(long? receiptNumber) =>
        receiptNumber.HasValue ? receiptNumber.Value.ToString("00000000", CultureInfo.InvariantCulture) : "Pendiente";

    private static string FormatOptionalDate(DateOnly? date) =>
        date.HasValue ? FormatDate(date.Value) : "Pendiente";

    private static string FormatOptionalText(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string GetDocumentStatus(InvoiceRecord invoice) =>
        invoice.Status == InvoiceStatus.Authorized
            ? "Comprobante autorizado por ARCA"
            : "Vista previa local - no autorizada por ARCA";

    private static string GetFooterStatus(InvoiceRecord invoice) =>
        invoice.Status == InvoiceStatus.Authorized
            ? "Este documento contiene CAE informado por ARCA."
            : "Este documento local no reemplaza la autorizacion fiscal de ARCA.";

    private static string Sanitize(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character <= 127 ? character : ' ');
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string EscapePdfText(string text) =>
        text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    private static void Write(Stream stream, string value)
    {
        var bytes = PdfEncoding.GetBytes(value);
        stream.Write(bytes);
    }

    private sealed record PdfLine(int FontSize, string Text);
}
