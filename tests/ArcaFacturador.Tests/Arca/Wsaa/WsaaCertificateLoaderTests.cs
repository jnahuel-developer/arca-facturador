using System.Security.Cryptography.X509Certificates;
using ArcaFacturador.Arca.Wsaa;

namespace ArcaFacturador.Tests.Arca.Wsaa;

public class WsaaCertificateLoaderTests
{
    [Fact]
    public void Load_WhenPfxIsExpired_ThrowsClearError()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "ArcaFacturador.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        var pfxPath = Path.Combine(directoryPath, "certificado-vencido.pfx");

        try
        {
            File.WriteAllBytes(pfxPath, CertificateTestFactory.CreateExpiredPkcs12());

            var exception = Assert.Throws<InvalidOperationException>(
                () => new WsaaCertificateLoader().Load(new WsaaCertificateOptions { PfxPath = pfxPath }));

            Assert.Contains("vencido", exception.Message, StringComparison.OrdinalIgnoreCase);
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
