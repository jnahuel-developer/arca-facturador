using ArcaFacturador.Arca;
using ArcaFacturador.Arca.Wsaa;
using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Documents;
using ArcaFacturador.Domain;
using ArcaFacturador.Persistence.Models;
using ArcaFacturador.Persistence.Repositories;
using ArcaFacturador.Tests.Persistence;

namespace ArcaFacturador.Tests.Arca;

public class ArcaInvoiceEmissionServiceTests
{
    [Fact]
    public async Task TestConnectionAsync_ConsultsLastAuthorizedWithoutEmitting()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var wsfeClient = new FakeWsfeClient(
            new WsfeLastAuthorizedResult(1, 11, 42),
            vouchers: [CreateAuthorizedVoucher(42, new DateOnly(2026, 9, 25))]);
        var service = CreateService(temporaryDatabase, wsfeClient);

        var result = await service.TestConnectionAsync();

        Assert.Equal(42, result.LastAuthorizedReceiptNumber);
        Assert.Equal(new DateOnly(2026, 9, 25), result.LastAuthorizedIssueDate);
        Assert.Equal(1, wsfeClient.LastAuthorizedRequestCount);
        Assert.Equal(0, wsfeClient.CaeRequestCount);
    }

    [Fact]
    public async Task EmitAsync_WhenAuthorized_StoresAuthorizedInvoice()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var wsfeClient = new FakeWsfeClient(
            new WsfeLastAuthorizedResult(1, 11, 42),
            caeResponse: CreateAuthorizedResponse(receiptNumber: 43));
        var service = CreateService(temporaryDatabase, wsfeClient);

        var result = await service.EmitAsync(CreateInvoice());

        Assert.Equal(ArcaEmissionStatus.Authorized, result.Status);
        Assert.Equal(InvoiceStatus.Authorized, result.Invoice.Status);
        Assert.Equal(43, result.Invoice.ReceiptNumber);
        Assert.Equal("74370123456789", result.Invoice.Cae);
        Assert.Equal(1, wsfeClient.CaeRequestCount);
    }

    [Fact]
    public async Task EmitAsync_WhenIssueDateIsBeforeLastAuthorizedIssueDate_RejectsBeforeRequestingCae()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        var wsfeClient = new FakeWsfeClient(
            new WsfeLastAuthorizedResult(1, 11, 42),
            caeResponse: CreateAuthorizedResponse(receiptNumber: 43),
            vouchers: [CreateAuthorizedVoucher(42, new DateOnly(2026, 9, 25))]);
        var service = new ArcaInvoiceEmissionService(
            repository,
            new InvoicePdfGenerator(),
            new FakeRuntimeFactory(wsfeClient));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EmitAsync(CreateInvoice() with { IssueDate = new DateOnly(2026, 9, 24) }));

        Assert.Contains("último comprobante autorizado", exception.Message);
        Assert.Empty(repository.GetAll());
        Assert.Equal(0, wsfeClient.CaeRequestCount);
    }

    [Fact]
    public async Task EmitAsync_WhenRejected_ReturnsRejected()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var wsfeClient = new FakeWsfeClient(
            new WsfeLastAuthorizedResult(1, 11, 42),
            caeResponse: CreateRejectedResponse(receiptNumber: 43));
        var service = CreateService(temporaryDatabase, wsfeClient);

        var result = await service.EmitAsync(CreateInvoice());

        Assert.Equal(ArcaEmissionStatus.Rejected, result.Status);
        Assert.Equal(InvoiceStatus.Rejected, result.Invoice.Status);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public async Task EmitAsync_WhenRecoverableErrorCannotBeReconciled_ReturnsPendingReview()
    {
        using var temporaryDatabase = new TemporaryDatabase();
        var wsfeClient = new FakeWsfeClient(
            new WsfeLastAuthorizedResult(1, 11, 42),
            caeException: new ArcaServiceException(ArcaServiceErrorKind.RemoteUnavailable, "ORA-01034"),
            vouchers: [null, null]);
        var service = CreateService(temporaryDatabase, wsfeClient);

        var result = await service.EmitAsync(CreateInvoice());

        Assert.Equal(ArcaEmissionStatus.PendingReview, result.Status);
        Assert.Equal(InvoiceStatus.Pending, result.Invoice.Status);
        Assert.Equal(43, result.Invoice.ReceiptNumber);
    }

    private static ArcaInvoiceEmissionService CreateService(TemporaryDatabase temporaryDatabase, FakeWsfeClient wsfeClient)
    {
        var repository = new InvoiceRepository(temporaryDatabase.Database);
        return new ArcaInvoiceEmissionService(
            repository,
            new InvoicePdfGenerator(),
            new FakeRuntimeFactory(wsfeClient));
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

    private static WsfeCaeResponse CreateAuthorizedResponse(long receiptNumber) => new(
        HeaderResult: "A",
        DetailResult: "A",
        ReceiptNumber: receiptNumber,
        Cae: "74370123456789",
        CaeExpirationDate: new DateOnly(2026, 10, 5),
        Observations: [],
        Errors: []);

    private static WsfeCaeResponse CreateRejectedResponse(long receiptNumber) => new(
        HeaderResult: "R",
        DetailResult: "R",
        ReceiptNumber: receiptNumber,
        Cae: null,
        CaeExpirationDate: null,
        Observations: [],
        Errors: [new WsfeMessage(10016, "Rechazado")]);

    private static WsfeVoucher CreateAuthorizedVoucher(long receiptNumber, DateOnly receiptDate) => new(
        PointOfSale: 1,
        ReceiptType: 11,
        ReceiptNumber: receiptNumber,
        ReceiptDate: receiptDate,
        Result: "A",
        Cae: "74370123456789",
        CaeExpirationDate: new DateOnly(2026, 10, 5));

    private sealed class FakeRuntimeFactory(FakeWsfeClient wsfeClient) : IArcaRuntimeFactory
    {
        public ArcaRuntime Create()
        {
            var preview = new ArcaOperationPreview(
                ArcaEnvironmentName.Produccion,
                new FiscalConfiguration("20111111112", 1),
                "appsettings.Local.json");

            return new ArcaRuntime(preview, new FakeTicketProvider(), wsfeClient);
        }
    }

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

        public int LastAuthorizedRequestCount { get; private set; }

        public int CaeRequestCount { get; private set; }

        public Task<WsfeLastAuthorizedResult> GetLastAuthorizedAsync(
            WsfeAuth auth,
            int pointOfSale,
            int receiptType,
            CancellationToken cancellationToken = default)
        {
            LastAuthorizedRequestCount++;
            return Task.FromResult(lastAuthorized);
        }

        public Task<WsfeVoucher?> GetVoucherAsync(
            WsfeAuth auth,
            int pointOfSale,
            int receiptType,
            long receiptNumber,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_vouchers.Count == 0 ? null : _vouchers.Dequeue());
        }

        public Task<WsfeCaeResponse> RequestCaeAsync(
            WsfeAuth auth,
            WsfeInvoiceRequest request,
            CancellationToken cancellationToken = default)
        {
            CaeRequestCount++;
            if (caeException is not null)
            {
                throw caeException;
            }

            return Task.FromResult(caeResponse ?? throw new InvalidOperationException("No se configuró respuesta CAE."));
        }
    }
}
