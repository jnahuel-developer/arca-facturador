using System.Text.Json;
using ArcaFacturador.Arca;
using ArcaFacturador.Arca.Wsaa;

namespace ArcaFacturador.Tests.Arca;

public class ArcaConfigurationStoreTests
{
    [Fact]
    public void Save_WhenProductionForm_WritesProductionUrlsAndConfirmation()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "ArcaFacturador.Tests", Guid.NewGuid().ToString("N"));
        var configurationPath = Path.Combine(directoryPath, "appsettings.Local.json");
        var store = new ArcaConfigurationStore(configurationPath);
        var form = new ArcaConfigurationForm(
            ArcaEnvironmentName.Produccion,
            "27354180753",
            1,
            @"C:\ARCA\produccion\certificado-produccion.pfx",
            "secret",
            AllowProduction: true);

        try
        {
            store.Save(form);
            var configuration = ArcaLocalConfigurationLoader.Load(configurationPath).Arca;

            Assert.Equal(ArcaEnvironmentName.Produccion, configuration.Environment);
            Assert.True(configuration.AllowProduction);
            Assert.Equal(ArcaLocalConfiguration.ProductionConfirmationText, configuration.ProductionConfirmation);
            Assert.Equal("https://wsaa.afip.gov.ar/ws/services/LoginCms", configuration.LoginUrl);
            Assert.Equal("https://servicios1.afip.gov.ar/wsfev1/service.asmx", configuration.WsfeUrl);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }
}
