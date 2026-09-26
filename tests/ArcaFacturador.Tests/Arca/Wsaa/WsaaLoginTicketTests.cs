using ArcaFacturador.Arca.Wsaa;

namespace ArcaFacturador.Tests.Arca.Wsaa;

public class WsaaLoginTicketTests
{
    [Fact]
    public void IsValid_ReturnsFalseInsideRenewalMargin()
    {
        var ticket = new WsaaLoginTicket(
            Token: "token",
            Sign: "sign",
            Service: "wsfe",
            Source: "CN=test",
            Destination: "CN=wsaa",
            GenerationTime: new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero),
            ExpirationTime: new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero));

        Assert.True(ticket.IsValid(new DateTimeOffset(2026, 9, 25, 11, 30, 0, TimeSpan.Zero), TimeSpan.FromMinutes(10)));
        Assert.False(ticket.IsValid(new DateTimeOffset(2026, 9, 25, 11, 55, 0, TimeSpan.Zero), TimeSpan.FromMinutes(10)));
    }
}
