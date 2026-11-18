using System.Globalization;
using System.Text.Json.Serialization;
using EventManagerAttendee.ViewModels;

namespace EventManagerAttendee.Models;

/// <summary>
/// One published event, read from the Firestore "events" collection that the
/// Android admin app uploads to (coursework feature g).
/// </summary>
public class EventModel : ObservableObject
{
    private static readonly CultureInfo UkCulture = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>Firestore document ID (= the event ID in the admin app's SQLite database).</summary>
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Date as text, DD/MM/YYYY (same format as the admin app).</summary>
    public string Date { get; set; } = "";
    /// <summary>Date in milliseconds - used only for sorting.</summary>
    public long DateMillis { get; set; }
    public string Time { get; set; } = "";
    public string Location { get; set; } = "";
    public string Organiser { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusLabel { get; set; } = "";
    public int Attendees { get; set; }
    public string Description { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public List<SessionModel> Sessions { get; set; } = new();

    // ===== Feature (h): favourite star =====

    private bool _isFavourite;

    /// <summary>Saved on the device by FavouritesService, not in the cloud.</summary>
    [JsonIgnore]
    public bool IsFavourite
    {
        get => _isFavourite;
        set
        {
            if (SetProperty(ref _isFavourite, value))
            {
                OnPropertyChanged(nameof(StarIcon)); // the star must be redrawn too
            }
        }
    }

    /// <summary>Filled star for favourites, empty star otherwise.</summary>
    [JsonIgnore]
    public string StarIcon => IsFavourite ? "★" : "☆";

    // ===== Values prepared for display =====

    /// <summary>The date as a real DateTime, or null if the text is not DD/MM/YYYY.</summary>
    [JsonIgnore]
    public DateTime? DateValue =>
        DateTime.TryParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) ? date : null;

    /// <summary>"Fri, 25 Dec 2026 · 09:30"</summary>
    [JsonIgnore]
    public string DateTimeText =>
        (DateValue?.ToString("ddd, d MMM yyyy", UkCulture) ?? Date) + " · " + Time;

    /// <summary>"Conference · Upcoming"</summary>
    [JsonIgnore]
    public string TypeAndStatus => $"{TypeLabel} · {StatusLabel}";

    [JsonIgnore]
    public bool IsCancelled => Status == "CANCELLED";

    /// <summary>"Dr Tuan Nguyen · 3 sessions"</summary>
    [JsonIgnore]
    public string OrganiserAndSessions =>
        $"{Organiser} · {Sessions.Count} session{(Sessions.Count == 1 ? "" : "s")}";

    [JsonIgnore]
    public string SessionsHeader => $"Sessions ({Sessions.Count})";

    [JsonIgnore]
    public bool HasNoSessions => Sessions.Count == 0;

    [JsonIgnore]
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    /// <summary>"events@gre.ac.uk · 0912345678" - only what was entered.</summary>
    [JsonIgnore]
    public string ContactText => string.Join(" · ",
        new[] { ContactEmail, ContactPhone }.Where(part => !string.IsNullOrWhiteSpace(part)));

    [JsonIgnore]
    public bool HasContact => ContactText.Length > 0;

    /// <summary>
    /// Search (feature g): matches when the NAME contains the text (any case),
    /// or the DATE contains it - so typing "25/12" finds events on 25 December.
    /// </summary>
    public bool Matches(string text) =>
        Name.Contains(text, StringComparison.OrdinalIgnoreCase)
        || Date.Contains(text, StringComparison.Ordinal);
}
