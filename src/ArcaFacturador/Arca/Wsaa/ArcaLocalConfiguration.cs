using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Domain;

namespace ArcaFacturador.Arca.Wsaa;

public sealed record LocalConfigurationFile
{
    public ArcaLocalConfiguration Arca { get; init; } = new();
}

public sealed record ArcaLocalConfiguration
{
    public string Environment { get; init; } = "Homologacion";

    public string? RepresentedCuit { get; init; }

    public int PointOfSale { get; init; } = 1;

    public string Service { get; init; } = WsaaOptions.DefaultService;

    public string LoginUrl { get; init; } = WsaaOptions.HomologationLoginUrl.ToString();

    public string WsfeUrl { get; init; } = Wsfev1Options.HomologationServiceUrl.ToString();

    public int TicketLifetimeHours { get; init; } = 12;

    public WsaaCertificateOptions Certificate { get; init; } = new();

    public WsaaOptions ToWsaaOptions()
    {
        if (!Uri.TryCreate(LoginUrl, UriKind.Absolute, out var loginUrl))
        {
            throw new InvalidOperationException("La URL configurada para WSAA no es válida.");
        }

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
        if (!Uri.TryCreate(WsfeUrl, UriKind.Absolute, out var serviceUrl))
        {
            throw new InvalidOperationException("La URL configurada para WSFEv1 no es válida.");
        }

        return new Wsfev1Options
        {
            ServiceUrl = serviceUrl,
        };
    }

    public FiscalConfiguration ToFiscalConfiguration()
    {
        if (string.IsNullOrWhiteSpace(RepresentedCuit))
        {
            throw new InvalidOperationException("Configurá el CUIT representado para operar con ARCA.");
        }

        return new FiscalConfiguration(RepresentedCuit, PointOfSale);
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
