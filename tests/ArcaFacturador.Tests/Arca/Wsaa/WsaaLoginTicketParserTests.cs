using ArcaFacturador.Arca.Wsaa;

namespace ArcaFacturador.Tests.Arca.Wsaa;

public class WsaaLoginTicketParserTests
{
    [Fact]
    public void Parse_ReadsLoginCmsSoapResponse()
    {
        var parser = new WsaaLoginTicketParser();

        var ticket = parser.Parse(BuildSoapResponse());

        Assert.Equal("token-value", ticket.Token);
        Assert.Equal("sign-value", ticket.Sign);
        Assert.Equal("wsfe", ticket.Service);
        Assert.Equal("CN=client", ticket.Source);
        Assert.Equal("CN=wsaa", ticket.Destination);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(-3)), ticket.GenerationTime);
        Assert.Equal(new DateTimeOffset(2026, 9, 26, 0, 0, 0, TimeSpan.FromHours(-3)), ticket.ExpirationTime);
    }

    [Fact]
    public void Parse_AllowsResponseWithoutService()
    {
        var parser = new WsaaLoginTicketParser();
        var response = BuildSoapResponse().Replace("&lt;service&gt;wsfe&lt;/service&gt;", string.Empty, StringComparison.Ordinal);

        var ticket = parser.Parse(response);

        Assert.Equal(string.Empty, ticket.Service);
        Assert.Equal("token-value", ticket.Token);
    }

    public static string BuildSoapResponse()
    {
        return """
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
              <soapenv:Body>
                <loginCmsResponse>
                  <loginCmsReturn>&lt;loginTicketResponse version="1.0"&gt;&lt;header&gt;&lt;source&gt;CN=client&lt;/source&gt;&lt;destination&gt;CN=wsaa&lt;/destination&gt;&lt;generationTime&gt;2026-09-25T12:00:00-03:00&lt;/generationTime&gt;&lt;expirationTime&gt;2026-09-26T00:00:00-03:00&lt;/expirationTime&gt;&lt;/header&gt;&lt;credentials&gt;&lt;token&gt;token-value&lt;/token&gt;&lt;sign&gt;sign-value&lt;/sign&gt;&lt;/credentials&gt;&lt;service&gt;wsfe&lt;/service&gt;&lt;/loginTicketResponse&gt;</loginCmsReturn>
                </loginCmsResponse>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }
}
