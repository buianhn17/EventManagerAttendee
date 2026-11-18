using System.Text.Json;
using EventManagerAttendee.Models;

namespace EventManagerAttendee.Services;

/// <summary>
/// Keeps a copy of the last events downloaded from the cloud in a file on the device.
/// When there is no internet, the app shows this copy instead of an empty screen,
/// so favourite events are still available offline.
/// </summary>
public class EventCacheService
{
    private static readonly string CacheFile =
        Path.Combine(FileSystem.AppDataDirectory, "events_cache.json");

    public async Task SaveAsync(List<EventModel> events)
    {
        try
        {
            await using var stream = File.Create(CacheFile);
            await JsonSerializer.SerializeAsync(stream, events);
        }
        catch (Exception)
        {
            // A failed cache write is not important: the data is still in the cloud
        }
    }

    /// <returns>The saved events, or an empty list if there is no (valid) copy.</returns>
    public async Task<List<EventModel>> LoadAsync()
    {
        try
        {
            if (!File.Exists(CacheFile))
            {
                return new List<EventModel>();
            }
            await using var stream = File.OpenRead(CacheFile);
            return await JsonSerializer.DeserializeAsync<List<EventModel>>(stream)
                   ?? new List<EventModel>();
        }
        catch (Exception)
        {
            return new List<EventModel>();
        }
    }
}
