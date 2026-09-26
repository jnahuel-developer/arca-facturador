namespace ArcaFacturador.Arca.Wsfev1;

public sealed record WsfeVoucher(
    int PointOfSale,
    int ReceiptType,
    long ReceiptNumber,
    string Result,
    string? Cae,
    DateOnly? CaeExpirationDate)
{
    public bool IsAuthorized =>
        string.Equals(Result, "A", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(Cae)
        && CaeExpirationDate.HasValue;
}
