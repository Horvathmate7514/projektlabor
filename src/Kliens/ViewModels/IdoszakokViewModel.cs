using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Leltarkezelo.Client.Szolgaltatasok;
using LeltarKezelo.Kozos.Leltar;

namespace Leltarkezelo.Client.ViewModels;

public sealed record IdoszakStatuszValasztas(LeltarIdoszakStatusz? Ertek, string Nev);
public sealed record IdoszakTipusValasztas(LeltarIdoszakTipus Ertek, string Nev);

public sealed record IdoszakSor(LeltarIdoszakAdat Adat)
{
    public long Id => Adat.Id;
    public string Megnevezes => Adat.Megnevezes;
    public string Tipus => Adat.Tipus == LeltarIdoszakTipus.Eves ? "Éves" : "Rendkívüli";
    public string Statusz => Adat.Statusz switch
    {
        LeltarIdoszakStatusz.Tervezett => "Tervezett",
        LeltarIdoszakStatusz.Nyitott => "Nyitott",
        _ => "Lezárt",
    };
    public string Kezdete => Adat.Kezdete.ToString("yyyy. MM. dd.");
    public string Vege => Adat.Vege?.ToString("yyyy. MM. dd.") ?? "–";
    public int ElvartTetelek => Adat.ElvartTetelekSzama;
    public string Lezarva => Adat.Lezarva is { } ido ? Minositesek.HelyiIdo(ido).ToString("yyyy. MM. dd. HH:mm") : "–";
}

/// <summary>A meglévő időszak-API kezelőfelülete; az állapotváltás üzleti logikája a szerveren marad.</summary>
public sealed partial class IdoszakokViewModel : OldalViewModel
{
    private readonly ApiKliens _api;
    private readonly Munkamenet _munkamenet;
    private readonly Parbeszedek _parbeszedek;
    private int _listaVerzio;
    private bool _szuroValtozasLetiltva;

