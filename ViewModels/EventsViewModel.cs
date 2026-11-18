using System.Collections.ObjectModel;
using System.Windows.Input;
using EventManagerAttendee.Models;
using EventManagerAttendee.Services;

namespace EventManagerAttendee.ViewModels;

/// <summary>
/// Logic of the event list screen (MVVM: the page only shows what this class exposes).
/// <list type="bullet">
///   <item>Feature (g): load the events from the cloud; search by name and by date.</item>
///   <item>Feature (h): mark events as favourites and show only the favourites.</item>
/// </list>
/// </summary>
public class EventsViewModel : ObservableObject
{
    private readonly FirestoreService _firestore;
    private readonly FavouritesService _favourites;
    private readonly EventCacheService _cache;

    /// <summary>Every event downloaded; <see cref="Events"/> is the filtered view of it.</summary>
    private List<EventModel> _allEvents = new();

    public EventsViewModel(FirestoreService firestore, FavouritesService favourites,
        EventCacheService cache)
    {
        _firestore = firestore;
        _favourites = favourites;
        _cache = cache;

        LoadCommand = new Command(async () => await LoadAsync());
        ToggleFavouritesFilterCommand = new Command(() => ShowFavouritesOnly = !ShowFavouritesOnly);
    }

    /// <summary>The events shown in the list (after search and filters).</summary>
    public ObservableCollection<EventModel> Events { get; } = new();

    public ICommand LoadCommand { get; }
    public ICommand ToggleFavouritesFilterCommand { get; }

    // =====================================================================
    // Search and filters - every change re-applies the filter immediately
    // =====================================================================

    private string _searchText = "";
    /// <summary>Text typed in the search bar (name, or part of a date such as "25/12").</summary>
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value ?? "")) ApplyFilter(); }
    }

    private bool _filterByDate;
    /// <summary>When ticked, only events on <see cref="SelectedDate"/> are shown.</summary>
    public bool FilterByDate
    {
        get => _filterByDate;
        set { if (SetProperty(ref _filterByDate, value)) ApplyFilter(); }
    }

    private DateTime? _selectedDate = DateTime.Today;
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set { if (SetProperty(ref _selectedDate, value) && FilterByDate) ApplyFilter(); }
    }

    private bool _showFavouritesOnly;
    /// <summary>Feature (h): quick access to the saved events.</summary>
    public bool ShowFavouritesOnly
    {
        get => _showFavouritesOnly;
        set
        {
            if (SetProperty(ref _showFavouritesOnly, value))
            {
                OnPropertyChanged(nameof(FavouritesButtonText));
                ApplyFilter();
            }
        }
    }

    public string FavouritesButtonText => ShowFavouritesOnly ? "★ Favourites" : "☆ Favourites";

    // =====================================================================
    // Status shown on screen
    // =====================================================================

    private bool _isBusy;
    /// <summary>True while downloading (shows the spinner).</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private bool _isRefreshing;
    /// <summary>Bound to the pull-to-refresh control.</summary>
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    private string _statusText = "";
    /// <summary>"5 events" or "2 of 5 events".</summary>
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private string _messageText = "";
    /// <summary>Offline / error message shown above the list ("" = hidden).</summary>
    public string MessageText
    {
        get => _messageText;
        set
        {
            if (SetProperty(ref _messageText, value))
            {
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    public bool HasMessage => MessageText.Length > 0;

    private string _emptyText = "";
    /// <summary>Text shown when the list is empty (explains why).</summary>
    public string EmptyText
    {
        get => _emptyText;
        set => SetProperty(ref _emptyText, value);
    }

    // =====================================================================
    // Loading from the cloud
    // =====================================================================

    /// <summary>
    /// Downloads the events. Without internet, or if the cloud cannot be reached,
    /// the last saved copy is shown with a message, so the app is still useful offline.
    /// </summary>
    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }
        IsBusy = true;

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ShowSavedCopyAsync("You are offline – showing the events saved on this device.");
                return;
            }

            var events = await _firestore.GetEventsAsync();
            MessageText = "";
            SetEvents(events);
            await _cache.SaveAsync(events);
        }
        catch (Exception)
        {
            await ShowSavedCopyAsync("Could not reach the cloud – showing the events saved on this device. Pull down to try again.");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    private async Task ShowSavedCopyAsync(string message)
    {
        var saved = await _cache.LoadAsync();
        MessageText = saved.Count > 0
            ? message
            : "No internet connection and no saved events yet. Connect and pull down to refresh.";
        SetEvents(saved);
    }

    /// <summary>Replaces all events, marks the favourites, and refreshes the list.</summary>
    private void SetEvents(List<EventModel> events)
    {
        foreach (var e in events)
        {
            e.IsFavourite = _favourites.IsFavourite(e.Id);
        }
        _allEvents = events;
        ApplyFilter();
    }

    // =====================================================================
    // Favourites - feature (h)
    // =====================================================================

    /// <summary>Called by the star buttons on the list and on the details page.</summary>
    public void ToggleFavourite(EventModel e)
    {
        e.IsFavourite = _favourites.Toggle(e.Id);
        if (ShowFavouritesOnly)
        {
            ApplyFilter(); // an un-starred event must disappear from the favourites view
        }
    }

    // =====================================================================
    // Filtering
    // =====================================================================

    /// <summary>Rebuilds <see cref="Events"/> from all events using the current criteria.</summary>
    public void ApplyFilter()
    {
        IEnumerable<EventModel> result = _allEvents;

        if (ShowFavouritesOnly)
        {
            result = result.Where(e => e.IsFavourite);
        }

        var text = SearchText.Trim();
        if (text.Length > 0)
        {
            result = result.Where(e => e.Matches(text));
        }

        if (FilterByDate && SelectedDate is DateTime day)
        {
            result = result.Where(e => e.DateValue?.Date == day.Date);
        }

        var list = result.ToList();
        Events.Clear();
        foreach (var e in list)
        {
            Events.Add(e);
        }

        StatusText = list.Count == _allEvents.Count
            ? $"{_allEvents.Count} event{(_allEvents.Count == 1 ? "" : "s")}"
            : $"{list.Count} of {_allEvents.Count} events";

        EmptyText = _allEvents.Count == 0
            ? "No events have been published yet."
            : ShowFavouritesOnly && !_allEvents.Any(e => e.IsFavourite)
                ? "No favourite events yet. Tap ☆ on an event to save it here."
                : "No events match your search.";
    }
}
