using System.Windows;
using System.Windows.Controls;

namespace Leltarkezelo.Client.Views;

/// <summary>Egyszerű ablak egy rövid, kötelező szöveg bekérésére (pl. a sztornó indoklása).</summary>
public partial class SzovegBekeroAblak : Window
{
    public SzovegBekeroAblak(string cim, string kerdes, int maxHossz)
    {
        InitializeComponent();
        Title = cim;
        KerdesSzoveg.Text = kerdes;
        Mezo.MaxLength = maxHossz;
        Loaded += (_, _) => Mezo.Focus();
    }

    public string Szoveg => Mezo.Text.Trim();

    private void SzovegValtozott(object sender, TextChangedEventArgs e) => RendbenGomb.IsEnabled = Szoveg.Length > 0;

    private void Rendben(object sender, RoutedEventArgs e) => DialogResult = true;
}
