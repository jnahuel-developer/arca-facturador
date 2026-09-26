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
            Assert.Contains("FACTURA C", pdfText);
            Assert.Contains("Comprobante local: 00000007", pdfText);
            Assert.Contains("Periodo de servicio: 01/09/2026 al 30/09/2026", pdfText);
            Assert.Contains("Codigo: 0001", pdfText);
            Assert.Contains("Servicio: Honorarios por servicio", pdfText);
            Assert.Contains("Importe total: $", pdfText);
            Assert.Contains("CAE: Pendiente de autorizacion", pdfText);
            Assert.Contains("Vencimiento CAE: Pendiente", pdfText);
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
                pdfPath);
            var pdfText = Encoding.ASCII.GetString(File.ReadAllBytes(pdfPath));

            Assert.Contains("Comprobante autorizado por ARCA", pdfText);
            Assert.Contains("Numero fiscal: 00000126", pdfText);
            Assert.Contains("CAE: 74370123456789", pdfText);
            Assert.Contains("Vencimiento CAE: 05/10/2026", pdfText);
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
