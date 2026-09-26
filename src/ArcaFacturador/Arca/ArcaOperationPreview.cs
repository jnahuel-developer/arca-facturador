using ArcaFacturador.Domain;

namespace ArcaFacturador.Arca;

public sealed record ArcaOperationPreview(
    string Environment,
    FiscalConfiguration FiscalConfiguration,
    string ConfigurationPath)
{
    public bool IsProduction => string.Equals(Environment, ArcaEnvironmentName.Produccion, StringComparison.Ordinal);
}
