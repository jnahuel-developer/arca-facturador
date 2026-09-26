namespace ArcaFacturador.Arca.Wsaa;

public sealed record WsaaLoginTicket(
    string Token,
    string Sign,
    string Service,
    string Source,
    string Destination,
    DateTimeOffset GenerationTime,
    DateTimeOffset ExpirationTime)
{
    public bool IsValid(DateTimeOffset now, TimeSpan renewalMargin)
    {
        return !string.IsNullOrWhiteSpace(Token)
            && !string.IsNullOrWhiteSpace(Sign)
            && now < ExpirationTime.Subtract(renewalMargin);
    }
}
