using System.IO;
using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Documents;
using ArcaFacturador.Domain;
using ArcaFacturador.Persistence.Models;
using ArcaFacturador.Persistence.Repositories;

namespace ArcaFacturador.Arca;

public sealed class ArcaInvoiceEmissionService(
    InvoiceRepository invoiceRepository,
    InvoicePdfGenerator pdfGenerator,
    IArcaRuntimeFactory runtimeFactory)
{
    public ArcaOperationPreview GetPreview()
    {
        using var runtime = runtimeFactory.Create();
        return runtime.Preview;
    }

    public async Task<ArcaConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var runtime = runtimeFactory.Create();
        var ticket = await runtime.TicketProvider.GetTicketAsync(cancellationToken).ConfigureAwait(false);
        var auth = new WsfeAuth(ticket.Token, ticket.Sign, long.Parse(runtime.FiscalConfiguration.Cuit));
        var lastAuthorized = await runtime.WsfeClient
            .GetLastAuthorizedAsync(
                auth,
                runtime.FiscalConfiguration.PointOfSale,
                Wsfev1Constants.FacturaC,
                cancellationToken)
            .ConfigureAwait(false);
        var lastAuthorizedIssueDate = await GetLastAuthorizedIssueDateAsync(
                runtime.WsfeClient,
                auth,
                runtime.FiscalConfiguration.PointOfSale,
                lastAuthorized.ReceiptNumber,
                cancellationToken)
            .ConfigureAwait(false);

        return new ArcaConnectionTestResult(runtime.Preview, lastAuthorized.ReceiptNumber, lastAuthorizedIssueDate);
    }

    public async Task<ArcaEmissionResult> EmitAsync(InvoiceRecord draftInvoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draftInvoice);

        using var runtime = runtimeFactory.Create();
        var ticket = await runtime.TicketProvider.GetTicketAsync(cancellationToken).ConfigureAwait(false);
        var auth = new WsfeAuth(ticket.Token, ticket.Sign, long.Parse(runtime.FiscalConfiguration.Cuit));
        await ValidateIssueDateAgainstLastAuthorizedAsync(
                draftInvoice,
                runtime.WsfeClient,
                auth,
                runtime.FiscalConfiguration,
                cancellationToken)
            .ConfigureAwait(false);

        var storedInvoice = invoiceRepository.Add(draftInvoice);
        var authorizationService = new WsfeInvoiceAuthorizationService(
            runtime.TicketProvider,
            runtime.WsfeClient,
            invoiceRepository,
            pdfGenerator);

        try
        {
            var outcome = await authorizationService
                .AuthorizeAsync(storedInvoice, runtime.FiscalConfiguration, cancellationToken)
                .ConfigureAwait(false);

            return MapOutcome(outcome);
        }
        catch (ArcaServiceException exception) when (exception.IsRecoverable)
        {
            var currentInvoice = invoiceRepository.GetById(storedInvoice.Id) ?? storedInvoice;
            return new ArcaEmissionResult(
                ArcaEmissionStatus.PendingReview,
                currentInvoice,
                null,
                ArcaUserMessageBuilder.FromException(exception));
        }
        catch (IOException)
        {
            return TryBuildAuthorizedWithPdfError(storedInvoice);
        }
        catch (UnauthorizedAccessException)
        {
            return TryBuildAuthorizedWithPdfError(storedInvoice);
        }
    }

    private static ArcaEmissionResult MapOutcome(WsfeAuthorizationOutcome outcome)
    {
        if (outcome.Invoice.Status == InvoiceStatus.Authorized)
        {
            var status = outcome.Response.Observations.Any(observation =>
                observation.Message.Contains("reconciliación", StringComparison.OrdinalIgnoreCase))
                    ? ArcaEmissionStatus.Recovered
                    : ArcaEmissionStatus.Authorized;

            return new ArcaEmissionResult(status, outcome.Invoice, outcome.Response);
        }

        return outcome.Invoice.Status == InvoiceStatus.Rejected
            ? new ArcaEmissionResult(ArcaEmissionStatus.Rejected, outcome.Invoice, outcome.Response)
            : new ArcaEmissionResult(ArcaEmissionStatus.PendingReview, outcome.Invoice, outcome.Response);
    }

    private ArcaEmissionResult TryBuildAuthorizedWithPdfError(InvoiceRecord storedInvoice)
    {
        var currentInvoice = invoiceRepository.GetById(storedInvoice.Id) ?? storedInvoice;
        if (currentInvoice.Status == InvoiceStatus.Authorized)
        {
            return new ArcaEmissionResult(ArcaEmissionStatus.AuthorizedWithPdfError, currentInvoice, null);
        }

        throw new IOException("No se pudo generar el PDF local del comprobante.");
    }

    private static async Task ValidateIssueDateAgainstLastAuthorizedAsync(
        InvoiceRecord draftInvoice,
        IWsfev1Client wsfeClient,
        WsfeAuth auth,
        FiscalConfiguration fiscalConfiguration,
        CancellationToken cancellationToken)
    {
        var lastAuthorized = await wsfeClient
            .GetLastAuthorizedAsync(auth, fiscalConfiguration.PointOfSale, Wsfev1Constants.FacturaC, cancellationToken)
            .ConfigureAwait(false);
        var lastAuthorizedIssueDate = await GetLastAuthorizedIssueDateAsync(
                wsfeClient,
                auth,
                fiscalConfiguration.PointOfSale,
                lastAuthorized.ReceiptNumber,
                cancellationToken)
            .ConfigureAwait(false);

        if (lastAuthorizedIssueDate is null || draftInvoice.IssueDate >= lastAuthorizedIssueDate.Value)
        {
            return;
        }

        throw new InvalidOperationException(
            $"La fecha de factura no puede ser anterior al último comprobante autorizado en ARCA ({lastAuthorizedIssueDate:dd/MM/yyyy}). " +
            "Elegí esa fecha o una posterior.");
    }

    private static async Task<DateOnly?> GetLastAuthorizedIssueDateAsync(
        IWsfev1Client wsfeClient,
        WsfeAuth auth,
        int pointOfSale,
        long lastAuthorizedReceiptNumber,
        CancellationToken cancellationToken)
    {
        if (lastAuthorizedReceiptNumber <= 0)
        {
            return null;
        }

        var voucher = await wsfeClient
            .GetVoucherAsync(auth, pointOfSale, Wsfev1Constants.FacturaC, lastAuthorizedReceiptNumber, cancellationToken)
            .ConfigureAwait(false);
        return voucher?.ReceiptDate;
    }
}
