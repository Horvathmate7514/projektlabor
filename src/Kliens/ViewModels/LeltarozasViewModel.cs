using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Leltarkezelo.Client.Szolgaltatasok;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Kozos.Torzsadatok;

namespace Leltarkezelo.Client.ViewModels;

/// <summary>Egy saját leolvasás a „legutóbbi beolvasások” listában.</summary>
public sealed record LeolvasasSor(
    long Id, string Ido, string Kod, string Eszkoz, Minosites Minosites, string MinositesNev, Jelzes Jelzes,
    int Mennyiseg, string Mod, string Korzet, bool Sztornozva)
{
    public static LeolvasasSor Ebbol(LeolvasasListaElem l) => new(
        l.Id,
        Minositesek.HelyiIdo(l.Idopont).ToString("HH:mm:ss", CultureInfo.CurrentCulture),
        l.BeolvasottKod,
        l.EszkozMegnevezes ?? "–",
        l.Minosites,
        l.Sztornozva ? "Sztornózva" : Minositesek.Nev(l.Minosites),
        l.Sztornozva ? Jelzes.Semleges : Minositesek.Jelzes(l.Minosites),
        l.Mennyiseg,
        Minositesek.Nev(l.BeviteliMod),
        l.AktivKorzetKod,
        l.Sztornozva);
}

/// <summary>
/// A leltározó képernyő (UC-06, 6. alkalom): időszak- és körzetválasztás, vonalkód beolvasása olvasóval,
/// beillesztéssel vagy demókóddal, azonnali színes visszajelzés, a saját legutóbbi beolvasások és a sztornó.
/// Minden beolvasás a <see cref="BeolvasasAsync"/> közös belépési ponton megy át; a későbbi kamerás beolvasás
/// (D-009) is ezt hívja majd <see cref="BeviteliMod.Kamera"/> móddal.
/// </summary>
public sealed partial class LeltarozasViewModel(ApiKliens api, Munkamenet munkamenet, Parbeszedek parbeszedek) : OldalViewModel
{
    private const int UtolsoBeolvasasokSzama = 20;
    private readonly SemaphoreSlim _sorrend = new(1, 1);

    public override string Cim => "Leltározás";

    public bool Engedelyezett => munkamenet.Leltarozhat;