    public IdoszakokViewModel(ApiKliens api, Munkamenet munkamenet, Parbeszedek parbeszedek)
    {
        _api = api;
        _munkamenet = munkamenet;
        _parbeszedek = parbeszedek;
        _szuroValtozasLetiltva = true;
        Statusz = Statuszok[0];
        Tipus = Tipusok[0];
        _szuroValtozasLetiltva = false;
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Foglalt)) MuveletekFrissitese();
        };
    }

    public override string Cim => "Leltáridőszakok";
    public bool Kezelhet => _munkamenet.Importalhat;
    public ObservableCollection<IdoszakSor> Idoszakok { get; } = [];
    public IReadOnlyList<IdoszakStatuszValasztas> Statuszok { get; } =
    [new(null, "Minden állapot"), new(LeltarIdoszakStatusz.Tervezett, "Tervezett"),
     new(LeltarIdoszakStatusz.Nyitott, "Nyitott"), new(LeltarIdoszakStatusz.Lezart, "Lezárt")];
    public IReadOnlyList<IdoszakTipusValasztas> Tipusok { get; } =
    [new(LeltarIdoszakTipus.Eves, "Éves"), new(LeltarIdoszakTipus.Rendkivuli, "Rendkívüli")];

    [ObservableProperty] private IdoszakStatuszValasztas? _statusz;
    [ObservableProperty] private IdoszakSor? _kivalasztott;
    [ObservableProperty] private string _megnevezes = "";
    [ObservableProperty] private IdoszakTipusValasztas? _tipus;
    [ObservableProperty] private DateTime? _kezdete = DateTime.Today;
    [ObservableProperty] private DateTime? _vege;
    [ObservableProperty] private string? _tajekoztatas;

    public override Task MegjelenitesAsync() => ListaBetolteseAsync();
    partial void OnStatuszChanged(IdoszakStatuszValasztas? value)
    {
        if (!_szuroValtozasLetiltva) _ = ListaBetolteseAsync();
    }
    partial void OnKivalasztottChanged(IdoszakSor? value) => MuveletekFrissitese();

    private void MuveletekFrissitese()
    {
        LetrehozasCommand.NotifyCanExecuteChanged();
        NyitasCommand.NotifyCanExecuteChanged();
        LezarasCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand] private Task FrissitesAsync() => ListaBetolteseAsync();

    private async Task ListaBetolteseAsync(long? kivalasztando = null)
    {
        var verzio = ++_listaVerzio;
        var id = kivalasztando ?? Kivalasztott?.Id;
        await FuttatasAsync(async () =>
        {
            var lista = await _api.LeltarIdoszakokAsync(Statusz?.Ertek);
            if (verzio != _listaVerzio) return;
            Idoszakok.Clear();
            foreach (var adat in lista) Idoszakok.Add(new(adat));
            Kivalasztott = Idoszakok.FirstOrDefault(i => i.Id == id) ?? Idoszakok.FirstOrDefault();
        });
    }

    private bool Letrehozhat() => Kezelhet && !Foglalt;
    private bool Nyithat() => Kezelhet && !Foglalt && Kivalasztott?.Adat.Statusz == LeltarIdoszakStatusz.Tervezett;
    private bool Lezarhat() => Kezelhet && !Foglalt && Kivalasztott?.Adat.Statusz == LeltarIdoszakStatusz.Nyitott;

    [RelayCommand(CanExecute = nameof(Letrehozhat))]
    private async Task LetrehozasAsync()
    {
        if (!Letrehozhat()) return;
        Hiba = null;
        Tajekoztatas = null;
        if (string.IsNullOrWhiteSpace(Megnevezes) || Megnevezes.Trim().Length > 200)
        { Hiba = "Adj meg egy 1–200 karakteres megnevezést."; return; }
        if (Kezdete is null || (Vege is { } veg && veg.Date < Kezdete.Value.Date))
        { Hiba = "A kezdődátum kötelező; a záródátum nem lehet korábbi."; return; }
        LeltarIdoszakAdat? uj = null;
        if (await FuttatasAsync(async () => uj = await _api.LeltarIdoszakLetrehozasaAsync(new(
            Megnevezes.Trim(), Tipus?.Ertek ?? LeltarIdoszakTipus.Eves,
            DateOnly.FromDateTime(Kezdete.Value), Vege is { } v ? DateOnly.FromDateTime(v) : null))))
        {
            Megnevezes = "";
            Tajekoztatas = $"Létrejött: {uj!.Megnevezes}. A leltározáshoz még meg kell nyitni.";
            // Az új rekord akkor is látható legyen, ha korábban más állapotra szűrtünk.
            _szuroValtozasLetiltva = true;
            Statusz = Statuszok[0];
            _szuroValtozasLetiltva = false;
            await ListaBetolteseAsync(uj.Id);
        }
    }

    [RelayCommand(CanExecute = nameof(Nyithat))]
    private async Task NyitasAsync()
    {
        if (!Nyithat() || Kivalasztott is not { } sor) return;
        if (!_parbeszedek.Megerosites("Leltáridőszak megnyitása",
            $"Megnyitod a(z) „{sor.Megnevezes}” időszakot?\n\nA szerver rögzíti az elvárt eszközállomány pillanatképét, és lehetővé válik a leolvasás.")) return;
        Tajekoztatas = null;
        if (await FuttatasAsync(async () => await _api.LeltarIdoszakNyitasaAsync(sor.Id)))
        {
            Tajekoztatas = $"Megnyitva: {sor.Megnevezes}. A Leltározás oldalon kiválasztható.";
            await ListaBetolteseAsync(sor.Id);
        }
        else await KivalasztottFrissiteseAsync(sor.Id);
    }

    [RelayCommand(CanExecute = nameof(Lezarhat))]
    private async Task LezarasAsync()
    {
        if (!Lezarhat() || Kivalasztott is not { } sor) return;
        if (!_parbeszedek.Megerosites("Leltáridőszak lezárása",
            $"Lezárod a(z) „{sor.Megnevezes}” időszakot?\n\nEzután új leolvasás és sztornó nem rögzíthető. A lezárt időszak nem nyitható meg újra.")) return;
        Tajekoztatas = null;
        if (await FuttatasAsync(async () => await _api.LeltarIdoszakLezarasaAsync(sor.Id)))
        {
            Tajekoztatas = $"Lezárva: {sor.Megnevezes}. A leolvasások a Leolvasási naplóban továbbra is megtekinthetők.";
            await ListaBetolteseAsync(sor.Id);
        }
        else await KivalasztottFrissiteseAsync(sor.Id);
    }

    private async Task KivalasztottFrissiteseAsync(long id)
    {
        // Például más felhasználó időközben lezárta; az eredeti hiba maradjon olvasható.
        try
        {
            var friss = new IdoszakSor(await _api.LeltarIdoszakAsync(id));
            var regi = Idoszakok.FirstOrDefault(i => i.Id == id);
            if (regi is not null) Idoszakok[Idoszakok.IndexOf(regi)] = friss;
            Kivalasztott = friss;
        }
        catch (ApiHiba) { }
    }
}
