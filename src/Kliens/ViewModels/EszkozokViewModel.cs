using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Leltarkezelo.Client.Szolgaltatasok;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Kozos.Torzsadatok;

namespace Leltarkezelo.Client.ViewModels;

/// <summary>Egy választólista eleme; a <c>null</c> azonosító jelenti a „mind” lehetőséget.</summary>
public sealed record Valasztas(long? Id, string Nev);

/// <summary>Az eszközlista egy sora a táblázat számára előkészítve.</summary>
public sealed record EszkozSor(
    long Id, string Kod, string? TovabbiKodok, string Megnevezes, string? Tipus, string Korzet,
    string? Helyiseg, string? Felelos, string Allapot)
{
    public static EszkozSor Ebbol(EszkozListaElem e) => new(
        e.Id,
        e.Kodok.FirstOrDefault() ?? "–",
        e.Kodok.Count > 1 ? string.Join(", ", e.Kodok.Skip(1)) : null,
        e.Megnevezes,
        e.EszkozTipus,
        e.LeltarkorzetKod,
        e.Helyiseg,
        e.Felelos,
        e.Allapot);
}

/// <summary>
/// Eszközlista keresővel, szűrőkkel és lapozással (UC-10, 5. alkalom). A szűrés és a lapozás a szerveren történik
/// (<c>GET /api/eszkozok</c>), a kliens csak az aktuális oldalt tartja a memóriában.
/// </summary>
public sealed partial class EszkozokViewModel : OldalViewModel
{
    private readonly ApiKliens _api;
    private readonly DispatcherTimer _keresesKesleltetes = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private bool _szurokBetoltese;
    private int _listaVerzio;
    private int _reszletVerzio;

    public EszkozokViewModel(ApiKliens api)
    {
        _api = api;
        _keresesKesleltetes.Tick += async (_, _) =>
        {
            _keresesKesleltetes.Stop();
            await ElsoOldalBetolteseAsync();
        };
    }

    public override string Cim => "Eszközök";

    public ObservableCollection<EszkozSor> Eszkozok { get; } = [];
    public ObservableCollection<Valasztas> Korzetek { get; } = [];
    public ObservableCollection<Valasztas> Tipusok { get; } = [];
    public ObservableCollection<Valasztas> Allapotok { get; } = [];
    public ObservableCollection<Valasztas> Helyisegek { get; } = [];
    public ObservableCollection<Valasztas> Felelosok { get; } = [];
    public IReadOnlyList<int> OldalMeretek { get; } = [25, 50, 100, 200];

    [ObservableProperty]
    private string _kereses = "";

    [ObservableProperty]
    private Valasztas? _korzet;

    [ObservableProperty]
    private Valasztas? _tipus;

    [ObservableProperty]
    private Valasztas? _allapot;

    [ObservableProperty]
    private Valasztas? _helyiseg;

