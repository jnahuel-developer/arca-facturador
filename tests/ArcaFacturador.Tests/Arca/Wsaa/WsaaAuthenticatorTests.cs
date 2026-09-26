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
            new NullTicketCache(),
            clock: () => new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(-3)));

        var firstTicket = await authenticator.GetTicketAsync();
        var secondTicket = await authenticator.GetTicketAsync();

        Assert.Same(firstTicket, secondTicket);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetTicketAsync_ReusesPersistedTicket()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), "ArcaFacturador.Tests", Guid.NewGuid().ToString("N"), "wsaa-ticket-cache.json");
        using var certificate = CertificateTestFactory.CreateValidCertificate();
        var handler = new CountingHandler();
        using var httpClient = new HttpClient(handler);
        var options = new WsaaOptions
        {
            LoginUrl = new Uri("https://wsaahomo.afip.gov.ar/ws/services/LoginCms"),
            RenewalMargin = TimeSpan.FromMinutes(10),
        };
        var cache = new FileWsaaTicketCache(cachePath);
        var clock = () => new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(-3));

        try
        {
            var firstAuthenticator = new WsaaAuthenticator(options, certificate, httpClient, cache, clock);
            var firstTicket = await firstAuthenticator.GetTicketAsync();
            var secondAuthenticator = new WsaaAuthenticator(options, certificate, httpClient, cache, clock);
            var secondTicket = await secondAuthenticator.GetTicketAsync();

            Assert.Equal(firstTicket, secondTicket);
            Assert.Equal(1, handler.RequestCount);
        }
        finally
        {
            var directoryPath = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
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

    private sealed class NullTicketCache : IWsaaTicketCache
    {
        public WsaaLoginTicket? Load(WsaaOptions options, DateTimeOffset now) => null;

        public void Save(WsaaOptions options, WsaaLoginTicket ticket)
        {
        }

        public void Clear(WsaaOptions options)
        {
        }
    }
}
