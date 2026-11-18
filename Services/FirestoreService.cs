using System.Text.Json;
using EventManagerAttendee.Models;

namespace EventManagerAttendee.Services;

/// <summary>
/// Reads the events published by the Android admin app from Cloud Firestore
/// (coursework feature g), using the Firestore REST API and HttpClient.
/// <para>
/// No Firebase library is needed: the security rule "allow read: if true" on the
/// events collection lets any attendee app read, while only the admin app writes.
/// </para>
/// </summary>
public class FirestoreService
{
    /// <summary>Firebase project ID (the "project_id" in the admin app's google-services.json).</summary>
    public const string ProjectId = "eventmanager-comp1786";

    private const string EventsUrl =
        "https://firestore.googleapis.com/v1/projects/" + ProjectId +
        "/databases/(default)/documents/events";

    /// <summary>One shared HttpClient for the whole app (recommended by Microsoft).</summary>
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    /// <summary>
    /// Downloads every event (page by page) and returns them sorted by date and time.
    /// Throws HttpRequestException / TaskCanceledException when the cloud cannot be reached.
    /// </summary>
    public async Task<List<EventModel>> GetEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = new List<EventModel>();
        string? pageToken = null;

        do
        {
            var url = EventsUrl + "?pageSize=100";
            if (!string.IsNullOrEmpty(pageToken))
            {
                url += "&pageToken=" + Uri.EscapeDataString(pageToken);
            }

            using var response = await Http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode(); // e.g. 403 if the read rule is missing

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = json.RootElement;

            // An empty collection returns {} - no "documents" property at all
            if (root.TryGetProperty("documents", out var documents))
            {
                foreach (var document in documents.EnumerateArray())
                {
                    events.Add(ParseEvent(document));
                }
            }

            pageToken = root.TryGetProperty("nextPageToken", out var token) ? token.GetString() : null;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return events
            .OrderBy(e => e.DateMillis)
            .ThenBy(e => e.Time, StringComparer.Ordinal)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // =====================================================================
    // Firestore REST JSON -> C# objects
    // Every value is wrapped with its type, e.g.
    //   "name":      { "stringValue": "Tech Summit" }
    //   "attendees": { "integerValue": "50" }        (64-bit numbers are sent as text)
    //   "sessions":  { "arrayValue": { "values": [ { "mapValue": { "fields": {...} } } ] } }
    // =====================================================================

    private static EventModel ParseEvent(JsonElement document)
    {
        // "name": "projects/<id>/databases/(default)/documents/events/12" -> "12"
        var path = document.GetProperty("name").GetString() ?? "";
        var fields = document.TryGetProperty("fields", out var f) ? f : default;

        return new EventModel
        {
            Id = path[(path.LastIndexOf('/') + 1)..],
            Name = GetString(fields, "name"),
            Date = GetString(fields, "date"),
            DateMillis = GetLong(fields, "dateMillis"),
            Time = GetString(fields, "time"),
            Location = GetString(fields, "location"),
            Organiser = GetString(fields, "organiser"),
            TypeLabel = GetString(fields, "typeLabel"),
            Status = GetString(fields, "status"),
            StatusLabel = GetString(fields, "statusLabel"),
            Attendees = (int)GetLong(fields, "attendees"),
            Description = GetString(fields, "description"),
            ContactEmail = GetString(fields, "contactEmail"),
            ContactPhone = GetString(fields, "contactPhone"),
            Sessions = ParseSessions(fields)
        };
    }

    private static List<SessionModel> ParseSessions(JsonElement eventFields)
    {
        var sessions = new List<SessionModel>();
        if (!TryGetField(eventFields, "sessions", out var value)
            || !value.TryGetProperty("arrayValue", out var array)
            || !array.TryGetProperty("values", out var items)) // an empty array has no "values"
        {
            return sessions;
        }

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("mapValue", out var map)
                || !map.TryGetProperty("fields", out var s))
            {
                continue;
            }
            sessions.Add(new SessionModel
            {
                Id = GetLong(s, "id"),
                Name = GetString(s, "name"),
                Time = GetString(s, "time"),
                Speaker = GetString(s, "speaker"),
                Room = GetString(s, "room"),
                TypeLabel = GetString(s, "typeLabel"),
                Description = GetString(s, "description")
            });
        }
        return sessions.OrderBy(s => s.Time, StringComparer.Ordinal).ToList();
    }

    /// <summary>Finds a field; safe when "fields" is missing (a document with no data).</summary>
    private static bool TryGetField(JsonElement fields, string key, out JsonElement value)
    {
        value = default;
        return fields.ValueKind == JsonValueKind.Object && fields.TryGetProperty(key, out value);
    }

    /// <summary>Text value, or "" when missing or null (optional fields).</summary>
    private static string GetString(JsonElement fields, string key) =>
        TryGetField(fields, key, out var value) && value.TryGetProperty("stringValue", out var text)
            ? text.GetString() ?? ""
            : "";

    /// <summary>Whole number, or 0 when missing.</summary>
    private static long GetLong(JsonElement fields, string key)
    {
        if (!TryGetField(fields, key, out var value))
        {
            return 0;
        }
        if (value.TryGetProperty("integerValue", out var integer)
            && long.TryParse(integer.GetString(), out var number))
        {
            return number;
        }
        if (value.TryGetProperty("doubleValue", out var dbl))
        {
            return (long)dbl.GetDouble();
        }
        return 0;
    }
}
