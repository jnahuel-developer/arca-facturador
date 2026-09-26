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
        var receiptNumber = await ResolveReceiptNumberAsync(invoice, auth, fiscalConfiguration, cancellationToken).ConfigureAwait(false);

        var reconciledInvoice = await TryRecoverAuthorizedInvoiceAsync(invoice, auth, fiscalConfiguration, receiptNumber, cancellationToken)
            .ConfigureAwait(false);
        if (reconciledInvoice is not null)
        {
            return new WsfeAuthorizationOutcome(reconciledInvoice, CreateRecoveredResponse(reconciledInvoice));
        }

        var request = _requestFactory.CreateFacturaCServices(
            invoice,
            fiscalConfiguration.PointOfSale,
            receiptNumber);
        var preparedInvoice = invoice.ReceiptNumber == receiptNumber
            ? invoice
            : invoiceRepository.UpdateAuthorizationResult(
                invoice.Id,
                receiptNumber,
                InvoiceStatus.Pending,
                cae: null,
                caeExpirationDate: null,
                pdfPath: invoice.PdfPath);

        WsfeCaeResponse response;
        try
        {
            response = await wsfeClient.RequestCaeAsync(auth, request, cancellationToken).ConfigureAwait(false);
        }
        catch (ArcaServiceException exception) when (exception.IsRecoverable)
        {
            var recoveredInvoice = await TryRecoverAuthorizedInvoiceAsync(
                    preparedInvoice,
                    auth,
                    fiscalConfiguration,
                    receiptNumber,
                    cancellationToken)
                .ConfigureAwait(false);

            if (recoveredInvoice is not null)
            {
                return new WsfeAuthorizationOutcome(recoveredInvoice, CreateRecoveredResponse(recoveredInvoice));
            }

            throw;
        }

        var updatedInvoice = response.IsAuthorized
            ? AuthorizeInvoice(preparedInvoice, response)
            : RejectInvoice(preparedInvoice);

        return new WsfeAuthorizationOutcome(updatedInvoice, response);
    }

    private async Task<long> ResolveReceiptNumberAsync(
        InvoiceRecord invoice,
        WsfeAuth auth,
        FiscalConfiguration fiscalConfiguration,
        CancellationToken cancellationToken)
    {
        if (invoice.ReceiptNumber is { } existingReceiptNumber)
        {
            return existingReceiptNumber;
        }

        var lastAuthorized = await wsfeClient
            .GetLastAuthorizedAsync(auth, fiscalConfiguration.PointOfSale, Wsfev1Constants.FacturaC, cancellationToken)
            .ConfigureAwait(false);
        return lastAuthorized.ReceiptNumber + 1;
    }

    private async Task<InvoiceRecord?> TryRecoverAuthorizedInvoiceAsync(
        InvoiceRecord invoice,
        WsfeAuth auth,
        FiscalConfiguration fiscalConfiguration,
        long receiptNumber,
        CancellationToken cancellationToken)
    {
        var voucher = await wsfeClient
            .GetVoucherAsync(
                auth,
                fiscalConfiguration.PointOfSale,
                Wsfev1Constants.FacturaC,
                receiptNumber,
                cancellationToken)
            .ConfigureAwait(false);

        if (voucher?.IsAuthorized != true)
        {
            return null;
        }

        var response = new WsfeCaeResponse(
            HeaderResult: "A",
            DetailResult: voucher.Result,
            ReceiptNumber: voucher.ReceiptNumber,
            Cae: voucher.Cae,
            CaeExpirationDate: voucher.CaeExpirationDate,
            Observations: [],
            Errors: []);
        return AuthorizeInvoice(invoice, response);
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

    private static WsfeCaeResponse CreateRecoveredResponse(InvoiceRecord invoice)
    {
        return new WsfeCaeResponse(
            HeaderResult: "A",
            DetailResult: "A",
            ReceiptNumber: invoice.ReceiptNumber ?? 0,
            Cae: invoice.Cae,
            CaeExpirationDate: invoice.CaeExpirationDate,
            Observations: [new WsfeMessage(0, "Comprobante recuperado por reconciliación.")],
            Errors: []);
    }
}
