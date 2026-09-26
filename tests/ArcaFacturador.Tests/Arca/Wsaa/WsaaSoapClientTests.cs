using System.Net;
using System.Text;
using ArcaFacturador.Arca.Wsaa;

namespace ArcaFacturador.Tests.Arca.Wsaa;

public class WsaaSoapClientTests
{
    [Fact]
    public async Task LoginCmsAsync_PostsCmsAndParsesTicket()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(WsaaLoginTicketParserTests.BuildSoapResponse(), Encoding.UTF8, "text/xml"),
        });
        using var httpClient = new HttpClient(handler);
        var client = new WsaaSoapClient(httpClient);

        var ticket = await client.LoginCmsAsync(new Uri("https://wsaahomo.afip.gov.ar/ws/services/LoginCms"), "signed-cms");

        Assert.Equal("token-value", ticket.Token);
        Assert.Equal(HttpMethod.Post, handler.Request?.Method);
        Assert.Equal("https://wsaahomo.afip.gov.ar/ws/services/LoginCms", handler.Request?.RequestUri?.ToString());
        Assert.Contains("loginCms", handler.RequestBody);
        Assert.Contains("signed-cms", handler.RequestBody);
    }

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return response;
        }
    }
}
