using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClockWidg.Models;

namespace ClockWidg.Faces;

/// <summary>
/// Remembers a TextBlock's designed font so a face can swap in a user-picked
/// <see cref="FontChoice"/> and swap back out again.
/// </summary>
internal sealed class FontDefaults
{
    private readonly FontFamily _family;
    private readonly double _size;
    private readonly FontWeight _weight;
    private readonly FontStyle _style;

    private FontDefaults(TextBlock source)
    {
        _family = source.FontFamily;
        _size = source.FontSize;
        _weight = source.FontWeight;
        _style = source.FontStyle;
    }

    public static FontDefaults From(TextBlock source) => new(source);

    /// <summary>Applies <paramref name="choice"/>, or restores the designed font when it is null.</summary>
    public void ApplyTo(TextBlock target, FontChoice? choice)
    {
        if (choice is null)
        {
            target.FontFamily = _family;
            target.FontSize = _size;
            target.FontWeight = _weight;
            target.FontStyle = _style;
            return;
        }

        target.FontFamily = new FontFamily(choice.Family);
        target.FontSize = choice.Size > 0 ? choice.Size : _size;
        target.FontWeight = choice.Bold ? FontWeights.Bold : FontWeights.Normal;
        target.FontStyle = choice.Italic ? FontStyles.Italic : FontStyles.Normal;
    }
}
