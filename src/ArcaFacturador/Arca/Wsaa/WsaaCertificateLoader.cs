using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace ArcaFacturador.Arca.Wsaa;

public sealed class WsaaCertificateLoader
{
    public X509Certificate2 Load(WsaaCertificateOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var certificate = !string.IsNullOrWhiteSpace(options.PfxPath)
            ? LoadPfx(options)
            : LoadPem(options);

        if (!certificate.HasPrivateKey)
        {
            certificate.Dispose();
            throw new InvalidOperationException("El certificado configurado no contiene clave privada.");
        }

        return certificate;
    }

    private static X509Certificate2 LoadPfx(WsaaCertificateOptions options)
    {
        var pfxPath = options.PfxPath;
        if (string.IsNullOrWhiteSpace(pfxPath))
        {
            throw new InvalidOperationException("Configurá la ruta del archivo PFX.");
        }

        if (!File.Exists(pfxPath))
        {
            throw new FileNotFoundException("No se encontró el archivo PFX configurado.", pfxPath);
        }

        return X509CertificateLoader.LoadPkcs12FromFile(
            pfxPath,
            options.PfxPassword,
            X509KeyStorageFlags.UserKeySet);
    }

    private static X509Certificate2 LoadPem(WsaaCertificateOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CertificatePemPath) || string.IsNullOrWhiteSpace(options.PrivateKeyPemPath))
        {
            throw new InvalidOperationException("Configurá un PFX o el par certificado PEM + clave privada PEM.");
        }

        if (!File.Exists(options.CertificatePemPath))
        {
            throw new FileNotFoundException("No se encontró el certificado PEM configurado.", options.CertificatePemPath);
        }

        if (!File.Exists(options.PrivateKeyPemPath))
        {
            throw new FileNotFoundException("No se encontró la clave privada PEM configurada.", options.PrivateKeyPemPath);
        }

        return string.IsNullOrEmpty(options.PrivateKeyPassword)
            ? X509Certificate2.CreateFromPemFile(options.CertificatePemPath, options.PrivateKeyPemPath)
            : X509Certificate2.CreateFromEncryptedPemFile(options.CertificatePemPath, options.PrivateKeyPassword, options.PrivateKeyPemPath);
    }
}
