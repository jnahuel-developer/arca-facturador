using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ArcaFacturador.Arca.Wsaa;

public sealed class WsaaCmsSigner
{
    public string Sign(string loginTicketRequestXml, X509Certificate2 certificate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loginTicketRequestXml);
        ArgumentNullException.ThrowIfNull(certificate);
        ValidateCertificate(certificate);

        var contentInfo = new ContentInfo(Encoding.UTF8.GetBytes(loginTicketRequestXml));
        var signedCms = new SignedCms(contentInfo, detached: true);
        var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate)
        {
            IncludeOption = X509IncludeOption.EndCertOnly,
        };

        signedCms.ComputeSignature(signer);
        return Convert.ToBase64String(signedCms.Encode());
    }

    private static void ValidateCertificate(X509Certificate2 certificate)
    {
        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException("El certificado de ARCA no contiene clave privada.");
        }

        var now = DateTimeOffset.Now;
        if (now < certificate.NotBefore || now > certificate.NotAfter)
        {
            throw new InvalidOperationException("El certificado de ARCA no está vigente.");
        }
    }
}
