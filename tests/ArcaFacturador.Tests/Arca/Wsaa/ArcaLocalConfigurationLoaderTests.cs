using System.Text.Json;
using ArcaFacturador.Arca;
using ArcaFacturador.Arca.Wsaa;
using ArcaFacturador.Arca.Wsfev1;

namespace ArcaFacturador.Tests.Arca.Wsaa;

public class ArcaLocalConfigurationLoaderTests
{
    [Fact]
    public void Load_ReadsLocalWsaaSettings()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "ArcaFacturador.Tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(directoryPath, "appsettings.Local.json");
        Directory.CreateDirectory(directoryPath);

        try
        {
            File.WriteAllText(filePath, """
                {
                  "Arca": {
                    "Environment": "Homologacion",
                    "RepresentedCuit": "20111111112",
                    "PointOfSale": 1,
                    "Service": "wsfe",
                    "LoginUrl": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
                    "WsfeUrl": "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
                    "TicketLifetimeHours": 12,
                    "Certificate": {
                      "PfxPath": "C:\\ARCA\\certificado.pfx",
                      "PfxPassword": "secret"
                    }
                  }
                }
                """);

            var configuration = ArcaLocalConfigurationLoader.Load(filePath);
            var options = configuration.Arca.ToWsaaOptions();
            var wsfeOptions = configuration.Arca.ToWsfev1Options();
            var fiscalConfiguration = configuration.Arca.ToFiscalConfiguration();

            Assert.Equal("Homologacion", configuration.Arca.Environment);
            Assert.Equal("20111111112", options.RepresentedCuit);
            Assert.Equal("wsfe", options.Service);
            Assert.Equal(WsaaOptions.HomologationLoginUrl, options.LoginUrl);
            Assert.Equal(1, fiscalConfiguration.PointOfSale);
            Assert.Equal(Wsfev1Options.HomologationServiceUrl, wsfeOptions.ServiceUrl);
            Assert.Equal(TimeSpan.FromHours(12), options.TicketLifetime);
            Assert.Equal(@"C:\ARCA\certificado.pfx", configuration.Arca.Certificate.PfxPath);
            Assert.Equal("secret", configuration.Arca.Certificate.PfxPassword);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void ToWsaaOptions_WhenProductionIsNotExplicitlyEnabled_Throws()
    {
        var configuration = new ArcaLocalConfiguration
        {
            Environment = ArcaEnvironmentName.Produccion,
            LoginUrl = WsaaOptions.ProductionLoginUrl.ToString(),
            WsfeUrl = Wsfev1Options.ProductionServiceUrl.ToString(),
        };

        var exception = Assert.Throws<InvalidOperationException>(configuration.ToWsaaOptions);

        Assert.Contains("producción", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToWsaaOptions_WhenProductionIsExplicitlyEnabled_UsesProductionUrl()
    {
        var configuration = new ArcaLocalConfiguration
        {
            Environment = ArcaEnvironmentName.Produccion,
            AllowProduction = true,
            ProductionConfirmation = ArcaLocalConfiguration.ProductionConfirmationText,
            LoginUrl = WsaaOptions.ProductionLoginUrl.ToString(),
            WsfeUrl = Wsfev1Options.ProductionServiceUrl.ToString(),
        };

        var options = configuration.ToWsaaOptions();
        var wsfeOptions = configuration.ToWsfev1Options();

        Assert.Equal(WsaaOptions.ProductionLoginUrl, options.LoginUrl);
        Assert.Equal(Wsfev1Options.ProductionServiceUrl, wsfeOptions.ServiceUrl);
    }

    [Fact]
    public void ToWsfev1Options_WhenUrlDoesNotMatchEnvironment_Throws()
    {
        var configuration = new ArcaLocalConfiguration
        {
            Environment = ArcaEnvironmentName.Homologacion,
            LoginUrl = WsaaOptions.HomologationLoginUrl.ToString(),
            WsfeUrl = Wsfev1Options.ProductionServiceUrl.ToString(),
        };

        var exception = Assert.Throws<InvalidOperationException>(configuration.ToWsfev1Options);

        Assert.Contains("ambiente seleccionado", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
