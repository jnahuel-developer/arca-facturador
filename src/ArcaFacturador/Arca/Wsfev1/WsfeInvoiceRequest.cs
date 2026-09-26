namespace ArcaFacturador.Arca.Wsfev1;

public sealed record WsfeInvoiceRequest(
    int PointOfSale,
    int ReceiptType,
    int Concept,
    int DocumentType,
    long DocumentNumber,
    long ReceiptNumber,
    DateOnly ReceiptDate,
    decimal TotalAmount,
    decimal NonTaxedAmount,
    decimal NetAmount,
    decimal ExemptAmount,
    decimal TaxAmount,
    decimal VatAmount,
    DateOnly ServiceFrom,
    DateOnly ServiceTo,
    DateOnly PaymentDueDate,
    string CurrencyId,
    decimal CurrencyRate,
    int ReceiverVatConditionId)
{
    public void Validate()
    {
        if (PointOfSale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(PointOfSale));
        }

        if (ReceiptType <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ReceiptType));
        }

        if (ReceiptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ReceiptNumber));
        }

        if (TotalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(TotalAmount));
        }

        if (ServiceFrom > ServiceTo)
        {
            throw new ArgumentException("La fecha de inicio del servicio no puede ser posterior a la fecha de finalización.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(CurrencyId);
    }
}
