using System.IO;
using System.Text;
using ArcaFacturador.Documents;
using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Tests.Documents;

public class InvoicePdfGeneratorTests
{
    [Fact]
    public void Generate_CreatesAReadablePdfWithEssentialFields()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "ArcaFacturador.Tests", Guid.NewGuid().ToString("N"));
        var pdfPath = Path.Combine(directoryPath, "factura-local-00000007.pdf");
        var generator = new InvoicePdfGenerator();

        try
        {
            var resultPath = generator.Generate(CreateInvoice(), pdfPath);
            var pdfText = Encoding.ASCII.GetString(File.ReadAllBytes(resultPath));

            Assert.Equal(Path.GetFullPath(pdfPath), resultPath);
            Assert.StartsWith("%PDF-1.4", pdfText);
            Assert.Contains("BORRADOR", pdfText);
            Assert.Contains("FACTURA", pdfText);
            Assert.Contains("Periodo Facturado Desde:", pdfText);
            Assert.Contains("0001", pdfText);
            Assert.Contains("Honorarios por servicio", pdfText);
            Assert.Contains("CAE Nro:", pdfText);
            Assert.Contains("Pendiente", pdfText);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void Generate_RejectsUnsavedInvoice()
    {
        var generator = new InvoicePdfGenerator();
        var invoice = CreateInvoice() with { Id = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(invoice, "factura.pdf"));
    }

    [Fact]
    public void Generate_IncludesCaeForAuthorizedInvoice()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "ArcaFacturador.Tests", Guid.NewGuid().ToString("N"));
        var pdfPath = Path.Combine(directoryPath, "factura-local-00000007.pdf");
        var generator = new InvoicePdfGenerator();

        try
        {
            generator.Generate(
                CreateInvoice() with
                {
                    ReceiptNumber = 126,
                    Status = InvoiceStatus.Authorized,
                    Cae = "74370123456789",
                    CaeExpirationDate = new DateOnly(2026, 10, 5),
                },
                pdfPath,
                InvoicePdfIssuerData.ForBrenda("20111111112", 3));
            var pdfText = Encoding.ASCII.GetString(File.ReadAllBytes(pdfPath));

            Assert.Contains("ORIGINAL", pdfText);
            Assert.Contains("DUPLICADO", pdfText);
            Assert.Contains("TRIPLICADO", pdfText);
            Assert.Contains("Punto de Venta:", pdfText);
            Assert.Contains("00003", pdfText);
            Assert.Contains("Comp. Nro:", pdfText);
            Assert.Contains("00000126", pdfText);
            Assert.Contains("CUIT:", pdfText);
            Assert.Contains("20111111112", pdfText);
            Assert.Contains("CAE Nro:", pdfText);
            Assert.Contains("74370123456789", pdfText);
            Assert.Contains("05/10/2026", pdfText);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    private static InvoiceRecord CreateInvoice() => new(
        Id: 7,
        ReceiptNumber: null,
        IssueDate: new DateOnly(2026, 9, 25),
        ServiceFrom: new DateOnly(2026, 9, 1),
        ServiceTo: new DateOnly(2026, 9, 30),
        PaymentDueDate: new DateOnly(2026, 9, 25),
        AmountCents: 125_000,
        Status: InvoiceStatus.Pending,
        Cae: null,
        CaeExpirationDate: null,
        PdfPath: null);
}
