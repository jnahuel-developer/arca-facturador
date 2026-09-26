namespace ArcaFacturador.Persistence.Models;

public sealed record InvoiceRecord(
    long Id,
    long? ReceiptNumber,
    DateOnly IssueDate,
    DateOnly ServiceFrom,
    DateOnly ServiceTo,
    DateOnly PaymentDueDate,
    long AmountCents,
    InvoiceStatus Status,
    string? Cae,
    DateOnly? CaeExpirationDate,
    string? PdfPath);
