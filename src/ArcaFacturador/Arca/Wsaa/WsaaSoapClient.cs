using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace ArcaFacturador.Arca.Wsaa;

public sealed class WsaaSoapClient(HttpClient httpClient)
{
    private readonly WsaaLoginTicketParser _parser = new();

    public async Task<WsaaLoginTicket> LoginCmsAsync(Uri loginUrl, string signedCms, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loginUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(signedCms);

        using var request = new HttpRequestMessage(HttpMethod.Post, loginUrl)
        {
            Content = new StringContent(BuildEnvelope(signedCms), Encoding.UTF8, "text/xml"),
        };
        request.Headers.TryAddWithoutValidation("SOAPAction", string.Empty);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"WSAA devolvió HTTP {(int)response.StatusCode}: {ExtractFault(responseText)}");
        }

        return _parser.Parse(responseText);
    }

    private static string BuildEnvelope(string signedCms)
    {
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:wsaa="http://wsaa.view.sua.dvadac.desein.afip.gov">
              <soapenv:Header />
              <soapenv:Body>
                <wsaa:loginCms>
                  <wsaa:in0>{WebUtility.HtmlEncode(signedCms)}</wsaa:in0>
                </wsaa:loginCms>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    private static string ExtractFault(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return "respuesta vacía";
        }

        try
        {
            var document = XDocument.Parse(responseText);
            return document
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "faultstring")
                ?.Value
                ?? responseText;
        }
        catch (System.Xml.XmlException)
        {
            return responseText;
        }
    }
}
