using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ClockWidg.Models;
using ClockWidg.Services;

namespace ClockWidg;

/// <summary>
/// Picks a city for one slot. What the user types is resolved through the geocoder
/// before it is accepted, so a typo is caught here rather than leaving a row on the
/// widget showing the wrong side of the world.
/// </summary>
public partial class LocationPickerWindow : Window
{
    private readonly GeoService _geo;

    /// <summary>The chosen city, or null if the dialog was cancelled or the slot cleared.</summary>
    public CityClock? Result { get; private set; }

    /// <summary>True when the user asked for this slot to be emptied.</summary>
    public bool Removed { get; private set; }

    public LocationPickerWindow(int slot, CityClock? existing, GeoService geo)
    {
        InitializeComponent();
        _geo = geo;

        HeaderText.Text = $"CITY {slot + 1}";
        RemoveButton.Visibility = existing is null ? Visibility.Collapsed : Visibility.Visible;

        if (existing != null)
        {
            LocationBox.Text = existing.Query.Length > 0 ? existing.Query : existing.Name;
            LabelBox.Text = existing.Name;
        }

        // Borderless, so there is no title bar to drag by.
        MouseLeftButtonDown += (_, _) => { if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove(); };

        Loaded += (_, _) => { LocationBox.Focus(); LocationBox.SelectAll(); };
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        Removed = true;
        DialogResult = true;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        string query = LocationBox.Text.Trim();
        if (query.Length == 0) { Complain("Type a town, a postal code, or an airport code."); LocationBox.Focus(); return; }

        // Both buttons: a cancel that closes the dialog mid-lookup would leave this
        // continuation saving a city the user rejected, then throw setting
        // DialogResult on a window no longer shown as a dialog.
        SaveButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        Note("Looking it up…", muted: true);

        ResolvedPlace? place;
        try
        {
            place = await _geo.ResolveAsync(query);
        }
        catch (Exception)
        {
            Complain("Couldn't reach the geocoder — check the connection.");
            SaveButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            return;
        }

        // Closed some other way (Alt+F4) while the lookup ran: the user didn't save.
        if (!IsVisible) return;

        if (place is null)
        {
            Complain($"No match for \"{query}\".");
            SaveButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            LocationBox.Focus();
            return;
        }

        // A zone the geocoder knows but this machine doesn't is worth catching here.
        if (!TimeZones.TryResolve(place.TimeZoneId, out _))
        {
            Complain($"Windows has no time zone for {place.Detail} ({place.TimeZoneId}).");
            SaveButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            return;
        }

        string label = LabelBox.Text.Trim();
        if (label.Length == 0) label = place.Name.ToUpperInvariant();

        Result = new CityClock
        {
            Name = label,
            TimeZoneId = place.TimeZoneId,
            Query = query,
        };
        DialogResult = true;
    }

    private void Note(string message, bool muted)
    {
        NoteText.Text = message;
        NoteText.Foreground = new SolidColorBrush(Color.FromRgb(0x8F, 0xA0, 0xB4));
        NoteText.Opacity = muted ? 0.6 : 1.0;
    }

    private void Complain(string message)
    {
        NoteText.Text = message;
        NoteText.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x45));
        NoteText.Opacity = 1.0;
    }
}
