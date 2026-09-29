using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ArcaFacturador.Tests.Arca.Wsaa;

internal static class CertificateTestFactory
{
    public static X509Certificate2 CreateValidCertificate()
    {
        return CreateCertificate(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
    }

    public static X509Certificate2 CreateExpiredCertificate()
    {
        return CreateCertificate(DateTimeOffset.Now.AddDays(-3), DateTimeOffset.Now.AddDays(-1));
    }

    public static byte[] CreateExpiredPkcs12()
    {
        return CreatePkcs12(DateTimeOffset.Now.AddDays(-3), DateTimeOffset.Now.AddDays(-1));
    }

    public static X509Certificate2 CreateCertificateWithoutPrivateKey()
    {
        using var certificate = CreateValidCertificate();
        var bytes = certificate.Export(X509ContentType.Cert);
        return X509CertificateLoader.LoadCertificate(bytes);
    }

    private static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var bytes = CreatePkcs12(notBefore, notAfter);
        return X509CertificateLoader.LoadPkcs12(bytes, password: null);
    }

    private static byte[] CreatePkcs12(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=ARCA Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(notBefore, notAfter);
        return certificate.Export(X509ContentType.Pkcs12);
    }
}
