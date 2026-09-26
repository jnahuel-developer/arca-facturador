using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArcaFacturador.Arca;
using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Domain;

namespace ArcaFacturador.Arca.Wsaa;

public sealed record LocalConfigurationFile
{
    public ArcaLocalConfiguration Arca { get; init; } = new();
}

public sealed record ArcaLocalConfiguration
{
    public const string ProductionConfirmationText = "CONFIRMO_USO_PRODUCCION";

    public string Environment { get; init; } = "Homologacion";

    public bool AllowProduction { get; init; }

    public string? ProductionConfirmation { get; init; }

    public string? RepresentedCuit { get; init; }

    public int PointOfSale { get; init; } = 1;

    public string Service { get; init; } = WsaaOptions.DefaultService;

    public string LoginUrl { get; init; } = WsaaOptions.HomologationLoginUrl.ToString();

    public string WsfeUrl { get; init; } = Wsfev1Options.HomologationServiceUrl.ToString();

    public int TicketLifetimeHours { get; init; } = 12;

    public WsaaCertificateOptions Certificate { get; init; } = new();

    public WsaaOptions ToWsaaOptions()
    {
        Validate();
        var profile = ArcaEnvironmentProfile.FromName(Environment);
        var loginUrl = ResolveConfiguredUrl(LoginUrl, profile.WsaaLoginUrl, "WSAA");

        return new WsaaOptions
        {
            Service = Service,
            LoginUrl = loginUrl,
            TicketLifetime = TimeSpan.FromHours(TicketLifetimeHours),
            RepresentedCuit = RepresentedCuit,
        };
    }

    public Wsfev1Options ToWsfev1Options()
    {
        Validate();
        var profile = ArcaEnvironmentProfile.FromName(Environment);
        var serviceUrl = ResolveConfiguredUrl(WsfeUrl, profile.Wsfev1ServiceUrl, "WSFEv1");

        return new Wsfev1Options
        {
            ServiceUrl = serviceUrl,
        };
    }

    public FiscalConfiguration ToFiscalConfiguration()
    {
        Validate();

        if (string.IsNullOrWhiteSpace(RepresentedCuit))
        {
            throw new InvalidOperationException("Configurá el CUIT representado para operar con ARCA.");
        }

        return new FiscalConfiguration(RepresentedCuit, PointOfSale);
    }

    public void Validate()
    {
        var profile = ArcaEnvironmentProfile.FromName(Environment);

        ResolveConfiguredUrl(LoginUrl, profile.WsaaLoginUrl, "WSAA");
        ResolveConfiguredUrl(WsfeUrl, profile.Wsfev1ServiceUrl, "WSFEv1");

        if (TicketLifetimeHours <= 0)
        {
            throw new InvalidOperationException("La vigencia del ticket WSAA debe ser positiva.");
        }

        if (profile.Name == ArcaEnvironmentName.Produccion)
        {
            if (!AllowProduction || !string.Equals(ProductionConfirmation, ProductionConfirmationText, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Para usar producción configurá AllowProduction=true y ProductionConfirmation=\"{ProductionConfirmationText}\".");
            }
        }
    }

    private static Uri ResolveConfiguredUrl(string? configuredUrl, Uri expectedUrl, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            return expectedUrl;
        }

        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var url))
        {
            throw new InvalidOperationException($"La URL configurada para {serviceName} no es válida.");
        }

        if (!UriEquals(url, expectedUrl))
        {
            throw new InvalidOperationException(
                $"La URL configurada para {serviceName} no corresponde al ambiente seleccionado. Valor esperado: {expectedUrl}");
        }

        return url;
    }

    private static bool UriEquals(Uri left, Uri right)
    {
        var leftText = left.GetLeftPart(UriPartial.Path).TrimEnd('/');
        var rightText = right.GetLeftPart(UriPartial.Path).TrimEnd('/');

        return string.Equals(leftText, rightText, StringComparison.OrdinalIgnoreCase);
    }
}

public static class ArcaLocalConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() },
    };

    public static LocalConfigurationFile Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<LocalConfigurationFile>(stream, JsonOptions)
            ?? throw new InvalidOperationException("El archivo de configuración local está vacío.");
    }
}
