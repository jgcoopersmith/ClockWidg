using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClockWidg.Models;

namespace ClockWidg;

public partial class LocationPickerWindow : Window
{
    private readonly List<TimeZoneInfo> _zones = TimeZoneInfo.GetSystemTimeZones().ToList();

    /// <summary>The chosen city, or null if the slot was left empty or cleared.</summary>
    public CityClock? Result { get; private set; }

    /// <summary>True when the user asked for this slot to be emptied.</summary>
    public bool Removed { get; private set; }

    public LocationPickerWindow(CityClock? existing)
    {
        InitializeComponent();
        ShowZones(_zones);

        if (existing != null)
        {
            LabelBox.Text = existing.Name;
            var match = _zones.FirstOrDefault(z => z.Id == existing.TimeZoneId);
            if (match != null) ZoneList.SelectedItem = ZoneList.Items
                .Cast<ListBoxItem>()
                .FirstOrDefault(i => (string)i.Tag == match.Id);
        }

        Loaded += (_, _) => SearchBox.Focus();
    }

    private void ShowZones(IEnumerable<TimeZoneInfo> zones)
    {
        ZoneList.Items.Clear();
        foreach (var z in zones)
            ZoneList.Items.Add(new ListBoxItem { Content = z.DisplayName, Tag = z.Id });
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string q = SearchBox.Text.Trim();
        ShowZones(q.Length == 0
            ? _zones
            : _zones.Where(z => z.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || z.Id.Contains(q, StringComparison.OrdinalIgnoreCase)));
    }

    private void ZoneList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Only prefill the label while the user hasn't written one of their own.
        if (ZoneList.SelectedItem is not ListBoxItem item) return;
        if (!string.IsNullOrWhiteSpace(LabelBox.Text) && LabelBox.Tag as string != "auto") return;

        LabelBox.Text = SuggestLabel((string)item.Content);
        LabelBox.Tag = "auto";
    }

    /// <summary>Turns "(UTC-05:00) Eastern Time (US &amp; Canada)" into "Eastern Time".</summary>
    private static string SuggestLabel(string displayName)
    {
        int close = displayName.IndexOf(')');
        string rest = close >= 0 && close + 1 < displayName.Length
            ? displayName[(close + 1)..].Trim()
            : displayName;

        int paren = rest.IndexOf('(');
        if (paren > 0) rest = rest[..paren].Trim();

        return rest.Length == 0 ? displayName : rest.ToUpperInvariant();
    }

    private void ZoneList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ZoneList.SelectedItem != null) Ok_Click(sender, e);
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (ZoneList.SelectedItem is not ListBoxItem item)
        {
            MessageBox.Show(this, "Pick a city from the list first.", "Choose a City",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string id = (string)item.Tag;
        string label = string.IsNullOrWhiteSpace(LabelBox.Text)
            ? SuggestLabel((string)item.Content)
            : LabelBox.Text.Trim();

        Result = new CityClock { Name = label, TimeZoneId = id };
        DialogResult = true;
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        Removed = true;
        DialogResult = true;
    }
}
