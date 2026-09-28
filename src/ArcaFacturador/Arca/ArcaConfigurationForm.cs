using ArcaFacturador.Arca.Wsaa;
using ArcaFacturador.Arca.Wsfev1;

namespace ArcaFacturador.Arca;

public sealed record ArcaConfigurationForm(
    string Environment,
    string RepresentedCuit,
    int PointOfSale,
    string PfxPath,
    string PfxPassword,
    bool AllowProduction)
{
    public static ArcaConfigurationForm FromConfiguration(ArcaLocalConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new ArcaConfigurationForm(
            ArcaEnvironmentName.Normalize(configuration.Environment),
            configuration.RepresentedCuit ?? string.Empty,
            configuration.PointOfSale,
            configuration.Certificate.PfxPath ?? string.Empty,
            configuration.Certificate.PfxPassword ?? string.Empty,
            configuration.AllowProduction);
    }

    public ArcaLocalConfiguration ToConfiguration()
    {
        var profile = ArcaEnvironmentProfile.FromName(Environment);
        var normalizedEnvironment = profile.Name;

        return new ArcaLocalConfiguration
        {
            Environment = normalizedEnvironment,
            AllowProduction = normalizedEnvironment == ArcaEnvironmentName.Produccion && AllowProduction,
            ProductionConfirmation = normalizedEnvironment == ArcaEnvironmentName.Produccion && AllowProduction
                ? ArcaLocalConfiguration.ProductionConfirmationText
                : null,
            RepresentedCuit = RepresentedCuit,
            PointOfSale = PointOfSale,
            Service = WsaaOptions.DefaultService,
            LoginUrl = profile.WsaaLoginUrl.ToString(),
            WsfeUrl = profile.Wsfev1ServiceUrl.ToString(),
            TicketLifetimeHours = 12,
            Certificate = new WsaaCertificateOptions
            {
                PfxPath = PfxPath,
                PfxPassword = PfxPassword,
            },
        };
    }

    public string BuildSummary()
    {
        var profile = ArcaEnvironmentProfile.FromName(Environment);
        return $"Ambiente: {profile.Name} | CUIT: {RepresentedCuit} | PV: {PointOfSale}";
    }
}
