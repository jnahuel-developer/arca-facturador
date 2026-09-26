using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Arca.Wsfev1;

public sealed class WsfeInvoiceRequestFactory
{
    public WsfeInvoiceRequest CreateFacturaCServices(InvoiceRecord invoice, int pointOfSale, long nextReceiptNumber)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.Id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(invoice), "La factura debe estar guardada antes de pedir CAE.");
        }

        if (invoice.Status != InvoiceStatus.Pending)
        {
            throw new InvalidOperationException("Sólo se puede pedir CAE para facturas pendientes.");
        }

        if (nextReceiptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nextReceiptNumber));
        }

        var amount = invoice.AmountCents / 100m;
        var request = new WsfeInvoiceRequest(
            PointOfSale: pointOfSale,
            ReceiptType: Wsfev1Constants.FacturaC,
            Concept: Wsfev1Constants.ConceptoServicios,
            DocumentType: Wsfev1Constants.DocumentoConsumidorFinal,
            DocumentNumber: Wsfev1Constants.DocumentoNumeroConsumidorFinal,
            ReceiptNumber: nextReceiptNumber,
            ReceiptDate: invoice.IssueDate,
            TotalAmount: amount,
            NonTaxedAmount: 0m,
            NetAmount: amount,
            ExemptAmount: 0m,
            TaxAmount: 0m,
            VatAmount: 0m,
            ServiceFrom: invoice.ServiceFrom,
            ServiceTo: invoice.ServiceTo,
            PaymentDueDate: invoice.PaymentDueDate,
            CurrencyId: Wsfev1Constants.MonedaPesos,
            CurrencyRate: 1m,
            ReceiverVatConditionId: Wsfev1Constants.CondicionIvaConsumidorFinal);
        request.Validate();
        return request;
    }
}
