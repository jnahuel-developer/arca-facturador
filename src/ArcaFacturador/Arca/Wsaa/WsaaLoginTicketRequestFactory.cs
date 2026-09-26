using System.Globalization;
using System.Xml.Linq;

namespace ArcaFacturador.Arca.Wsaa;

public sealed class WsaaLoginTicketRequestFactory
{
    public string Create(WsaaOptions options, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var generationTime = now.Subtract(options.ClockSkew);
        var expirationTime = now.Add(options.TicketLifetime);
        var uniqueId = now.ToUnixTimeSeconds();

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(
                "loginTicketRequest",
                new XAttribute("version", "1.0"),
                new XElement(
                    "header",
                    new XElement("uniqueId", uniqueId.ToString(CultureInfo.InvariantCulture)),
                    new XElement("generationTime", FormatDateTime(generationTime)),
                    new XElement("expirationTime", FormatDateTime(expirationTime))),
                new XElement("service", options.Service)));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static string FormatDateTime(DateTimeOffset value) =>
        value.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
}
