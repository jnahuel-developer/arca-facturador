using System.Security.Cryptography.Pkcs;
using ArcaFacturador.Arca.Wsaa;

namespace ArcaFacturador.Tests.Arca.Wsaa;

public class WsaaCmsSignerTests
{
    [Fact]
    public void Sign_ReturnsValidBase64Cms()
    {
        using var certificate = CertificateTestFactory.CreateValidCertificate();
        var signer = new WsaaCmsSigner();

        var signedCms = signer.Sign("<loginTicketRequest />", certificate);

        var cmsBytes = Convert.FromBase64String(signedCms);
        var cms = new SignedCms();
        cms.Decode(cmsBytes);
        Assert.NotEmpty(cms.SignerInfos);
    }

    [Fact]
    public void Sign_RejectsCertificateWithoutPrivateKey()
    {
        using var certificate = CertificateTestFactory.CreateCertificateWithoutPrivateKey();
        var signer = new WsaaCmsSigner();

        Assert.Throws<InvalidOperationException>(() => signer.Sign("<loginTicketRequest />", certificate));
    }
}