    [ObservableProperty]
    private Valasztas? _felelos;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LapozasSzoveg))]
    [NotifyCanExecuteChangedFor(nameof(ElozoOldalCommand), nameof(KovetkezoOldalCommand), nameof(ElsoOldalCommand), nameof(UtolsoOldalCommand))]
    private int _oldal = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LapozasSzoveg))]
    [NotifyCanExecuteChangedFor(nameof(ElozoOldalCommand), nameof(KovetkezoOldalCommand), nameof(ElsoOldalCommand), nameof(UtolsoOldalCommand))]
    private int _oldalakSzama;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LapozasSzoveg))]
    private int _osszesen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LapozasSzoveg))]
    private int _oldalMeret = 50;

    [ObservableProperty]
    private EszkozSor? _kivalasztott;

    [ObservableProperty]
    private EszkozReszletek? _reszletek;

    public string LapozasSzoveg => Osszesen == 0
        ? "Nincs a szűrésnek megfelelő eszköz"
        : $"{(Oldal - 1) * OldalMeret + 1}–{Math.Min(Oldal * OldalMeret, Osszesen)} · összesen {Osszesen} eszköz · {Oldal}/{OldalakSzama}. oldal";

    protected override async Task BetoltesAsync()
    {
        await FuttatasAsync(SzurokBetolteseAsync);
        await ListaBetolteseAsync();
    }

    private async Task SzurokBetolteseAsync()
    {
        var korzetek = _api.LeltarkorzetekAsync();
        var tipusok = _api.EszkozTipusokAsync();
        var allapotok = _api.EszkozAllapotokAsync();
        var helyisegek = _api.HelyisegekAsync();
        var felelosok = _api.FelelosokAsync();

        _szurokBetoltese = true;
        try
        {
            Feltoltes(Korzetek, "Minden körzet", (await korzetek).Select(k => new Valasztas(k.Id, $"{k.Kod} – {k.Nev}")));
            Feltoltes(Tipusok, "Minden típus", TipusFa(await tipusok));
            Feltoltes(Allapotok, "Minden állapot", (await allapotok).Select(a => new Valasztas(a.Id, a.Nev)));
            Feltoltes(Helyisegek, "Minden helyiség", (await helyisegek).Select(h => new Valasztas(h.Id, HelyisegNev(h))));
            Feltoltes(Felelosok, "Minden felelős", (await felelosok).Select(f => new Valasztas(f.Id, f.Nev)));
            Korzet = Korzetek[0];
            Tipus = Tipusok[0];
            Allapot = Allapotok[0];
            Helyiseg = Helyisegek[0];
            Felelos = Felelosok[0];
        }
        finally
        {
            _szurokBetoltese = false;
        }
    }

    internal static string HelyisegNev(HelyisegAdat h) =>
        $"{h.Epulet} {h.Szobaszam}" + (string.IsNullOrWhiteSpace(h.Megnevezes) ? "" : $" – {h.Megnevezes}");

    /// <summary>A típusok fába rendezve: a főtípus alatt behúzva az altípusai (a szűrés az altípusokra is kiterjed).</summary>
    private static IEnumerable<Valasztas> TipusFa(IReadOnlyList<EszkozTipusAdat> tipusok)
    {
        IEnumerable<Valasztas> Agak(long? szuloId, int szint) => tipusok
            .Where(t => t.SzuloId == szuloId)
            .OrderBy(t => t.Nev)
            .SelectMany(t => Agak(t.Id, szint + 1).Prepend(new Valasztas(t.Id, new string(' ', szint * 4) + t.Nev)));

        var gyokerek = tipusok.Where(t => t.SzuloId is null || tipusok.All(s => s.Id != t.SzuloId)).Select(t => t.SzuloId).Distinct();
        return gyokerek.SelectMany(g => Agak(g, 0));
    }

    private static void Feltoltes(ObservableCollection<Valasztas> lista, string mind, IEnumerable<Valasztas> elemek)
    {
        lista.Clear();
        lista.Add(new Valasztas(null, mind));
        foreach (var elem in elemek)
            lista.Add(elem);
    }

    partial void OnKeresesChanged(string value)
    {
        _keresesKesleltetes.Stop();
        _keresesKesleltetes.Start();
    }

    partial void OnKorzetChanged(Valasztas? value) => SzuroValtozott();
    partial void OnTipusChanged(Valasztas? value) => SzuroValtozott();
    partial void OnAllapotChanged(Valasztas? value) => SzuroValtozott();
    partial void OnHelyisegChanged(Valasztas? value) => SzuroValtozott();
    partial void OnFelelosChanged(Valasztas? value) => SzuroValtozott();
    partial void OnOldalMeretChanged(int value) => SzuroValtozott();

    private void SzuroValtozott()
    {
        if (!_szurokBetoltese)
            _ = ElsoOldalBetolteseAsync();
    }

    partial void OnKivalasztottChanged(EszkozSor? value) => _ = ReszletekBetolteseAsync(value);

    [RelayCommand]
    private Task KeresesAsync()
    {
        _keresesKesleltetes.Stop();
        return ElsoOldalBetolteseAsync();
    }

    [RelayCommand]
    private void SzurokTorlese()
    {
        _szurokBetoltese = true;
        Kereses = "";
        Korzet = Korzetek.FirstOrDefault();
        Tipus = Tipusok.FirstOrDefault();
        Allapot = Allapotok.FirstOrDefault();
        Helyiseg = Helyisegek.FirstOrDefault();
        Felelos = Felelosok.FirstOrDefault();
        _szurokBetoltese = false;
        _keresesKesleltetes.Stop();
        _ = ElsoOldalBetolteseAsync();
    }

    [RelayCommand]
    private Task FrissitesAsync() => ListaBetolteseAsync();

    [RelayCommand]
    private void ReszletekBezarasa() => Kivalasztott = null;

    private bool VanElozo() => Oldal > 1;
    private bool VanKovetkezo() => Oldal < OldalakSzama;

    [RelayCommand(CanExecute = nameof(VanElozo))]
    private Task ElsoOldalAsync() => OldalraAsync(1);

    [RelayCommand(CanExecute = nameof(VanElozo))]
    private Task ElozoOldalAsync() => OldalraAsync(Oldal - 1);

    [RelayCommand(CanExecute = nameof(VanKovetkezo))]
    private Task KovetkezoOldalAsync() => OldalraAsync(Oldal + 1);

    [RelayCommand(CanExecute = nameof(VanKovetkezo))]
    private Task UtolsoOldalAsync() => OldalraAsync(OldalakSzama);

    private Task ElsoOldalBetolteseAsync() => OldalraAsync(1);

    private Task OldalraAsync(int oldal)
    {
        Oldal = Math.Max(1, oldal);
        return ListaBetolteseAsync();
    }

    private async Task ListaBetolteseAsync()
    {
        var verzio = ++_listaVerzio;
        var szuro = new EszkozSzuro
        {
            Kereses = string.IsNullOrWhiteSpace(Kereses) ? null : Kereses.Trim(),
            LeltarkorzetId = Korzet?.Id,
            EszkozTipusId = Tipus?.Id,
            EszkozAllapotId = Allapot?.Id,
            HelyisegId = Helyiseg?.Id,
            FelelosId = Felelos?.Id,
            Oldal = Oldal,
            OldalMeret = OldalMeret,
        };

        await FuttatasAsync(async () =>
        {
            var lista = await _api.EszkozokAsync(szuro);
            if (verzio != _listaVerzio)
                return;

            Eszkozok.Clear();
            foreach (var elem in lista.Elemek)
                Eszkozok.Add(EszkozSor.Ebbol(elem));
            Osszesen = lista.Osszesen;
            OldalakSzama = lista.OldalakSzama;
            Oldal = lista.Oldal;
        });
    }

    private async Task ReszletekBetolteseAsync(EszkozSor? sor)
    {
        var verzio = ++_reszletVerzio;
        Reszletek = null;
        if (sor is null)
            return;

        try
        {
            var reszletek = await _api.EszkozAsync(sor.Id);
            if (verzio == _reszletVerzio)
                Reszletek = reszletek;
        }
        catch (ApiHiba ex)
        {
            Hiba = ex.Message;
        }
    }
}
