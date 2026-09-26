using System.IO;
using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Documents;
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

        return new ArcaConnectionTestResult(runtime.Preview, lastAuthorized.ReceiptNumber);
    }

    public async Task<ArcaEmissionResult> EmitAsync(InvoiceRecord draftInvoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draftInvoice);

        using var runtime = runtimeFactory.Create();
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
}
