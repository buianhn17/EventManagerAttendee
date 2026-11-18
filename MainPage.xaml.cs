using EventManagerAttendee.Models;
using EventManagerAttendee.Services;
using EventManagerAttendee.ViewModels;
using EventManagerAttendee.Views;

namespace EventManagerAttendee;

/// <summary>
/// Code-behind of the event list. It only creates the ViewModel and handles taps;
/// all the logic is in <see cref="EventsViewModel"/>.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly EventsViewModel _viewModel;
    private bool _hasLoaded;

    public MainPage()
    {
        InitializeComponent();
        _viewModel = new EventsViewModel(new FirestoreService(), new FavouritesService(),
            new EventCacheService());
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_hasLoaded)
        {
            _hasLoaded = true;
            await _viewModel.LoadAsync(); // first time: download from the cloud
        }
        else
        {
            _viewModel.ApplyFilter(); // back from details: a star may have changed
        }
    }

    /// <summary>Tap on a card: open its details and sessions.</summary>
    private async void OnEventTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is EventModel selected)
        {
            await Navigation.PushAsync(new EventDetailPage(selected, _viewModel));
        }
    }

    /// <summary>Tap on a star: add / remove the event from the favourites.</summary>
    private void OnStarClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is EventModel selected)
        {
            _viewModel.ToggleFavourite(selected);
        }
    }
}
