using ArcaFacturador.Arca.Wsaa;
using ArcaFacturador.Arca.Wsfev1;

namespace ArcaFacturador.Arca;

public sealed record ArcaEnvironmentProfile(
    string Name,
    Uri WsaaLoginUrl,
    Uri Wsfev1ServiceUrl)
{
    public static ArcaEnvironmentProfile FromName(string? environment)
    {
        var normalizedEnvironment = ArcaEnvironmentName.Normalize(environment);

        return normalizedEnvironment switch
        {
            ArcaEnvironmentName.Homologacion => new(
                ArcaEnvironmentName.Homologacion,
                WsaaOptions.HomologationLoginUrl,
                Wsfev1Options.HomologationServiceUrl),
            ArcaEnvironmentName.Produccion => new(
                ArcaEnvironmentName.Produccion,
                WsaaOptions.ProductionLoginUrl,
                Wsfev1Options.ProductionServiceUrl),
            _ => throw new InvalidOperationException("El ambiente ARCA debe ser Homologacion o Produccion."),
        };
    }
}
