using System.Globalization;
using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Presentation;

public sealed record InvoiceHistoryItem(
    long Id,
    string IssueDate,
    string Status,
    string ReceiptNumber,
    string Amount,
    string Cae,
    string CaeExpirationDate,
    string PdfPath)
{
    private static readonly CultureInfo ArgentineCulture = CultureInfo.GetCultureInfo("es-AR");

    public static InvoiceHistoryItem FromInvoice(InvoiceRecord invoice) => new(
        invoice.Id,
        invoice.IssueDate.ToString("dd/MM/yyyy", ArgentineCulture),
        TranslateStatus(invoice.Status),
        invoice.ReceiptNumber?.ToString("00000000", CultureInfo.InvariantCulture) ?? "Pendiente",
        (invoice.AmountCents / 100m).ToString("C2", ArgentineCulture),
        string.IsNullOrWhiteSpace(invoice.Cae) ? "Pendiente" : invoice.Cae,
        invoice.CaeExpirationDate?.ToString("dd/MM/yyyy", ArgentineCulture) ?? "Pendiente",
        invoice.PdfPath ?? string.Empty);

    private static string TranslateStatus(InvoiceStatus status) =>
        status switch
        {
            InvoiceStatus.Authorized => "Autorizada",
            InvoiceStatus.Rejected => "Rechazada",
            _ => "Pendiente",
        };
}
