using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Leltarkezelo.Client.Szolgaltatasok;
using LeltarKezelo.Kozos.Auth;

namespace Leltarkezelo.Client.ViewModels;

public sealed partial class BejelentkezesViewModel(ApiKliens api) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BelepesCommand))]
    private string _felhasznalonev = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BelepesCommand))]
    private string _jelszo = "";

    [ObservableProperty]
    private string? _hiba;

    [ObservableProperty]
    private string? _tajekoztatas;

    public string SzerverCim => api.SzerverCim?.ToString() ?? "";

    /// <summary>Sikeres bejelentkezés után a főablak erre vált át a program felületére.</summary>
    public event EventHandler<BejelentkezesValasz>? Belepett;

    private bool BelephetE() => Felhasznalonev.Trim().Length > 0 && Jelszo.Length > 0;

    [RelayCommand(CanExecute = nameof(BelephetE))]
    private async Task BelepesAsync()
    {
        Hiba = null;
        Tajekoztatas = null;
        try
        {
            var valasz = await api.BejelentkezesAsync(Felhasznalonev.Trim(), Jelszo);
            Jelszo = "";
            Belepett?.Invoke(this, valasz);
        }
        catch (ApiHiba ex)
        {
            Hiba = ex.Message;
        }
    }
}
