using System.Globalization;
using System.IO;
using System.Text;
using ArcaFacturador.Domain;
using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Documents;

public sealed class InvoicePdfGenerator
{
    private static readonly CultureInfo ArgentineCulture = CultureInfo.GetCultureInfo("es-AR");
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
    private static readonly Encoding PdfEncoding = Encoding.ASCII;

    public string Generate(InvoiceRecord invoice, string outputPath, InvoicePdfIssuerData? issuerData = null)
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

        File.WriteAllBytes(fullPath, BuildPdf(invoice, issuerData ?? InvoicePdfIssuerData.Pending));
        return fullPath;
    }

    private static byte[] BuildPdf(InvoiceRecord invoice, InvoicePdfIssuerData issuerData)
    {
        var copies = invoice.Status == InvoiceStatus.Authorized
            ? new[] { "ORIGINAL", "DUPLICADO", "TRIPLICADO" }
            : new[] { "BORRADOR" };
        var contentStreams = copies
            .Select(copy => BuildPageContent(invoice, issuerData, copy))
            .ToArray();

        return BuildPdfDocument(contentStreams);
    }

    private static string BuildPageContent(InvoiceRecord invoice, InvoicePdfIssuerData issuerData, string copyTitle)
    {
        var writer = new PdfContentWriter();
        var total = FormatAmountValue(invoice.AmountCents);
        var receiptNumber = FormatReceiptNumber(invoice.ReceiptNumber);
        var pointOfSale = issuerData.PointOfSale.HasValue
            ? issuerData.PointOfSale.Value.ToString("00000", InvariantCulture)
            : "Pendiente";

        writer.Font(18, bold: true);
        writer.TextCentered(297, 786, copyTitle);
        writer.Box(28, 770, 539, 34);

        writer.Line(300, 630, 300, 770);
        writer.Box(276, 728, 48, 42);
        writer.Font(28, bold: true);
        writer.TextCentered(296, 748, "C");
        writer.Font(8, bold: true);
        writer.TextCentered(296, 734, "COD. 011");

        writer.Font(13, bold: true);
        writer.Text(52, 742, issuerData.FantasyName);
        writer.Font(10, bold: true);
        writer.Text(42, 704, "Razon Social:");
        writer.Font(10);
        writer.Text(125, 704, issuerData.BusinessName);
        writer.Font(10, bold: true);
        writer.Text(42, 681, "Domicilio Comercial:");
        writer.Font(10);
        writer.Text(155, 681, issuerData.AddressLine1);
        writer.Text(155, 666, issuerData.AddressLine2);
        writer.Font(10, bold: true);
        writer.Text(42, 642, "Condicion frente al IVA:");
        writer.Font(10, bold: true);
        writer.Text(172, 642, issuerData.VatCondition);

        writer.Font(22, bold: true);
        writer.Text(340, 742, "FACTURA");
        writer.Font(10, bold: true);
        writer.Text(340, 718, "Punto de Venta:");
        writer.Text(430, 718, pointOfSale);
        writer.Text(470, 718, "Comp. Nro:");
        writer.Text(535, 718, receiptNumber);
        writer.Text(340, 696, "Fecha de Emision:");
        writer.Text(440, 696, FormatDate(invoice.IssueDate));
        writer.Text(340, 672, "CUIT:");
        writer.Font(10);
        writer.Text(378, 672, issuerData.Cuit);
        writer.Font(10, bold: true);
        writer.Text(340, 657, "Ingresos Brutos:");
        writer.Font(10);
        writer.Text(425, 657, issuerData.GrossIncome);
        writer.Font(10, bold: true);
        writer.Text(340, 642, "Fecha de Inicio de Actividades:");
        writer.Font(10);
        writer.Text(498, 642, issuerData.ActivityStartDate);

        writer.Box(28, 606, 539, 24);
        writer.Font(11, bold: true);
        writer.Text(42, 614, "Periodo Facturado Desde:");
        writer.Font(11);
        writer.Text(190, 614, FormatDate(invoice.ServiceFrom));
        writer.Font(11, bold: true);
        writer.Text(274, 614, "Hasta:");
        writer.Font(11);
        writer.Text(322, 614, FormatDate(invoice.ServiceTo));
        writer.Font(11, bold: true);
        writer.Text(405, 614, "Vto. pago:");
        writer.Font(11);
        writer.Text(545, 614, FormatDate(invoice.PaymentDueDate), alignRight: true);

        writer.Box(28, 542, 539, 62);
        writer.Font(9, bold: true);
        writer.Text(42, 588, "Doc.:");
        writer.Font(9);
        writer.Text(72, 588, "-");
        writer.Font(9, bold: true);
        writer.Text(230, 588, "Apellido y Nombre / Razon Social:");
        writer.Text(42, 566, "Condicion frente al IVA:");
        writer.Font(9);
        writer.Text(160, 566, InvoiceDefaults.CustomerType);
        writer.Font(9, bold: true);
        writer.Text(42, 544, "Condicion de venta:");
        writer.Font(9);
        writer.Text(145, 544, InvoiceDefaults.PaymentMethod);

        DrawItemsTable(writer, total);
        DrawTotals(writer, total);
        DrawAuthorizationFooter(writer, invoice);

        return writer.Build();
    }

    private static void DrawItemsTable(PdfContentWriter writer, string total)
    {
        writer.FillGray(0.82);
        writer.FilledBox(28, 522, 539, 19);
        writer.FillGray(0);
        writer.StrokeGray(0);
        writer.Box(28, 522, 539, 19);
        writer.Line(66, 522, 66, 541);
        writer.Line(194, 522, 194, 541);
        writer.Line(258, 522, 258, 541);
        writer.Line(300, 522, 300, 541);
        writer.Line(380, 522, 380, 541);
        writer.Line(420, 522, 420, 541);
        writer.Line(490, 522, 490, 541);
        writer.Font(8, bold: true);
        writer.TextCentered(47, 529, "Codigo");
        writer.Text(72, 529, "Producto / Servicio");
        writer.TextCentered(226, 529, "Cantidad");
        writer.TextCentered(279, 529, "U. Medida");
        writer.TextCentered(340, 529, "Precio Unit.");
        writer.TextCentered(400, 529, "% Bonif");
        writer.TextCentered(455, 529, "Imp. Bonif.");
        writer.TextCentered(528, 529, "Subtotal");

        writer.Font(8);
        writer.Text(36, 506, InvoiceDefaults.ProductCode);
        writer.Text(72, 506, InvoiceDefaults.ProductDescription);
        writer.Text(250, 506, "1,00", alignRight: true);
        writer.TextCentered(279, 506, "otras");
        writer.TextCentered(279, 494, "unidades");
        writer.Text(374, 506, total, alignRight: true);
        writer.Text(414, 506, "0,00", alignRight: true);
        writer.Text(484, 506, "0,00", alignRight: true);
        writer.Text(560, 506, total, alignRight: true);
    }

    private static void DrawTotals(PdfContentWriter writer, string total)
    {
        writer.Box(28, 222, 539, 96);
        writer.Font(10, bold: true);
        writer.Text(470, 280, "Subtotal: $", alignRight: true);
        writer.Text(558, 280, total, alignRight: true);
        writer.Text(470, 258, "Importe Otros Tributos: $", alignRight: true);
        writer.Text(558, 258, "0,00", alignRight: true);
        writer.Font(12, bold: true);
        writer.Text(470, 235, "Importe Total: $", alignRight: true);
        writer.Text(558, 235, total, alignRight: true);
    }

    private static void DrawAuthorizationFooter(PdfContentWriter writer, InvoiceRecord invoice)
    {
        writer.Box(54, 110, 64, 64);
        writer.Font(6);
        writer.TextCentered(86, 145, "QR");
        writer.TextCentered(86, 135, "ARCA");
        writer.Font(20, bold: true);
        writer.Text(140, 152, "ARCA");
        writer.Font(6);
        writer.Text(140, 141, "AGENCIA DE RECAUDACION");
        writer.Text(140, 132, "Y CONTROL ADUANERO");
        writer.Font(10, bold: true);
        writer.Text(140, 112, invoice.Status == InvoiceStatus.Authorized ? "Comprobante Autorizado" : "Vista previa local");
        writer.Font(7, bold: true);
        writer.Text(140, 95, "Esta Agencia no se responsabiliza por los datos ingresados en el detalle de la operacion");

        writer.Font(10, bold: true);
        writer.TextCentered(297, 136, "Pag. 1/1");
        writer.Text(435, 136, "CAE Nro:");
        writer.Font(10);
        writer.Text(558, 136, FormatOptionalText(invoice.Cae, "Pendiente"), alignRight: true);
        writer.Font(10, bold: true);
        writer.Text(435, 120, "Vto. CAE:");
        writer.Font(10);
        writer.Text(522, 120, FormatOptionalDate(invoice.CaeExpirationDate));
    }

    private static byte[] BuildPdfDocument(IReadOnlyList<string> contentStreams)
    {
        var pageCount = contentStreams.Count;
        var fontObjectNumber = 3 + (pageCount * 2);
        var pageObjectNumbers = Enumerable.Range(0, pageCount)
            .Select(index => 3 + index)
            .ToArray();
        var contentObjectNumbers = Enumerable.Range(0, pageCount)
            .Select(index => 3 + pageCount + index)
            .ToArray();

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(number => $"{number} 0 R"))}] /Count {pageCount} >>",
        };

        for (var index = 0; index < pageCount; index++)
        {
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontObjectNumber} 0 R /F2 {fontObjectNumber + 1} 0 R >> >> /Contents {contentObjectNumbers[index]} 0 R >>");
        }

        foreach (var contentStream in contentStreams)
        {
            var contentLength = PdfEncoding.GetByteCount(contentStream);
            objects.Add($"<< /Length {contentLength} >>\nstream\n{contentStream}endstream");
        }

        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

        using var stream = new MemoryStream();
        Write(stream, "%PDF-1.4\n");

        var offsets = new List<long>();
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            Write(stream, $"{index + 1} 0 obj\n");
            Write(stream, objects[index]);
            Write(stream, "\nendobj\n");
        }

        var xrefPosition = stream.Position;
        Write(stream, $"xref\n0 {objects.Count + 1}\n");
        Write(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            Write(stream, $"{offset:0000000000} 00000 n \n");
        }

        Write(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        Write(stream, $"startxref\n{xrefPosition}\n%%EOF\n");
        return stream.ToArray();
    }

    private static string FormatDate(DateOnly date) =>
        date.ToString("dd/MM/yyyy", ArgentineCulture);

    private static string FormatAmountValue(long amountCents) =>
        (amountCents / 100m).ToString("N2", ArgentineCulture);

    private static string FormatReceiptNumber(long? receiptNumber) =>
        receiptNumber.HasValue ? receiptNumber.Value.ToString("00000000", InvariantCulture) : "Pendiente";

    private static string FormatOptionalDate(DateOnly? date) =>
        date.HasValue ? FormatDate(date.Value) : "Pendiente";

    private static string FormatOptionalText(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

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

    private sealed class PdfContentWriter
    {
        private readonly StringBuilder _builder = new();

        public PdfContentWriter()
        {
            _builder.AppendLine("0 G");
            _builder.AppendLine("1 w");
        }

        public void Font(int size, bool bold = false)
        {
            _builder.AppendLine($"/{(bold ? "F2" : "F1")} {size} Tf");
        }

        public void Text(double x, double y, string text, bool alignRight = false)
        {
            var sanitized = EscapePdfText(Sanitize(text));
            if (alignRight)
            {
                var approximateWidth = sanitized.Length * 4.8;
                x -= approximateWidth;
            }

            _builder.AppendLine("BT");
            _builder.AppendLine($"1 0 0 1 {FormatNumber(x)} {FormatNumber(y)} Tm ({sanitized}) Tj");
            _builder.AppendLine("ET");
        }

        public void TextCentered(double x, double y, string text)
        {
            var sanitized = Sanitize(text);
            var approximateWidth = sanitized.Length * 4.8;
            Text(x - (approximateWidth / 2), y, sanitized);
        }

        public void Box(double x, double y, double width, double height)
        {
            _builder.AppendLine($"{FormatNumber(x)} {FormatNumber(y)} {FormatNumber(width)} {FormatNumber(height)} re S");
        }

        public void FilledBox(double x, double y, double width, double height)
        {
            _builder.AppendLine($"{FormatNumber(x)} {FormatNumber(y)} {FormatNumber(width)} {FormatNumber(height)} re f");
        }

        public void Line(double x1, double y1, double x2, double y2)
        {
            _builder.AppendLine($"{FormatNumber(x1)} {FormatNumber(y1)} m {FormatNumber(x2)} {FormatNumber(y2)} l S");
        }

        public void FillGray(double gray)
        {
            _builder.AppendLine($"{FormatNumber(gray)} g");
        }

        public void StrokeGray(double gray)
        {
            _builder.AppendLine($"{FormatNumber(gray)} G");
        }

        public string Build() => _builder.ToString();

        private static string FormatNumber(double value) =>
            value.ToString("0.###", InvariantCulture);
    }
}

public sealed record InvoicePdfIssuerData(
    string Cuit,
    int? PointOfSale,
    string BusinessName,
    string FantasyName,
    string AddressLine1,
    string AddressLine2,
    string VatCondition,
    string GrossIncome,
    string ActivityStartDate)
{
    public static InvoicePdfIssuerData Pending { get; } = new(
        "Pendiente de configuracion local",
        null,
        "BRENDA MANSILLA",
        "BRENDA MANSILLA",
        "Pendiente de configuracion local",
        string.Empty,
        "Responsable Monotributo",
        "Pendiente",
        "Pendiente");

    public static InvoicePdfIssuerData ForBrenda(string cuit, int pointOfSale) => new(
        cuit,
        pointOfSale,
        "MANSILLA BRENDA SOFIA",
        "BRENDA MANSILLA",
        "Maximiliano Matoso 1545",
        "Ing. A. Sourdeaux, Buenos Aires",
        "Responsable Monotributo",
        cuit,
        "01/11/2024");
}
