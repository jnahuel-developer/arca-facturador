using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace ArcaFacturador.Arca.Wsaa;

public sealed class WsaaAuthenticator : IWsaaTicketProvider
{
    private readonly WsaaOptions _options;
    private readonly X509Certificate2 _certificate;
    private readonly WsaaLoginTicketRequestFactory _requestFactory;
    private readonly WsaaCmsSigner _signer;
    private readonly WsaaSoapClient _client;
    private readonly Func<DateTimeOffset> _clock;
    private WsaaLoginTicket? _cachedTicket;

    public WsaaAuthenticator(
        WsaaOptions options,
        X509Certificate2 certificate,
        HttpClient httpClient,
        Func<DateTimeOffset>? clock = null)
        : this(options, certificate, new WsaaLoginTicketRequestFactory(), new WsaaCmsSigner(), new WsaaSoapClient(httpClient), clock)
    {
    }

    public WsaaAuthenticator(
        WsaaOptions options,
        X509Certificate2 certificate,
        WsaaLoginTicketRequestFactory requestFactory,
        WsaaCmsSigner signer,
        WsaaSoapClient client,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNull(requestFactory);
        ArgumentNullException.ThrowIfNull(signer);
        ArgumentNullException.ThrowIfNull(client);

        options.Validate();
        _options = options;
        _certificate = certificate;
        _requestFactory = requestFactory;
        _signer = signer;
        _client = client;
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public async Task<WsaaLoginTicket> GetTicketAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock();
        if (_cachedTicket?.IsValid(now, _options.RenewalMargin) == true)
        {
            return _cachedTicket;
        }

        var loginTicketRequestXml = _requestFactory.Create(_options, now);
        var signedCms = _signer.Sign(loginTicketRequestXml, _certificate);
        _cachedTicket = await _client.LoginCmsAsync(_options.LoginUrl, signedCms, cancellationToken).ConfigureAwait(false);
        return _cachedTicket;
    }
}
