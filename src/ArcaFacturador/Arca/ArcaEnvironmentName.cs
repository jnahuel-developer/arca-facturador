namespace ArcaFacturador.Arca;

public static class ArcaEnvironmentName
{
    public const string Homologacion = "Homologacion";
    public const string Produccion = "Produccion";

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Homologacion;
        }

        var normalized = value.Trim();

        return normalized.ToUpperInvariant() switch
        {
            "HOMOLOGACION" or "HOMOLOGACIÓN" or "HOMOLOGATION" or "TEST" or "TESTING" => Homologacion,
            "PRODUCCION" or "PRODUCCIÓN" or "PRODUCTION" or "PROD" => Produccion,
            _ => throw new InvalidOperationException("El ambiente ARCA debe ser Homologacion o Produccion."),
        };
    }
}
