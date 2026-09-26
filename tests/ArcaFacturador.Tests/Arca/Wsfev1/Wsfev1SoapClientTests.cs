using System.Net;
using System.Text;
using ArcaFacturador.Arca.Wsfev1;

namespace ArcaFacturador.Tests.Arca.Wsfev1;

public class Wsfev1SoapClientTests
{
    [Fact]
    public async Task GetLastAuthorizedAsync_PostsRequestAndParsesNumber()
    {
        var handler = new QueueHandler(BuildLastAuthorizedResponse());
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var result = await client.GetLastAuthorizedAsync(CreateAuth(), pointOfSale: 1, receiptType: 11);

        Assert.Equal(125, result.ReceiptNumber);
        Assert.Contains("FECompUltimoAutorizado", handler.Requests.Single().Body);
        Assert.Contains("<ar:PtoVta>1</ar:PtoVta>", handler.Requests.Single().Body);
        Assert.Contains("<ar:CbteTipo>11</ar:CbteTipo>", handler.Requests.Single().Body);
    }

    [Fact]
    public async Task RequestCaeAsync_PostsFacturaCAndParsesAuthorizedResponse()
    {
        var handler = new QueueHandler(BuildAuthorizedCaeResponse());
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var response = await client.RequestCaeAsync(CreateAuth(), CreateRequest());

        Assert.True(response.IsAuthorized);
        Assert.Equal(126, response.ReceiptNumber);
        Assert.Equal("74370123456789", response.Cae);
        Assert.Equal(new DateOnly(2026, 10, 5), response.CaeExpirationDate);
        Assert.Contains("<ar:Concepto>2</ar:Concepto>", handler.Requests.Single().Body);
        Assert.Contains("<ar:DocTipo>99</ar:DocTipo>", handler.Requests.Single().Body);
        Assert.Contains("<ar:CondicionIVAReceptorId>5</ar:CondicionIVAReceptorId>", handler.Requests.Single().Body);
        Assert.Contains("<ar:FchServDesde>20260901</ar:FchServDesde>", handler.Requests.Single().Body);
    }

    [Fact]
    public async Task RequestCaeAsync_ParsesRejectedResponseWithErrors()
    {
        var handler = new QueueHandler(BuildRejectedCaeResponse());
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var response = await client.RequestCaeAsync(CreateAuth(), CreateRequest());

        Assert.True(response.IsRejected);
        Assert.False(response.IsAuthorized);
        Assert.Equal(10016, response.Errors.Single().Code);
        Assert.Contains("rechazado", response.Errors.Single().Message);
    }

    public static string BuildLastAuthorizedResponse() => """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <FECompUltimoAutorizadoResponse xmlns="http://ar.gov.afip.dif.FEV1/">
              <FECompUltimoAutorizadoResult>
                <PtoVta>1</PtoVta>
                <CbteTipo>11</CbteTipo>
                <CbteNro>125</CbteNro>
              </FECompUltimoAutorizadoResult>
            </FECompUltimoAutorizadoResponse>
          </soap:Body>
        </soap:Envelope>
        """;

    public static string BuildAuthorizedCaeResponse() => """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <FECAESolicitarResponse xmlns="http://ar.gov.afip.dif.FEV1/">
              <FECAESolicitarResult>
                <FeCabResp>
                  <Cuit>20111111112</Cuit>
                  <PtoVta>1</PtoVta>
                  <CbteTipo>11</CbteTipo>
                  <FchProceso>20260925123000</FchProceso>
                  <CantReg>1</CantReg>
                  <Resultado>A</Resultado>
                  <Reproceso>N</Reproceso>
                </FeCabResp>
                <FeDetResp>
                  <FECAEDetResponse>
                    <Concepto>2</Concepto>
                    <DocTipo>99</DocTipo>
                    <DocNro>0</DocNro>
                    <CbteDesde>126</CbteDesde>
                    <CbteHasta>126</CbteHasta>
                    <CbteFch>20260925</CbteFch>
                    <Resultado>A</Resultado>
                    <CAE>74370123456789</CAE>
                    <CAEFchVto>20261005</CAEFchVto>
                  </FECAEDetResponse>
                </FeDetResp>
              </FECAESolicitarResult>
            </FECAESolicitarResponse>
          </soap:Body>
        </soap:Envelope>
        """;

    public static string BuildRejectedCaeResponse() => """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <FECAESolicitarResponse xmlns="http://ar.gov.afip.dif.FEV1/">
              <FECAESolicitarResult>
                <FeCabResp>
                  <Resultado>R</Resultado>
                </FeCabResp>
                <FeDetResp>
                  <FECAEDetResponse>
                    <CbteDesde>126</CbteDesde>
                    <Resultado>R</Resultado>
                  </FECAEDetResponse>
                </FeDetResp>
                <Errors>
                  <Err>
                    <Code>10016</Code>
                    <Msg>Comprobante rechazado para la prueba.</Msg>
                  </Err>
                </Errors>
              </FECAESolicitarResult>
            </FECAESolicitarResponse>
          </soap:Body>
        </soap:Envelope>
        """;

    private static Wsfev1SoapClient CreateClient(HttpClient httpClient) =>
        new(httpClient, new Wsfev1Options { ServiceUrl = new Uri("https://wswhomo.afip.gov.ar/wsfev1/service.asmx") });

    private static WsfeAuth CreateAuth() => new("token", "sign", 20_111_111_112);

    private static WsfeInvoiceRequest CreateRequest() => new(
        PointOfSale: 1,
        ReceiptType: 11,
        Concept: 2,
        DocumentType: 99,
        DocumentNumber: 0,
        ReceiptNumber: 126,
        ReceiptDate: new DateOnly(2026, 9, 25),
        TotalAmount: 1250m,
        NonTaxedAmount: 0m,
        NetAmount: 1250m,
        ExemptAmount: 0m,
        TaxAmount: 0m,
        VatAmount: 0m,
        ServiceFrom: new DateOnly(2026, 9, 1),
        ServiceTo: new DateOnly(2026, 9, 30),
        PaymentDueDate: new DateOnly(2026, 9, 25),
        CurrencyId: "PES",
        CurrencyRate: 1m,
        ReceiverVatConditionId: 5);

    private sealed class QueueHandler(params string[] responses) : HttpMessageHandler
    {
        private readonly Queue<string> _responses = new(responses);

        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.RequestUri?.ToString() ?? string.Empty, body));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responses.Dequeue(), Encoding.UTF8, "text/xml"),
            };
        }
    }

    private sealed record CapturedRequest(string Url, string Body);
}
