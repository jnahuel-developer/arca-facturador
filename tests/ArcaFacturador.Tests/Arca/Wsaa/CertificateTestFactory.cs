using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ArcaFacturador.Tests.Arca.Wsaa;

internal static class CertificateTestFactory
{
    public static X509Certificate2 CreateValidCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=ARCA Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var certificate = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var bytes = certificate.Export(X509ContentType.Pkcs12);
        return X509CertificateLoader.LoadPkcs12(bytes, password: null);
    }

    public static X509Certificate2 CreateCertificateWithoutPrivateKey()
    {
        using var certificate = CreateValidCertificate();
        var bytes = certificate.Export(X509ContentType.Cert);
        return X509CertificateLoader.LoadCertificate(bytes);
    }
}
