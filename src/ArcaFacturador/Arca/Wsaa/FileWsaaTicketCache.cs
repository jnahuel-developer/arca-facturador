using System.IO;
using System.Text.Json;

namespace ArcaFacturador.Arca.Wsaa;

public sealed class FileWsaaTicketCache(string filePath) : IWsaaTicketCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public WsaaLoginTicket? Load(WsaaOptions options, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            var entries = JsonSerializer.Deserialize<List<WsaaTicketCacheEntry>>(stream, JsonOptions) ?? [];
            var entry = entries.FirstOrDefault(candidate => candidate.Matches(options));
            return entry?.Ticket.IsValid(now, options.RenewalMargin) == true
                ? entry.Ticket
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Save(WsaaOptions options, WsaaLoginTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(ticket);

        var entries = ReadEntries();
        entries.RemoveAll(entry => entry.Matches(options));
        entries.Add(WsaaTicketCacheEntry.From(options, ticket));
        WriteEntries(entries);
    }

    public void Clear(WsaaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var entries = ReadEntries();
        entries.RemoveAll(entry => entry.Matches(options));
        WriteEntries(entries);
    }

    private List<WsaaTicketCacheEntry> ReadEntries()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            return JsonSerializer.Deserialize<List<WsaaTicketCacheEntry>>(stream, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private void WriteEntries(IReadOnlyCollection<WsaaTicketCacheEntry> entries)
    {
        var directoryPath = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, entries, JsonOptions);
    }

    private sealed record WsaaTicketCacheEntry(
        string Service,
        string? RepresentedCuit,
        string LoginUrl,
        WsaaLoginTicket Ticket)
    {
        public static WsaaTicketCacheEntry From(WsaaOptions options, WsaaLoginTicket ticket) =>
            new(options.Service, options.RepresentedCuit, options.LoginUrl.ToString(), ticket);

        public bool Matches(WsaaOptions options) =>
            string.Equals(Service, options.Service, StringComparison.OrdinalIgnoreCase)
            && string.Equals(RepresentedCuit, options.RepresentedCuit, StringComparison.Ordinal)
            && string.Equals(LoginUrl, options.LoginUrl.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
