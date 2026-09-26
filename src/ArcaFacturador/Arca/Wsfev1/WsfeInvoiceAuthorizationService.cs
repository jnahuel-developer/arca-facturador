using ArcaFacturador.Arca.Wsaa;
using ArcaFacturador.Documents;
using ArcaFacturador.Domain;
using ArcaFacturador.Persistence;
using ArcaFacturador.Persistence.Models;
using ArcaFacturador.Persistence.Repositories;

namespace ArcaFacturador.Arca.Wsfev1;

public sealed class WsfeInvoiceAuthorizationService(
    IWsaaTicketProvider ticketProvider,
    IWsfev1Client wsfeClient,
    InvoiceRepository invoiceRepository,
    InvoicePdfGenerator pdfGenerator)
{
    private readonly WsfeInvoiceRequestFactory _requestFactory = new();

    public async Task<WsfeAuthorizationOutcome> AuthorizeAsync(
        InvoiceRecord invoice,
        FiscalConfiguration fiscalConfiguration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(fiscalConfiguration);

        if (invoice.Id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(invoice), "La factura debe estar guardada antes de autorizar.");
        }

        if (invoice.Status != InvoiceStatus.Pending)
        {
            throw new InvalidOperationException("Sólo se pueden autorizar facturas pendientes.");
        }

        var ticket = await ticketProvider.GetTicketAsync(cancellationToken).ConfigureAwait(false);
        var auth = new WsfeAuth(ticket.Token, ticket.Sign, long.Parse(fiscalConfiguration.Cuit));
        var lastAuthorized = await wsfeClient
            .GetLastAuthorizedAsync(auth, fiscalConfiguration.PointOfSale, Wsfev1Constants.FacturaC, cancellationToken)
            .ConfigureAwait(false);

        var request = _requestFactory.CreateFacturaCServices(
            invoice,
            fiscalConfiguration.PointOfSale,
            lastAuthorized.ReceiptNumber + 1);
        var response = await wsfeClient.RequestCaeAsync(auth, request, cancellationToken).ConfigureAwait(false);

        var updatedInvoice = response.IsAuthorized
            ? AuthorizeInvoice(invoice, response)
            : RejectInvoice(invoice);

        return new WsfeAuthorizationOutcome(updatedInvoice, response);
    }

    private InvoiceRecord AuthorizeInvoice(InvoiceRecord invoice, WsfeCaeResponse response)
    {
        var pdfPath = invoice.PdfPath ?? LocalDataPaths.GetInvoicePdfFilePath(invoice.Id);
        var updatedInvoice = invoiceRepository.UpdateAuthorizationResult(
            invoice.Id,
            response.ReceiptNumber,
            InvoiceStatus.Authorized,
            response.Cae,
            response.CaeExpirationDate,
            pdfPath);
        pdfGenerator.Generate(updatedInvoice, pdfPath);
        return updatedInvoice;
    }

    private InvoiceRecord RejectInvoice(InvoiceRecord invoice)
    {
        return invoiceRepository.UpdateAuthorizationResult(
            invoice.Id,
            receiptNumber: null,
            InvoiceStatus.Rejected,
            cae: null,
            caeExpirationDate: null,
            pdfPath: invoice.PdfPath);
    }
}
