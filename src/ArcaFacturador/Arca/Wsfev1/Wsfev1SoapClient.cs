using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using ArcaFacturador.Arca;

namespace ArcaFacturador.Arca.Wsfev1;

public sealed class Wsfev1SoapClient(HttpClient httpClient, Wsfev1Options options) : IWsfev1Client
{
    private const string Namespace = "http://ar.gov.afip.dif.FEV1/";
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public async Task<WsfeLastAuthorizedResult> GetLastAuthorizedAsync(
        WsfeAuth auth,
        int pointOfSale,
        int receiptType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auth);
        auth.Validate();
        options.Validate();

        var envelope = BuildEnvelope("FECompUltimoAutorizado", BuildLastAuthorizedBody(auth, pointOfSale, receiptType));
        var responseText = await SendAsync("FECompUltimoAutorizado", envelope, cancellationToken).ConfigureAwait(false);
        return ParseLastAuthorized(responseText, pointOfSale, receiptType);
    }

    public async Task<WsfeVoucher?> GetVoucherAsync(
        WsfeAuth auth,
        int pointOfSale,
        int receiptType,
        long receiptNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auth);
        auth.Validate();
        options.Validate();

        if (receiptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(receiptNumber));
        }

        var envelope = BuildEnvelope("FECompConsultar", BuildVoucherQueryBody(auth, pointOfSale, receiptType, receiptNumber));
        var responseText = await SendAsync("FECompConsultar", envelope, cancellationToken).ConfigureAwait(false);
        return ParseVoucher(responseText, pointOfSale, receiptType, receiptNumber);
    }

    public async Task<WsfeCaeResponse> RequestCaeAsync(
        WsfeAuth auth,
        WsfeInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auth);
        ArgumentNullException.ThrowIfNull(request);
        auth.Validate();
        request.Validate();
        options.Validate();

        var envelope = BuildEnvelope("FECAESolicitar", BuildCaeRequestBody(auth, request));
        var responseText = await SendAsync("FECAESolicitar", envelope, cancellationToken).ConfigureAwait(false);
        return ParseCaeResponse(responseText);
    }

    private async Task<string> SendAsync(string operation, string envelope, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, options.ServiceUrl)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "text/xml"),
        };
        httpRequest.Headers.TryAddWithoutValidation("SOAPAction", $"{Namespace}{operation}");

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var fault = ExtractFault(responseText);
            throw new ArcaServiceException(
                ArcaErrorClassifier.Classify(fault),
                $"WSFEv1 devolvió HTTP {(int)response.StatusCode}: {fault}");
        }

        return responseText;
    }

    private static string BuildEnvelope(string operation, string body)
    {
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ar="{Namespace}">
              <soapenv:Header />
              <soapenv:Body>
                <ar:{operation}>
            {body}
                </ar:{operation}>
              </soapenv:Body>
            </soapenv:Envelope>
            """;
    }

    private static string BuildLastAuthorizedBody(WsfeAuth auth, int pointOfSale, int receiptType)
    {
        return $"""
                  <ar:Auth>
                    <ar:Token>{WebUtility.HtmlEncode(auth.Token)}</ar:Token>
                    <ar:Sign>{WebUtility.HtmlEncode(auth.Sign)}</ar:Sign>
                    <ar:Cuit>{auth.Cuit}</ar:Cuit>
                  </ar:Auth>
                  <ar:PtoVta>{pointOfSale}</ar:PtoVta>
                  <ar:CbteTipo>{receiptType}</ar:CbteTipo>
            """;
    }

    private static string BuildVoucherQueryBody(WsfeAuth auth, int pointOfSale, int receiptType, long receiptNumber)
    {
        return $"""
                  <ar:Auth>
                    <ar:Token>{WebUtility.HtmlEncode(auth.Token)}</ar:Token>
                    <ar:Sign>{WebUtility.HtmlEncode(auth.Sign)}</ar:Sign>
                    <ar:Cuit>{auth.Cuit}</ar:Cuit>
                  </ar:Auth>
                  <ar:FeCompConsReq>
                    <ar:CbteTipo>{receiptType}</ar:CbteTipo>
                    <ar:CbteNro>{receiptNumber}</ar:CbteNro>
                    <ar:PtoVta>{pointOfSale}</ar:PtoVta>
                  </ar:FeCompConsReq>
            """;
    }

    private static string BuildCaeRequestBody(WsfeAuth auth, WsfeInvoiceRequest request)
    {
        return $"""
                  <ar:Auth>
                    <ar:Token>{WebUtility.HtmlEncode(auth.Token)}</ar:Token>
                    <ar:Sign>{WebUtility.HtmlEncode(auth.Sign)}</ar:Sign>
                    <ar:Cuit>{auth.Cuit}</ar:Cuit>
                  </ar:Auth>
                  <ar:FeCAEReq>
                    <ar:FeCabReq>
                      <ar:CantReg>1</ar:CantReg>
                      <ar:PtoVta>{request.PointOfSale}</ar:PtoVta>
                      <ar:CbteTipo>{request.ReceiptType}</ar:CbteTipo>
                    </ar:FeCabReq>
                    <ar:FeDetReq>
                      <ar:FECAEDetRequest>
                        <ar:Concepto>{request.Concept}</ar:Concepto>
                        <ar:DocTipo>{request.DocumentType}</ar:DocTipo>
                        <ar:DocNro>{request.DocumentNumber}</ar:DocNro>
                        <ar:CbteDesde>{request.ReceiptNumber}</ar:CbteDesde>
                        <ar:CbteHasta>{request.ReceiptNumber}</ar:CbteHasta>
                        <ar:CbteFch>{FormatDate(request.ReceiptDate)}</ar:CbteFch>
                        <ar:ImpTotal>{FormatAmount(request.TotalAmount)}</ar:ImpTotal>
                        <ar:ImpTotConc>{FormatAmount(request.NonTaxedAmount)}</ar:ImpTotConc>
                        <ar:ImpNeto>{FormatAmount(request.NetAmount)}</ar:ImpNeto>
                        <ar:ImpOpEx>{FormatAmount(request.ExemptAmount)}</ar:ImpOpEx>
                        <ar:ImpTrib>{FormatAmount(request.TaxAmount)}</ar:ImpTrib>
                        <ar:ImpIVA>{FormatAmount(request.VatAmount)}</ar:ImpIVA>
                        <ar:FchServDesde>{FormatDate(request.ServiceFrom)}</ar:FchServDesde>
                        <ar:FchServHasta>{FormatDate(request.ServiceTo)}</ar:FchServHasta>
                        <ar:FchVtoPago>{FormatDate(request.PaymentDueDate)}</ar:FchVtoPago>
                        <ar:MonId>{request.CurrencyId}</ar:MonId>
                        <ar:MonCotiz>{FormatAmount(request.CurrencyRate)}</ar:MonCotiz>
                        <ar:CondicionIVAReceptorId>{request.ReceiverVatConditionId}</ar:CondicionIVAReceptorId>
                      </ar:FECAEDetRequest>
                    </ar:FeDetReq>
                  </ar:FeCAEReq>
            """;
    }

    private static WsfeLastAuthorizedResult ParseLastAuthorized(string xml, int pointOfSale, int receiptType)
    {
        var document = XDocument.Parse(xml);
        var receiptNumber = ParseLong(FindRequiredValue(document, "CbteNro"));
        return new WsfeLastAuthorizedResult(pointOfSale, receiptType, receiptNumber);
    }

    private static WsfeVoucher? ParseVoucher(string xml, int pointOfSale, int receiptType, long receiptNumber)
    {
        var document = XDocument.Parse(xml);
        var resultRoot = FindElement(document, "FECompConsultarResult")
            ?? throw new InvalidOperationException("La respuesta de WSFEv1 no contiene FECompConsultarResult.");
        var voucher = FindElement(resultRoot, "ResultGet");
        if (voucher is null)
        {
            return null;
        }

        var result = FindOptionalChildValue(voucher, "Resultado") ?? string.Empty;
        var cae = FindOptionalChildValue(voucher, "CodAutorizacion")
            ?? FindOptionalChildValue(voucher, "CAE");
        var caeExpirationDate = ParseOptionalDate(
            FindOptionalChildValue(voucher, "FchVto")
            ?? FindOptionalChildValue(voucher, "CAEFchVto"));

        return new WsfeVoucher(
            pointOfSale,
            receiptType,
            receiptNumber,
            result,
            cae,
            caeExpirationDate);
    }

    private static WsfeCaeResponse ParseCaeResponse(string xml)
    {
        var document = XDocument.Parse(xml);
        var resultRoot = FindElement(document, "FECAESolicitarResult")
            ?? throw new InvalidOperationException("La respuesta de WSFEv1 no contiene FECAESolicitarResult.");
        var header = FindElement(resultRoot, "FeCabResp");
        var detail = FindElement(resultRoot, "FECAEDetResponse")
            ?? throw new InvalidOperationException("La respuesta de WSFEv1 no contiene FECAEDetResponse.");

        return new WsfeCaeResponse(
            HeaderResult: FindOptionalChildValue(header, "Resultado") ?? string.Empty,
            DetailResult: FindOptionalChildValue(detail, "Resultado") ?? string.Empty,
            ReceiptNumber: ParseLong(FindOptionalChildValue(detail, "CbteDesde") ?? "0"),
            Cae: FindOptionalChildValue(detail, "CAE"),
            CaeExpirationDate: ParseOptionalDate(FindOptionalChildValue(detail, "CAEFchVto")),
            Observations: ParseMessages(detail, "Observaciones", "Obs"),
            Errors: ParseMessages(resultRoot, "Errors", "Err"));
    }

    private static IReadOnlyList<WsfeMessage> ParseMessages(XElement root, string containerName, string itemName)
    {
        var container = FindElement(root, containerName);
        if (container is null)
        {
            return [];
        }

        return container
            .Descendants()
            .Where(element => element.Name.LocalName == itemName)
            .Select(element => new WsfeMessage(
                Code: (int)ParseLong(FindOptionalChildValue(element, "Code") ?? "0"),
                Message: FindOptionalChildValue(element, "Msg") ?? string.Empty))
            .ToList();
    }

    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyyMMdd", InvariantCulture);

    private static string FormatAmount(decimal amount) =>
        amount.ToString("0.00", InvariantCulture);

    private static DateOnly? ParseOptionalDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.ParseExact(value, "yyyyMMdd", InvariantCulture);
    }

    private static long ParseLong(string value) =>
        long.Parse(value, NumberStyles.Integer, InvariantCulture);

    private static string FindRequiredValue(XDocument document, string localName) =>
        document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == localName)
            ?.Value
        ?? throw new InvalidOperationException($"La respuesta de WSFEv1 no contiene el campo {localName}.");

    private static XElement? FindElement(XContainer root, string localName) =>
        root.Descendants().FirstOrDefault(element => element.Name.LocalName == localName);

    private static string? FindOptionalChildValue(XElement? root, string localName) =>
        root?.Elements().FirstOrDefault(element => element.Name.LocalName == localName)?.Value;

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
