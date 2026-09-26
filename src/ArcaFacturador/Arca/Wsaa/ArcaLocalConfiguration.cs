using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcaFacturador.Arca.Wsaa;

public sealed record LocalConfigurationFile
{
    public ArcaLocalConfiguration Arca { get; init; } = new();
}

public sealed record ArcaLocalConfiguration
{
    public string Environment { get; init; } = "Homologacion";

    public string? RepresentedCuit { get; init; }

    public string Service { get; init; } = WsaaOptions.DefaultService;

    public string LoginUrl { get; init; } = WsaaOptions.HomologationLoginUrl.ToString();

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
