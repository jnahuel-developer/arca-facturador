namespace ArcaFacturador.Arca.Wsfev1;

public sealed record WsfeCaeResponse(
    string HeaderResult,
    string DetailResult,
    long ReceiptNumber,
    string? Cae,
    DateOnly? CaeExpirationDate,
    IReadOnlyList<WsfeMessage> Observations,
    IReadOnlyList<WsfeMessage> Errors)
{
    public bool IsAuthorized =>
        string.Equals(DetailResult, "A", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(Cae)
        && CaeExpirationDate.HasValue;

    public bool IsRejected =>
        string.Equals(DetailResult, "R", StringComparison.OrdinalIgnoreCase);
}
