namespace ArcaFacturador.Arca.Wsaa;

public sealed record WsaaCertificateOptions
{
    public string? PfxPath { get; init; }

    public string? PfxPassword { get; init; }

    public string? CertificatePemPath { get; init; }

    public string? PrivateKeyPemPath { get; init; }

    public string? PrivateKeyPassword { get; init; }
}
