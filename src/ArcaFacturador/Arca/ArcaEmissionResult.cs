using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Arca;

public sealed record ArcaEmissionResult(
    ArcaEmissionStatus Status,
    InvoiceRecord Invoice,
    WsfeCaeResponse? Response)
{
    public bool IsAuthorized =>
        Status is ArcaEmissionStatus.Authorized or ArcaEmissionStatus.AuthorizedWithPdfError or ArcaEmissionStatus.Recovered;
}
