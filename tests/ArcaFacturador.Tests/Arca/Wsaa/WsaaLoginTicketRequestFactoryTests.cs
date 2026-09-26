using System.Xml.Linq;
using ArcaFacturador.Arca.Wsaa;

namespace ArcaFacturador.Tests.Arca.Wsaa;

public class WsaaLoginTicketRequestFactoryTests
{
    [Fact]
    public void Create_BuildsLoginTicketRequestForWsfe()
    {
        var factory = new WsaaLoginTicketRequestFactory();
        var options = new WsaaOptions
        {
            Service = "wsfe",
            TicketLifetime = TimeSpan.FromHours(12),
            ClockSkew = TimeSpan.FromMinutes(5),
        };
        var now = new DateTimeOffset(2026, 9, 25, 12, 30, 0, TimeSpan.FromHours(-3));

        var xml = factory.Create(options, now);

        var document = XDocument.Parse(xml);
        Assert.Equal("loginTicketRequest", document.Root?.Name.LocalName);
        Assert.Equal("1.0", document.Root?.Attribute("version")?.Value);
        Assert.Equal("wsfe", document.Descendants("service").Single().Value);
        Assert.Equal(now.ToUnixTimeSeconds().ToString(), document.Descendants("uniqueId").Single().Value);
        Assert.Equal("2026-09-25T12:25:00-03:00", document.Descendants("generationTime").Single().Value);
        Assert.Equal("2026-09-26T00:30:00-03:00", document.Descendants("expirationTime").Single().Value);
    }
}
