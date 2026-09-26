using System.Globalization;
using ArcaFacturador.Persistence.Models;

namespace ArcaFacturador.Presentation;

public sealed record FrequentPriceItem(long Id, long AmountCents, string AmountText, string DisplayText)
{
    private static readonly CultureInfo ArgentineCulture = CultureInfo.GetCultureInfo("es-AR");

    public static FrequentPriceItem FromProduct(ProductRecord product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var amount = product.UnitPriceCents / 100m;
        return new FrequentPriceItem(
            product.Id,
            product.UnitPriceCents,
            amount.ToString("0.##", ArgentineCulture),
            amount.ToString("C2", ArgentineCulture));
    }
}
