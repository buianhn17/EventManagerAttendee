namespace EventManagerAttendee.Models;

/// <summary>
/// One session of an event, as uploaded by the Android admin app
/// (it is stored inside the event document, in the "sessions" array).
/// </summary>
public class SessionModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Time { get; set; } = "";
    public string Speaker { get; set; } = "";
    public string Room { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>"Dr A · Hall 1" - only the parts that were entered.</summary>
    public string MetaText => string.Join(" · ",
        new[] { Speaker, Room }.Where(part => !string.IsNullOrWhiteSpace(part)));

    public bool HasMeta => MetaText.Length > 0;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
}
