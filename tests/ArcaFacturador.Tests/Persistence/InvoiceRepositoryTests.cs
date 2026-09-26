using ArcaFacturador.Persistence.Models;
using ArcaFacturador.Persistence.Repositories;
using Microsoft.Data.Sqlite;

namespace ArcaFacturador.Tests.Persistence;

public class InvoiceRepositoryTests
{
    [Fact]
    public void Add_PersistsAndReturnsAnInvoice()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var invoice = CreateInvoice(receiptNumber: 42);

        var storedInvoice = repository.Add(invoice);

        Assert.True(storedInvoice.Id > 0);
        Assert.Equal(storedInvoice, repository.GetById(storedInvoice.Id));
    }

    [Fact]
    public void GetAll_ReturnsInvoicesInInsertionOrder()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var first = repository.Add(CreateInvoice(receiptNumber: 1));
        var second = repository.Add(CreateInvoice(receiptNumber: 2));

        var invoices = repository.GetAll();

        Assert.Equal([first, second], invoices);
    }

    [Fact]
    public void Add_RejectsDuplicateReceiptNumbers()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        repository.Add(CreateInvoice(receiptNumber: 42));

        Assert.Throws<SqliteException>(() => repository.Add(CreateInvoice(receiptNumber: 42)));
    }

    [Fact]
    public void Add_RejectsAnInvalidServicePeriod()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var invoice = CreateInvoice(receiptNumber: null) with
        {
            ServiceFrom = new DateOnly(2026, 10, 1),
            ServiceTo = new DateOnly(2026, 9, 30),
        };

        Assert.Throws<ArgumentException>(() => repository.Add(invoice));
    }

    [Fact]
    public void UpdatePdfPath_PersistsPdfPath()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var storedInvoice = repository.Add(CreateInvoice(receiptNumber: null) with { PdfPath = null });
        var pdfPath = @"C:\Facturas\factura-local-00000001.pdf";

        var updatedInvoice = repository.UpdatePdfPath(storedInvoice.Id, pdfPath);

        Assert.Equal(pdfPath, updatedInvoice.PdfPath);
        Assert.Equal(updatedInvoice, repository.GetById(storedInvoice.Id));
    }

    [Fact]
    public void UpdateAuthorizationResult_PersistsAuthorizedResult()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var storedInvoice = repository.Add(CreateInvoice(receiptNumber: null) with { Status = InvoiceStatus.Pending, Cae = null, CaeExpirationDate = null });

        var updatedInvoice = repository.UpdateAuthorizationResult(
            storedInvoice.Id,
            receiptNumber: 43,
            InvoiceStatus.Authorized,
            cae: "74370123456789",
            caeExpirationDate: new DateOnly(2026, 10, 5),
            pdfPath: @"C:\Facturas\factura-local-00000001.pdf");

        Assert.Equal(43, updatedInvoice.ReceiptNumber);
        Assert.Equal(InvoiceStatus.Authorized, updatedInvoice.Status);
        Assert.Equal("74370123456789", updatedInvoice.Cae);
        Assert.Equal(new DateOnly(2026, 10, 5), updatedInvoice.CaeExpirationDate);
        Assert.Equal(updatedInvoice, repository.GetById(storedInvoice.Id));
    }

    [Fact]
    public void UpdateAuthorizationResult_RejectsAuthorizedResultWithoutCae()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var storedInvoice = repository.Add(CreateInvoice(receiptNumber: null) with { Status = InvoiceStatus.Pending, Cae = null, CaeExpirationDate = null });

        Assert.Throws<ArgumentNullException>(
            () => repository.UpdateAuthorizationResult(
                storedInvoice.Id,
                receiptNumber: 43,
                InvoiceStatus.Authorized,
                cae: null,
                caeExpirationDate: new DateOnly(2026, 10, 5),
                pdfPath: null));
    }

    private static InvoiceRecord CreateInvoice(long? receiptNumber) => new(
        Id: 0,
        ReceiptNumber: receiptNumber,
        IssueDate: new DateOnly(2026, 9, 25),
        ServiceFrom: new DateOnly(2026, 9, 1),
        ServiceTo: new DateOnly(2026, 9, 30),
        PaymentDueDate: new DateOnly(2026, 9, 25),
        AmountCents: 125_000,
        Status: InvoiceStatus.Authorized,
        Cae: "12345678901234",
        CaeExpirationDate: new DateOnly(2026, 10, 5),
        PdfPath: @"C:\Facturas\00000042.pdf");
}
