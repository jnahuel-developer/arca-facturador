namespace ArcaFacturador.Arca;

public enum ArcaServiceErrorKind
{
    Unknown,
    Recoverable,
    RemoteUnavailable,
    ExistingValidTicket,
    Rejected,
}
