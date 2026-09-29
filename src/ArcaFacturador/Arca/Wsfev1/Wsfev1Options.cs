namespace ArcaFacturador.Arca.Wsfev1;

public sealed record Wsfev1Options
{
    public static readonly Uri HomologationServiceUrl = new("https://wswhomo.afip.gov.ar/wsfev1/service.asmx");
    public static readonly Uri ProductionServiceUrl = new("https://servicios1.afip.gov.ar/wsfev1/service.asmx");

    public Uri ServiceUrl { get; init; } = HomologationServiceUrl;

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(ServiceUrl);

        if (!ServiceUrl.IsAbsoluteUri)
        {
            throw new ArgumentException("La URL de WSFEv1 debe ser absoluta.", nameof(ServiceUrl));
        }
    }
}
