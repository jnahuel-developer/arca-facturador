using System.Xml.Linq;

namespace ArcaFacturador.Arca.Wsaa;

public sealed class WsaaLoginTicketParser
{
    public WsaaLoginTicket Parse(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        var document = XDocument.Parse(xml);
        var ticketXml = document.Root?.Name.LocalName == "loginTicketResponse"
            ? document.Root.ToString(SaveOptions.DisableFormatting)
            : FindRequiredValue(document, "loginCmsReturn");

        var ticketDocument = XDocument.Parse(ticketXml);
        return new WsaaLoginTicket(
            Token: FindRequiredValue(ticketDocument, "token"),
            Sign: FindRequiredValue(ticketDocument, "sign"),
            Service: FindOptionalValue(ticketDocument, "service") ?? string.Empty,
            Source: FindRequiredValue(ticketDocument, "source"),
            Destination: FindRequiredValue(ticketDocument, "destination"),
            GenerationTime: DateTimeOffset.Parse(FindRequiredValue(ticketDocument, "generationTime")),
            ExpirationTime: DateTimeOffset.Parse(FindRequiredValue(ticketDocument, "expirationTime")));
    }

    private static string FindRequiredValue(XDocument document, string localName)
    {
        var value = document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == localName)
            ?.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"La respuesta de WSAA no contiene el campo {localName}.");
        }

        return value;
    }

    private static string? FindOptionalValue(XDocument document, string localName)
    {
        return document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == localName)
            ?.Value;
    }
}
