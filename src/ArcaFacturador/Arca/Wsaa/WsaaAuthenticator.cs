using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using ArcaFacturador.Arca;
using ArcaFacturador.Persistence;

namespace ArcaFacturador.Arca.Wsaa;

public sealed class WsaaAuthenticator : IWsaaTicketProvider
{
    private readonly WsaaOptions _options;
    private readonly X509Certificate2 _certificate;
    private readonly WsaaLoginTicketRequestFactory _requestFactory;
    private readonly WsaaCmsSigner _signer;
    private readonly WsaaSoapClient _client;
    private readonly Func<DateTimeOffset> _clock;
    private readonly IWsaaTicketCache? _ticketCache;
    private WsaaLoginTicket? _cachedTicket;

    public WsaaAuthenticator(
        WsaaOptions options,
        X509Certificate2 certificate,
        HttpClient httpClient,
        IWsaaTicketCache? ticketCache = null,
        Func<DateTimeOffset>? clock = null)
        : this(options, certificate, new WsaaLoginTicketRequestFactory(), new WsaaCmsSigner(), new WsaaSoapClient(httpClient), ticketCache, clock)
    {
    }

    public WsaaAuthenticator(
        WsaaOptions options,
        X509Certificate2 certificate,
        WsaaLoginTicketRequestFactory requestFactory,
        WsaaCmsSigner signer,
        WsaaSoapClient client,
        IWsaaTicketCache? ticketCache = null,
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
        _ticketCache = ticketCache ?? new FileWsaaTicketCache(LocalDataPaths.WsaaTicketCacheFilePath);
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public async Task<WsaaLoginTicket> GetTicketAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock();
        if (_cachedTicket?.IsValid(now, _options.RenewalMargin) == true)
        {
            return _cachedTicket;
        }

        _cachedTicket = _ticketCache?.Load(_options, now);
        if (_cachedTicket?.IsValid(now, _options.RenewalMargin) == true)
        {
            return _cachedTicket;
        }

        var loginTicketRequestXml = _requestFactory.Create(_options, now);
        var signedCms = _signer.Sign(loginTicketRequestXml, _certificate);
        try
        {
            _cachedTicket = await _client.LoginCmsAsync(_options.LoginUrl, signedCms, cancellationToken).ConfigureAwait(false);
        }
        catch (ArcaServiceException exception) when (exception.Kind == ArcaServiceErrorKind.ExistingValidTicket)
        {
            _cachedTicket = _ticketCache?.Load(_options, now);
            if (_cachedTicket?.IsValid(now, _options.RenewalMargin) == true)
            {
                return _cachedTicket;
            }

            throw;
        }

        _ticketCache?.Save(_options, _cachedTicket);
        return _cachedTicket;
    }
}
