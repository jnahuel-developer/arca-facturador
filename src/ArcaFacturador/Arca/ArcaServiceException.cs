namespace ArcaFacturador.Arca;

public sealed class ArcaServiceException : Exception
{
    public ArcaServiceException(ArcaServiceErrorKind kind, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    public ArcaServiceErrorKind Kind { get; }

    public bool IsRecoverable =>
        Kind is ArcaServiceErrorKind.Recoverable or ArcaServiceErrorKind.RemoteUnavailable;
}
