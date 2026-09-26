namespace ArcaFacturador.Arca.Wsfev1;

public sealed record WsfeAuth(string Token, string Sign, long Cuit)
{
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Token);
        ArgumentException.ThrowIfNullOrWhiteSpace(Sign);

        if (Cuit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Cuit), "El CUIT representado debe ser mayor que cero.");
        }
    }
}
