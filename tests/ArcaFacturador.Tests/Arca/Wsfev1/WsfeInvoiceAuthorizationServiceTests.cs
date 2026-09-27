using ArcaFacturador.Arca.Wsaa;
using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Arca;
using ArcaFacturador.Documents;
using ArcaFacturador.Domain;
using ArcaFacturador.Persistence.Models;
using ArcaFacturador.Persistence.Repositories;
using ArcaFacturador.Tests.Persistence;

namespace ArcaFacturador.Tests.Arca.Wsfev1;

public class WsfeInvoiceAuthorizationServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_StoresCaeAndRegeneratesPdf()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var invoice = repository.Add(CreateInvoice());
        var pdfPath = Path.Combine(Path.GetTempPath(), "ArcaFacturador.Tests", Guid.NewGuid().ToString("N"), "factura.pdf");
        invoice = repository.UpdatePdfPath(invoice.Id, pdfPath);
        var service = new WsfeInvoiceAuthorizationService(
            new FakeTicketProvider(),
            new FakeWsfeClient(new WsfeLastAuthorizedResult(1, 11, 125), caeResponse: CreateAuthorizedResponse()),
            repository,
            new InvoicePdfGenerator());

        try
        {
            var outcome = await service.AuthorizeAsync(invoice, new FiscalConfiguration("20111111112", 1));
            var pdfText = File.ReadAllText(pdfPath);

            Assert.Equal(InvoiceStatus.Authorized, outcome.Invoice.Status);
            Assert.Equal(126, outcome.Invoice.ReceiptNumber);
            Assert.Equal("74370123456789", outcome.Invoice.Cae);
            Assert.Equal(new DateOnly(2026, 10, 5), outcome.Invoice.CaeExpirationDate);
            Assert.Contains("Comprobante Autorizado", pdfText);
            Assert.Contains("Punto de Venta:", pdfText);
            Assert.Contains("00001", pdfText);
            Assert.Contains("CAE Nro:", pdfText);
            Assert.Contains("74370123456789", pdfText);
        }
        finally
        {
            var directoryPath = Path.GetDirectoryName(pdfPath);
            if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AuthorizeAsync_StoresRejectedStatus()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var invoice = repository.Add(CreateInvoice());
        var service = new WsfeInvoiceAuthorizationService(
            new FakeTicketProvider(),
            new FakeWsfeClient(new WsfeLastAuthorizedResult(1, 11, 125), caeResponse: CreateRejectedResponse()),
            repository,
            new InvoicePdfGenerator());

        var outcome = await service.AuthorizeAsync(invoice, new FiscalConfiguration("20111111112", 1));

        Assert.Equal(InvoiceStatus.Rejected, outcome.Invoice.Status);
        Assert.Null(outcome.Invoice.ReceiptNumber);
        Assert.Null(outcome.Invoice.Cae);
    }

    [Fact]
    public async Task AuthorizeAsync_ReconcilesExistingReceiptBeforeRequestingCae()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var invoice = repository.Add(CreateInvoice());
        invoice = repository.UpdateAuthorizationResult(
            invoice.Id,
            receiptNumber: 126,
            InvoiceStatus.Pending,
            cae: null,
            caeExpirationDate: null,
            pdfPath: null);
        var wsfeClient = new FakeWsfeClient(
            new WsfeLastAuthorizedResult(1, 11, 125),
            vouchers: [CreateAuthorizedVoucher()]);
        var service = new WsfeInvoiceAuthorizationService(
            new FakeTicketProvider(),
            wsfeClient,
            repository,
            new InvoicePdfGenerator());

        var outcome = await service.AuthorizeAsync(invoice, new FiscalConfiguration("20111111112", 1));

        Assert.Equal(InvoiceStatus.Authorized, outcome.Invoice.Status);
        Assert.Equal("74370123456789", outcome.Invoice.Cae);
        Assert.Equal(0, wsfeClient.CaeRequestCount);
        Assert.Equal(1, wsfeClient.VoucherQueryCount);
    }

    [Fact]
    public async Task AuthorizeAsync_ReconcilesAfterRecoverableError()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var invoice = repository.Add(CreateInvoice());
        var wsfeClient = new FakeWsfeClient(
            new WsfeLastAuthorizedResult(1, 11, 125),
            caeException: new ArcaServiceException(ArcaServiceErrorKind.RemoteUnavailable, "ORA-01034"),
            vouchers: [null, CreateAuthorizedVoucher()]);
        var service = new WsfeInvoiceAuthorizationService(
            new FakeTicketProvider(),
            wsfeClient,
            repository,
            new InvoicePdfGenerator());

        var outcome = await service.AuthorizeAsync(invoice, new FiscalConfiguration("20111111112", 1));

        Assert.Equal(InvoiceStatus.Authorized, outcome.Invoice.Status);
        Assert.Equal(126, outcome.Invoice.ReceiptNumber);
        Assert.Equal("74370123456789", outcome.Invoice.Cae);
        Assert.Equal(1, wsfeClient.CaeRequestCount);
        Assert.Equal(2, wsfeClient.VoucherQueryCount);
    }

    private static InvoiceRecord CreateInvoice() => new(
        Id: 0,
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

    private static WsfeCaeResponse CreateAuthorizedResponse() => new(
        HeaderResult: "A",
        DetailResult: "A",
        ReceiptNumber: 126,
        Cae: "74370123456789",
        CaeExpirationDate: new DateOnly(2026, 10, 5),
        Observations: [],
        Errors: []);

    private static WsfeCaeResponse CreateRejectedResponse() => new(
        HeaderResult: "R",
        DetailResult: "R",
        ReceiptNumber: 126,
        Cae: null,
        CaeExpirationDate: null,
        Observations: [],
        Errors: [new WsfeMessage(10016, "Rechazado")]);

    private static WsfeVoucher CreateAuthorizedVoucher() => new(
        PointOfSale: 1,
        ReceiptType: 11,
        ReceiptNumber: 126,
        Result: "A",
        Cae: "74370123456789",
        CaeExpirationDate: new DateOnly(2026, 10, 5));

    private sealed class FakeTicketProvider : IWsaaTicketProvider
    {
        public Task<WsaaLoginTicket> GetTicketAsync(CancellationToken cancellationToken = default)
        {
            var ticket = new WsaaLoginTicket(
                Token: "token",
                Sign: "sign",
                Service: "wsfe",
                Source: "CN=test",
                Destination: "CN=wsaa",
                GenerationTime: DateTimeOffset.Now.AddMinutes(-1),
                ExpirationTime: DateTimeOffset.Now.AddHours(12));
            return Task.FromResult(ticket);
        }
    }

    private sealed class FakeWsfeClient(
        WsfeLastAuthorizedResult lastAuthorized,
        WsfeCaeResponse? caeResponse = null,
        Exception? caeException = null,
        IReadOnlyList<WsfeVoucher?>? vouchers = null) : IWsfev1Client
    {
        private readonly Queue<WsfeVoucher?> _vouchers = new(vouchers ?? []);

        public int CaeRequestCount { get; private set; }

        public int VoucherQueryCount { get; private set; }

        public Task<WsfeLastAuthorizedResult> GetLastAuthorizedAsync(
            WsfeAuth auth,
            int pointOfSale,
            int receiptType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(lastAuthorized);
        }

        public Task<WsfeVoucher?> GetVoucherAsync(
            WsfeAuth auth,
            int pointOfSale,
            int receiptType,
            long receiptNumber,
            CancellationToken cancellationToken = default)
        {
            VoucherQueryCount++;
            return Task.FromResult(_vouchers.Count == 0 ? null : _vouchers.Dequeue());
        }

        public Task<WsfeCaeResponse> RequestCaeAsync(
            WsfeAuth auth,
            WsfeInvoiceRequest request,
            CancellationToken cancellationToken = default)
        {
            CaeRequestCount++;
            Assert.Equal(126, request.ReceiptNumber);
            if (caeException is not null)
            {
                throw caeException;
            }

            return Task.FromResult(caeResponse ?? throw new InvalidOperationException("No se configuró respuesta CAE."));
        }
    }
}
