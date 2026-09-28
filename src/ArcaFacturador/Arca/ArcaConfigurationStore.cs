using System.IO;
using ArcaFacturador.Arca.Wsaa;
using ArcaFacturador.Persistence;

namespace ArcaFacturador.Arca;

public sealed class ArcaConfigurationStore(string? configurationPath = null)
{
    public string ConfigurationPath { get; } = configurationPath ?? Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");

    public ArcaConfigurationForm LoadForm()
    {
        if (!File.Exists(ConfigurationPath))
        {
            return ArcaConfigurationForm.FromConfiguration(new ArcaLocalConfiguration());
        }

        var file = ArcaLocalConfigurationLoader.Load(ConfigurationPath);
        return ArcaConfigurationForm.FromConfiguration(file.Arca);
    }

    public void Save(ArcaConfigurationForm form)
    {
        ArgumentNullException.ThrowIfNull(form);

        var configuration = form.ToConfiguration();
        configuration.Validate();
        ArcaLocalConfigurationLoader.Save(ConfigurationPath, new LocalConfigurationFile { Arca = configuration });
        CopyToLocalDataConfiguration(configuration);
    }

    private static void CopyToLocalDataConfiguration(ArcaLocalConfiguration configuration)
    {
        ArcaLocalConfigurationLoader.Save(
            LocalDataPaths.LocalConfigurationFilePath,
            new LocalConfigurationFile { Arca = configuration });
    }
}
