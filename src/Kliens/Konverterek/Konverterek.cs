using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Leltarkezelo.Client.ViewModels;

namespace Leltarkezelo.Client.Konverterek;

/// <summary>
/// A visszajelzés színe (UC-06): zöld = rendben, narancs = ismételt, sárga = más körzet, lila = figyelmeztetés
/// (többlet, nem aktív eszköz), piros = ismeretlen kód vagy hiba, kék = tájékoztatás, szürke = semleges.
/// A paraméter: <c>Hatter</c> (halvány háttér), <c>Szegely</c> vagy <c>Szoveg</c> (erős szín).
/// </summary>
public sealed class JelzesSzinKonverter : IValueConverter
{
    private static readonly Dictionary<Jelzes, (SolidColorBrush Hatter, SolidColorBrush Eros, SolidColorBrush Szoveg)> Szinek = new()
    {
        [Jelzes.Semleges] = Harom("#F2F4F7", "#98A2B3", "#344054"),
        [Jelzes.Info] = Harom("#EFF8FF", "#2E90FA", "#175CD3"),
        [Jelzes.Rendben] = Harom("#ECFDF3", "#12B76A", "#027A48"),
        [Jelzes.Ismetelt] = Harom("#FFF6ED", "#FB6514", "#C4320A"),
        [Jelzes.MasKorzet] = Harom("#FEFBE8", "#EAAA08", "#854A0E"),
        [Jelzes.Figyelmeztetes] = Harom("#F4F3FF", "#7A5AF8", "#5925DC"),
        [Jelzes.Ismeretlen] = Harom("#FEF3F2", "#F04438", "#B42318"),
        [Jelzes.Hiba] = Harom("#FEF3F2", "#D92D20", "#912018"),
    };

    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        var (hatter, eros, szoveg) = Szinek[value is Jelzes j ? j : Jelzes.Semleges];
        return (parameter as string) switch
        {
            "Hatter" => hatter,
            "Szegely" => eros,
            _ => szoveg,
        };
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static (SolidColorBrush, SolidColorBrush, SolidColorBrush) Harom(string hatter, string eros, string szoveg) =>
        (Ecset(hatter), Ecset(eros), Ecset(szoveg));

    private static SolidColorBrush Ecset(string szin)
    {
        var ecset = new SolidColorBrush((Color)ColorConverter.ConvertFromString(szin));
        ecset.Freeze();
        return ecset;
    }
}

/// <summary>
/// Láthatóság egy értékből: a <c>false</c>, a <c>null</c>, az üres szöveg, a 0 és az üres lista elrejt.
/// A <c>Fordit</c> paraméter megfordítja.
/// </summary>
public sealed class LathatosagKonverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var lathato = value switch
        {
            null => false,
            bool b => b,
            string s => s.Length > 0,
            int i => i != 0,
            ICollection c => c.Count > 0,
            _ => true,
        };
        if (parameter as string == "Fordit")
            lathato = !lathato;
        return lathato ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
