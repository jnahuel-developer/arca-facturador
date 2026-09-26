using System.Net;
using System.Text;
using ArcaFacturador.Arca.Wsaa;

namespace ArcaFacturador.Tests.Arca.Wsaa;

public class WsaaAuthenticatorTests
{
    [Fact]
    public async Task GetTicketAsync_ReusesValidTicket()
    {
        using var certificate = CertificateTestFactory.CreateValidCertificate();
        var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var options = new WsaaOptions
        {
            LoginUrl = new Uri("https://wsaahomo.afip.gov.ar/ws/services/LoginCms"),
            RenewalMargin = TimeSpan.FromMinutes(10),
        };
        var authenticator = new WsaaAuthenticator(
            options,
            certificate,
            httpClient,
            () => new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(-3)));

        var firstTicket = await authenticator.GetTicketAsync();
        var secondTicket = await authenticator.GetTicketAsync();

        Assert.Same(firstTicket, secondTicket);
        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(WsaaLoginTicketParserTests.BuildSoapResponse(), Encoding.UTF8, "text/xml"),
            };

            return Task.FromResult(response);
        }
    }
}