    public ObservableCollection<LeltarIdoszakAdat> Idoszakok { get; } = [];
    public ObservableCollection<LeltarkorzetAdat> Korzetek { get; } = [];
    public ObservableCollection<Valasztas> Helyisegek { get; } = [];
    public ObservableCollection<LeolvasasSor> UtolsoBeolvasasok { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KeszenAll), nameof(Allapotsor))]
    [NotifyCanExecuteChangedFor(nameof(RogzitesCommand), nameof(DemoErvenyesCommand), nameof(DemoErvenytelenCommand))]
    private LeltarIdoszakAdat? _idoszak;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KeszenAll), nameof(Allapotsor))]
    [NotifyCanExecuteChangedFor(nameof(RogzitesCommand), nameof(DemoErvenyesCommand), nameof(DemoErvenytelenCommand))]
    private LeltarkorzetAdat? _korzet;

    [ObservableProperty]
    private Valasztas? _helyiseg;

    [ObservableProperty]
    private string _kod = "";

    [ObservableProperty]
    private int _mennyiseg = 1;

    [ObservableProperty]
    private Jelzes _jelzes = Jelzes.Semleges;

    [ObservableProperty]
    private string _visszajelzes = "Válaszd ki az időszakot és a körzetet, majd olvasd be az első kódot.";

    [ObservableProperty]
    private string? _visszajelzesReszlet;

    [ObservableProperty]
    private string? _figyelmeztetes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Statisztika))]
    private int _beolvasasok;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Statisztika))]
    private int _rendben;

    /// <summary>A beolvasó mezőbe Ctrl+V-vel került a kód (a nézet állítja be); a beolvasás <c>Beillesztes</c> lesz.</summary>
    public bool Beillesztve { get; set; }

    public bool KeszenAll => Idoszak is not null && Korzet is { Leltarozhato: true };

    public string Allapotsor => KeszenAll
        ? $"{Idoszak!.Megnevezes} · {Korzet!.Kod} – {Korzet.Nev}"
        : "Nincs kiválasztott időszak vagy körzet";

    public string Statisztika => Beolvasasok == 0
        ? "Ebben a munkamenetben még nem volt beolvasás."
        : $"Ebben a munkamenetben {Beolvasasok} beolvasás, ebből {Rendben} rendben, {Beolvasasok - Rendben} eltéréssel.";

    /// <summary>A nézet erre visszaviszi a fókuszt a beolvasó mezőbe (az olvasó billentyűzetként ír, F-09).</summary>
    public event EventHandler? FokuszKell;

    protected override async Task BetoltesAsync()
    {
        if (!Engedelyezett)
        {
            Figyelmeztetes = "A leltározáshoz Leltározó, Leltárfelelős vagy Admin szerepkör kell. Az eszközöket az Eszközök oldalon nézheted meg.";
            return;
        }

        await FuttatasAsync(async () =>
        {
            var idoszakok = api.LeltarIdoszakokAsync(LeltarIdoszakStatusz.Nyitott);
            var korzetek = api.LeltarkorzetekAsync();
            var helyisegek = api.HelyisegekAsync();

            Idoszakok.Clear();
            foreach (var i in await idoszakok)
                Idoszakok.Add(i);
            Korzetek.Clear();
            foreach (var k in (await korzetek).OrderByDescending(k => k.Leltarozhato).ThenBy(k => k.Kod))
                Korzetek.Add(k);
            Helyisegek.Clear();
            Helyisegek.Add(new Valasztas(null, "– nincs megadva –"));
            foreach (var h in await helyisegek)
                Helyisegek.Add(new Valasztas(h.Id, EszkozokViewModel.HelyisegNev(h)));

            Helyiseg = Helyisegek[0];
            Idoszak = Idoszakok.FirstOrDefault();
            Korzet = Korzetek.FirstOrDefault(k => k.Leltarozhato);

            Figyelmeztetes = (Idoszak, Korzet) switch
            {
                (null, _) => "Nincs nyitott leltáridőszak, ezért most nem lehet beolvasni. A leltárfelelős nyithat egyet.",
                (_, null) => "Nincs olyan leltárkörzet, amelyben leltározhatsz. A leltárfelelős rendelhet hozzád körzetet.",
                _ => null,
            };
        });
        FokuszKell?.Invoke(this, EventArgs.Empty);
    }

    partial void OnIdoszakChanged(LeltarIdoszakAdat? value) => _ = UtolsoBeolvasasokFrissiteseAsync();

    [RelayCommand]
    private async Task FrissitesAsync()
    {
        await UtolsoBeolvasasokFrissiteseAsync();
        FokuszKell?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(KeszenAll))]
    private Task RogzitesAsync()
    {
        var mod = Beillesztve ? BeviteliMod.Beillesztes : BeviteliMod.Olvaso;
        Beillesztve = false;
        var kod = Kod;
        Kod = "";
        return BeolvasasAsync(kod, mod);
    }

    [RelayCommand(CanExecute = nameof(KeszenAll))]
    private Task DemoErvenyesAsync() => DemoAsync(ervenyes: true);

    [RelayCommand(CanExecute = nameof(KeszenAll))]
    private Task DemoErvenytelenAsync() => DemoAsync(ervenyes: false);

    private async Task DemoAsync(bool ervenyes)
    {
        try
        {
            var demo = await api.DemoKodAsync(Idoszak!.Id, Korzet!.Id, ervenyes);
            await BeolvasasAsync(demo.Kod, BeviteliMod.Demo);
        }
        catch (ApiHiba ex)
        {
            Jelez(Jelzes.Hiba, $"Nem sikerült demókódot kérni: {ex.Message}", null);
        }
    }

    /// <summary>Egy kód beküldése. A beolvasások sorban mennek a szerverre, így a gyors olvasó sem keveri össze őket.</summary>
    public async Task BeolvasasAsync(string kod, BeviteliMod mod)
    {
        kod = kod.Trim();
        if (kod.Length == 0 || !KeszenAll)
            return;
        if (kod.Length > BeolvasasKeres.MaxKodHossz)
        {
            Jelez(Jelzes.Hiba, $"A kód túl hosszú (legfeljebb {BeolvasasKeres.MaxKodHossz} karakter), nem rögzült.", kod);
            return;
        }

        var mennyiseg = Math.Clamp(Mennyiseg, 1, BeolvasasKeres.MaxMennyiseg);
        var keres = new BeolvasasKeres(kod, Korzet!.Id, mod, mennyiseg, Helyiseg?.Id);
        var idoszakId = Idoszak!.Id;

        await _sorrend.WaitAsync();
        try
        {
            var eredmeny = await api.BeolvasasAsync(idoszakId, keres);
            Mennyiseg = 1;
            Beolvasasok++;
            if (eredmeny.Minosites == Minosites.Ok)
                Rendben++;
            Jelez(Minositesek.Jelzes(eredmeny.Minosites), eredmeny.Uzenet, Reszlet(eredmeny, kod, mod));
            await UtolsoBeolvasasokFrissiteseAsync();
        }
        catch (ApiHiba ex)
        {
            Jelez(Jelzes.Hiba, $"Nem rögzült: {ex.Message}", $"Kód: {kod}");
        }
        finally
        {
            _sorrend.Release();
            FokuszKell?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string Reszlet(BeolvasasEredmeny e, string kod, BeviteliMod mod)
    {
        var reszek = new List<string> { $"Kód: {kod} ({Minositesek.Nev(mod)})" };
        if (e.Eszkoz is { } eszkoz)
            reszek.Add($"azonosítva: {eszkoz.KodTipus} · {eszkoz.LeltarkorzetKod} · {eszkoz.Allapot}");
        if (e.Minosites == Minosites.Ismetelt && e.KorabbiBeolvasas is { } korabbi)
        {
            var helyi = Minositesek.HelyiIdo(korabbi);
            reszek.Add(helyi.Date == DateTime.Today ? $"már beolvasva: ma {helyi:HH:mm}" : $"már beolvasva: {helyi:yyyy.MM.dd. HH:mm}");
        }
        if (e.ElvartMennyiseg is { } elvart && (elvart > 1 || e.BeolvasottMennyiseg > elvart))
        {
            var egyseg = e.Eszkoz?.MennyisegiEgyseg ?? "db";
            var kulonbseg = elvart - e.BeolvasottMennyiseg;
            reszek.Add($"elvárt {elvart} {egyseg} · beolvasva {e.BeolvasottMennyiseg} {egyseg} · " +
                       (kulonbseg >= 0 ? $"hiányzik {kulonbseg} {egyseg}" : $"többlet {-kulonbseg} {egyseg}"));
        }
        return string.Join(" · ", reszek);
    }

    private void Jelez(Jelzes jelzes, string uzenet, string? reszlet)
    {
        Jelzes = jelzes;
        Visszajelzes = uzenet;
        VisszajelzesReszlet = reszlet;
    }

    [RelayCommand]
    private async Task SztornoAsync(LeolvasasSor? sor)
    {
        if (sor is null || sor.Sztornozva)
            return;

        var indoklas = parbeszedek.SzovegBekerese(
            "Leolvasás sztornózása",
            $"Miért vonod vissza ezt a leolvasást?\n{sor.Ido} · {sor.Kod} · {sor.Eszkoz}",
            SztornoKeres.MaxHossz);
        if (string.IsNullOrWhiteSpace(indoklas))
        {
            FokuszKell?.Invoke(this, EventArgs.Empty);
            return;
        }

        try
        {
            await api.SztornoAsync(sor.Id, indoklas.Trim());
            var eszkoz = sor.Eszkoz == "–" ? "" : $" ({sor.Eszkoz})";
            Jelez(Jelzes.Semleges, $"Sztornózva: {sor.Kod}{eszkoz}.", $"Indoklás: {indoklas.Trim()}");
            await UtolsoBeolvasasokFrissiteseAsync();
        }
        catch (ApiHiba ex)
        {
            Jelez(Jelzes.Hiba, $"A sztornó nem sikerült: {ex.Message}", null);
        }
        FokuszKell?.Invoke(this, EventArgs.Empty);
    }

    private async Task UtolsoBeolvasasokFrissiteseAsync()
    {
        if (Idoszak is not { } idoszak)
        {
            UtolsoBeolvasasok.Clear();
            return;
        }

        try
        {
            var lista = await api.LeolvasasokAsync(idoszak.Id, new LeolvasasSzuro
            {
                CsakSajat = true,
                SztornozottakkalEgyutt = true,
                OldalMeret = UtolsoBeolvasasokSzama,
            });
            if (idoszak != Idoszak)
                return;
            UtolsoBeolvasasok.Clear();
            foreach (var l in lista.Elemek)
                UtolsoBeolvasasok.Add(LeolvasasSor.Ebbol(l));
        }
        catch (ApiHiba ex)
        {
            Hiba = ex.Message;
        }
    }
}
