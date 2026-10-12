using System.Windows;
using Leltarkezelo.Client.Views;
using Microsoft.Win32;

namespace Leltarkezelo.Client.Szolgaltatasok;

/// <summary>Párbeszédablakok egy helyen, hogy a nézetmodellek ne hivatkozzanak közvetlenül ablakokra.</summary>
public sealed class Parbeszedek
{
    private static Window? Tulajdonos => Application.Current?.MainWindow;

    public bool Megerosites(string cim, string uzenet) =>
        MessageBox.Show(Tulajdonos!, uzenet, cim, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void Tajekoztatas(string cim, string uzenet) =>
        MessageBox.Show(Tulajdonos!, uzenet, cim, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Hiba(string cim, string uzenet) =>
        MessageBox.Show(Tulajdonos!, uzenet, cim, MessageBoxButton.OK, MessageBoxImage.Warning);

    /// <summary>Rövid szöveg bekérése (pl. a sztornó indoklása); <c>null</c>, ha a felhasználó megszakította.</summary>
    public string? SzovegBekerese(string cim, string kerdes, int maxHossz)
    {
        var ablak = new SzovegBekeroAblak(cim, kerdes, maxHossz) { Owner = Tulajdonos };
        return ablak.ShowDialog() == true ? ablak.Szoveg : null;
    }

    public string? ExcelMegnyitasa()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Excel forrásfájl kiválasztása",
            Filter = "Excel-munkafüzet (*.xlsx)|*.xlsx|Minden fájl (*.*)|*.*",
        };
        return dialog.ShowDialog(Tulajdonos) == true ? dialog.FileName : null;
    }

    public string? ExcelMentese(string javasoltNev)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Mentés Excelbe",
            FileName = javasoltNev,
            DefaultExt = ".xlsx",
            Filter = "Excel-munkafüzet (*.xlsx)|*.xlsx",
        };
        return dialog.ShowDialog(Tulajdonos) == true ? dialog.FileName : null;
    }
}
