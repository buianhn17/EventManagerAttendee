namespace EventManagerAttendee.Services;

/// <summary>
/// Remembers the attendee's favourite events on the device - coursework feature (h).
/// <para>
/// The IDs are kept in .NET MAUI Preferences (SharedPreferences on Android,
/// local app settings on Windows), so they survive closing the app and need no account.
/// Example of the saved value: "3,7,12".
/// </para>
/// </summary>
public class FavouritesService
{
    private const string PreferencesKey = "favourite_event_ids";

    private readonly HashSet<string> _ids;

    public FavouritesService()
    {
        var saved = Preferences.Default.Get(PreferencesKey, "");
        _ids = new HashSet<string>(saved.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }

    public bool IsFavourite(string eventId) => _ids.Contains(eventId);

    /// <summary>Adds the event if it was not a favourite, removes it otherwise.</summary>
    /// <returns>True if the event is now a favourite.</returns>
    public bool Toggle(string eventId)
    {
        if (!_ids.Remove(eventId))
        {
            _ids.Add(eventId);
        }
        Preferences.Default.Set(PreferencesKey, string.Join(',', _ids));
        return _ids.Contains(eventId);
    }
}
