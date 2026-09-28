using System.IO;
using System.Net.Http;
using ArcaFacturador.Arca.Wsaa;
using ArcaFacturador.Arca.Wsfev1;
using ArcaFacturador.Persistence;

namespace ArcaFacturador.Arca;

public sealed class LocalArcaRuntimeFactory(string? configurationPath = null) : IArcaRuntimeFactory
{
    public ArcaRuntime Create()
    {
        var resolvedConfigurationPath = ResolveConfigurationPath();
        var configurationFile = ArcaLocalConfigurationLoader.Load(resolvedConfigurationPath);
        var arcaConfiguration = configurationFile.Arca;
        arcaConfiguration.Validate();

        var certificate = new WsaaCertificateLoader().Load(arcaConfiguration.Certificate);
        var httpClient = new HttpClient();
        var wsaaOptions = arcaConfiguration.ToWsaaOptions();
        var wsfeOptions = arcaConfiguration.ToWsfev1Options();
        var fiscalConfiguration = arcaConfiguration.ToFiscalConfiguration();
        var preview = new ArcaOperationPreview(
            ArcaEnvironmentName.Normalize(arcaConfiguration.Environment),
            fiscalConfiguration,
            resolvedConfigurationPath);

        var ticketProvider = new WsaaAuthenticator(wsaaOptions, certificate, httpClient);
        var wsfeClient = new Wsfev1SoapClient(httpClient, wsfeOptions);
        var disposable = new CompositeDisposable(certificate, httpClient);

        return new ArcaRuntime(preview, ticketProvider, wsfeClient, disposable);
    }

    private string ResolveConfigurationPath()
    {
        if (!string.IsNullOrWhiteSpace(configurationPath))
        {
            return EnsureConfigurationExists(configurationPath);
        }

        foreach (var candidatePath in EnumerateCandidatePaths())
        {
            if (File.Exists(candidatePath))
            {
                return Path.GetFullPath(candidatePath);
            }
        }

        throw new FileNotFoundException(
            "No se encontró appsettings.Local.json. Copiá appsettings.example.json o appsettings.production.example.json y completá los datos reales.",
            LocalDataPaths.LocalConfigurationFilePath);
    }

    private static string EnsureConfigurationExists(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("No se encontró el archivo de configuración local de ARCA.", path);
        }

        return Path.GetFullPath(path);
    }

    private static IEnumerable<string> EnumerateCandidatePaths()
    {
        yield return Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");
        yield return Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json");
        yield return LocalDataPaths.LocalConfigurationFilePath;
    }
}
