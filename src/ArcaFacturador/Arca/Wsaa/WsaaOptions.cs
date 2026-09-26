namespace ArcaFacturador.Arca.Wsaa;

public sealed record WsaaOptions
{
    public const string DefaultService = "wsfe";
    public static readonly Uri HomologationLoginUrl = new("https://wsaahomo.afip.gov.ar/ws/services/LoginCms");

    public string Service { get; init; } = DefaultService;

    public Uri LoginUrl { get; init; } = HomologationLoginUrl;

    public TimeSpan TicketLifetime { get; init; } = TimeSpan.FromHours(12);

    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan RenewalMargin { get; init; } = TimeSpan.FromMinutes(10);

    public string? RepresentedCuit { get; init; }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Service);
        ArgumentNullException.ThrowIfNull(LoginUrl);

        if (!LoginUrl.IsAbsoluteUri)
        {
            throw new ArgumentException("La URL de WSAA debe ser absoluta.", nameof(LoginUrl));
        }

        if (TicketLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(TicketLifetime), "La vigencia del ticket debe ser positiva.");
        }

        if (ClockSkew < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ClockSkew), "La tolerancia horaria no puede ser negativa.");
        }

        if (RenewalMargin < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(RenewalMargin), "El margen de renovación no puede ser negativo.");
        }
    }
}
