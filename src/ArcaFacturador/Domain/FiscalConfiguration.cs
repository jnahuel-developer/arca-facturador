namespace ArcaFacturador.Domain;

public sealed record FiscalConfiguration
{
    public const int MinimumPointOfSale = 1;
    public const int MaximumPointOfSale = 99_999;

    public FiscalConfiguration(string cuit, int pointOfSale)
    {
        Cuit = NormalizeAndValidateCuit(cuit);

        if (pointOfSale is < MinimumPointOfSale or > MaximumPointOfSale)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pointOfSale),
                pointOfSale,
                $"El punto de venta debe estar entre {MinimumPointOfSale} y {MaximumPointOfSale}.");
        }

        PointOfSale = pointOfSale;
    }

    public string Cuit { get; }

    public int PointOfSale { get; }

    private static string NormalizeAndValidateCuit(string cuit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cuit);

        var normalizedCuit = cuit
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (normalizedCuit.Length != 11 || !normalizedCuit.All(char.IsAsciiDigit))
        {
            throw new ArgumentException(
                "El CUIT debe contener exactamente 11 dígitos.",
                nameof(cuit));
        }

        if (!HasValidCheckDigit(normalizedCuit))
        {
            throw new ArgumentException(
                "El dígito verificador del CUIT no es válido.",
                nameof(cuit));
        }

        return normalizedCuit;
    }

    private static bool HasValidCheckDigit(string cuit)
    {
        int[] weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        var sum = 0;

        for (var index = 0; index < weights.Length; index++)
        {
            sum += (cuit[index] - '0') * weights[index];
        }

        var result = 11 - (sum % 11);
        var expectedCheckDigit = result switch
        {
            11 => 0,
            10 => 9,
            _ => result,
        };

        return cuit[^1] - '0' == expectedCheckDigit;
    }
}
