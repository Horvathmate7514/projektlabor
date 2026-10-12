using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Leltarkezelo.Client.Szolgaltatasok;
using LeltarKezelo.Kozos.Auth;

namespace Leltarkezelo.Client.ViewModels;

public sealed record MenuPont(string Cim, OldalViewModel Oldal, bool Kesobb = false);

/// <summary>
/// A főablak: bejelentkezés előtt a belépő képernyőt, utána az oldalsó menüt és a kiválasztott oldalt mutatja.
/// Az oldalak bejelentkezésenként újra létrejönnek, így egy másik felhasználó nem látja az előző adatait.
/// </summary>
public sealed partial class FoAblakViewModel : ObservableObject
{
    private readonly ApiKliens _api;
    private readonly Func<LeltarozasViewModel> _leltarozas;
    private readonly Func<EszkozokViewModel> _eszkozok;
    private readonly Func<ImportViewModel> _import;
    private readonly DispatcherTimer _allapotFigyelo = new() { Interval = TimeSpan.FromSeconds(30) };

    public FoAblakViewModel(
        ApiKliens api,
        Munkamenet munkamenet,
        BejelentkezesViewModel bejelentkezes,
        Func<LeltarozasViewModel> leltarozas,
        Func<EszkozokViewModel> eszkozok,
        Func<ImportViewModel> import)
    {
        _api = api;
        _leltarozas = leltarozas;
        _eszkozok = eszkozok;
        _import = import;
        Munkamenet = munkamenet;
        Bejelentkezes = bejelentkezes;

        Bejelentkezes.Belepett += (_, valasz) => Belepes(valasz);
        var dispatcher = Dispatcher.CurrentDispatcher;
        _api.MunkamenetLejart += (_, _) => dispatcher.InvokeAsync(() =>
        {
            if (Munkamenet.Bejelentkezve)
                Kilepes("A bejelentkezés lejárt. Jelentkezz be újra.");
        });

        _allapotFigyelo.Tick += async (_, _) => await AllapotEllenorzesAsync();
        _allapotFigyelo.Start();
        _ = AllapotEllenorzesAsync();
    }

    public Munkamenet Munkamenet { get; }

    public BejelentkezesViewModel Bejelentkezes { get; }

    public ObservableCollection<MenuPont> Menu { get; } = [];

    [ObservableProperty]
    private MenuPont? _kivalasztottMenu;

    [ObservableProperty]
    private OldalViewModel? _aktualisOldal;

    /// <summary><c>null</c>: még nem ellenőriztük.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SzerverAllapotSzoveg))]
    private bool? _szerverElerheto;

    public string SzerverAllapotSzoveg => SzerverElerheto switch
    {
        true => "Online",
        false => "A szerver nem érhető el",
        null => "Kapcsolódás…",
    };

    partial void OnKivalasztottMenuChanged(MenuPont? value)
    {
        AktualisOldal = value?.Oldal;
        if (value is not null)
            _ = value.Oldal.MegjelenitesAsync();
    }

    private void Belepes(BejelentkezesValasz valasz)
    {
        Munkamenet.Belepes(valasz);

        Menu.Clear();
        Menu.Add(new MenuPont("Áttekintés", new KesobbViewModel("Áttekintés", "9. alkalom, az összehasonlítással együtt",
            "A nyitott leltáridőszak állása egy pillantással.",
            ["Előrehaladás körzetenként (beolvasott / elvárt tétel)", "A saját mai beolvasások száma",
             "Az utolsó eltérések: ismeretlen kód, más körzet, többlet"]), Kesobb: true));
        var leltarozas = new MenuPont("Leltározás", _leltarozas());
        Menu.Add(leltarozas);
        var eszkozok = new MenuPont("Eszközök", _eszkozok());
        Menu.Add(eszkozok);
        Menu.Add(new MenuPont("Összehasonlítás", new KesobbViewModel("Összehasonlítás", "9. alkalom",
            "A leltár eredményének összevetése a nyitáskori elvárt állománnyal.",
            ["Hiányzó, többlet és más körzetben talált eszközök", "Szűrés időszakra, körzetre és eltéréstípusra",
             "Eltérések exportja Excelbe"]), Kesobb: true));
        Menu.Add(new MenuPont("Import", _import()));
        Menu.Add(new MenuPont("Riportok", new KesobbViewModel("Riportok", "11. alkalom",
            "Előre összeállított kimutatások Excelbe.",
            ["Hiányzó eszközök körzetenként", "Mennyiségi eltérések", "Más körzetből előkerült eszközök",
             "Helyiségenkénti leltárlista", "Felelős személy szerinti lista", "Eszközállapotok és változástörténet"]),
            Kesobb: true));

        KivalasztottMenu = Munkamenet.Leltarozhat ? leltarozas : eszkozok;
    }

    [RelayCommand]
    private void Kijelentkezes() => Kilepes(null);

    private void Kilepes(string? uzenet)
    {
        KivalasztottMenu = null;
        Menu.Clear();
        Munkamenet.Kilepes();
        Bejelentkezes.Tajekoztatas = uzenet;
    }

    private async Task AllapotEllenorzesAsync()
    {
        try
        {
            var allapot = await _api.AllapotAsync();
            SzerverElerheto = allapot.AdatbazisElerheto;
        }
        catch (ApiHiba)
        {
            SzerverElerheto = false;
        }
    }
}
