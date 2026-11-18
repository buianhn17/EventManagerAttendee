using EventManagerAttendee.Models;
using EventManagerAttendee.ViewModels;

namespace EventManagerAttendee.Views;

/// <summary>
/// Shows one event and its sessions. The page binds directly to the EventModel;
/// the star button goes through the list's ViewModel so the favourite is saved
/// in one place and the list updates too.
/// </summary>
public partial class EventDetailPage : ContentPage
{
    private readonly EventModel _event;
    private readonly EventsViewModel _listViewModel;

    public EventDetailPage(EventModel selectedEvent, EventsViewModel listViewModel)
    {
        InitializeComponent();
        _event = selectedEvent;
        _listViewModel = listViewModel;
        BindingContext = _event;
    }

    private void OnStarClicked(object? sender, EventArgs e)
    {
        _listViewModel.ToggleFavourite(_event);
    }
}
