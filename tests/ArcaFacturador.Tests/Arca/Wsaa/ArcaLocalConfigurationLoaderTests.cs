using System.Text.Json;
using ArcaFacturador.Arca.Wsaa;

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
                    "Service": "wsfe",
                    "LoginUrl": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
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

            Assert.Equal("Homologacion", configuration.Arca.Environment);
            Assert.Equal("20111111112", options.RepresentedCuit);
            Assert.Equal("wsfe", options.Service);
            Assert.Equal(WsaaOptions.HomologationLoginUrl, options.LoginUrl);
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
}
