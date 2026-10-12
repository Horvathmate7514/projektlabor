using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Leltarkezelo.Client.Szolgaltatasok;
using LeltarKezelo.Kozos.Import;
using LeltarKezelo.Kozos.Leltar;

namespace Leltarkezelo.Client.ViewModels;

/// <summary>A próbafuttatás egy sora a táblázat számára.</summary>
public sealed record ImportSor(int Sor, string? SapSzam, string? Megnevezes, string Allapot, Jelzes Jelzes, string Reszletek, bool Eltero)
{
    public static ImportSor Ebbol(ImportSorOsszegzes s)
    {
        var reszletek = s.Valtozasok
            .Select(v => $"{ImportMezok.Nev(v.Mezo)}: {v.Regi ?? "–"} → {v.Uj ?? "–"}")
            .Concat(s.Uzenetek.Select(u => (u.Sulyossag == UzenetSulyossag.Hiba ? "Hiba: " : "Figyelmeztetés: ") + u.Szoveg));
        var (allapot, jelzes) = s.Allapot switch
        {
            ImportSorAllapot.Uj => ("Új", Jelzes.Rendben),
            ImportSorAllapot.Modositott => ("Módosított", Jelzes.Info),
            ImportSorAllapot.Valtozatlan => ("Változatlan", Jelzes.Semleges),
            _ => ("Hibás", Jelzes.Ismeretlen),
        };
        if (s.Allapot != ImportSorAllapot.Hibas && s.Uzenetek.Count > 0)
            jelzes = Jelzes.Figyelmeztetes;
        return new ImportSor(s.Sor, s.SapSzam, s.Megnevezes, allapot, jelzes, string.Join(Environment.NewLine, reszletek),
            s.Allapot is ImportSorAllapot.Modositott or ImportSorAllapot.Hibas || s.Uzenetek.Count > 0);
    }
}

/// <summary>Egy korábbi importfutás a naplóban.</summary>
public sealed record ImportFutasSor(long Id, string Ido, string Fajlnev, string Felhasznalo, string Eredmeny, string Statusz, Jelzes Jelzes, int Hibas)
{
    public static ImportFutasSor Ebbol(ImportFutasListaElem f) => new(
        f.Id,
        Minositesek.HelyiIdo(f.Idopont).ToString("yyyy.MM.dd. HH:mm"),
        f.Fajlnev,
        f.Felhasznalo,
        $"{f.SorokSzama} sor · {f.Sikeres} sikeres · {f.Hibas} hibás",
        StatuszNev(f.Statusz),
        f.Statusz switch
        {
            ImportStatusz.Sikeres => Jelzes.Rendben,
            ImportStatusz.Reszleges => Jelzes.Figyelmeztetes,
            ImportStatusz.Sikertelen => Jelzes.Ismeretlen,
            _ => Jelzes.Semleges,
        },
        f.Hibas);

    public static string StatuszNev(ImportStatusz statusz) => statusz switch
    {
        ImportStatusz.Sikeres => "Sikeres",
        ImportStatusz.Reszleges => "Részleges",
        ImportStatusz.Sikertelen => "Sikertelen",
        ImportStatusz.Folyamatban => "Folyamatban",
        _ => statusz.ToString(),
    };
}

/// <summary>
/// Excel forrásadatok importálása (UC-01, 5. alkalom): fájl kiválasztása, próbafuttatás (semmit nem ír), az eredmény
/// soronként, jóváhagyás után a végrehajtás, végül az importnapló és a hibák letöltése Excelben (D-031).
/// </summary>
public sealed partial class ImportViewModel(ApiKliens api, Munkamenet munkamenet, Parbeszedek parbeszedek) : OldalViewModel
{
    public override string Cim => "Import";

    public bool Engedelyezett => munkamenet.Importalhat;

    public ObservableCollection<ImportSor> Sorok { get; } = [];
    public ObservableCollection<ImportFutasSor> Futasok { get; } = [];
    public ObservableCollection<ImportHibaAdat> FutasHibai { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProbafuttatasCommand))]
    private string? _fajlUt;

    [ObservableProperty]
    private string _munkalap = "";

    [ObservableProperty]
    private bool _frissitesek = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VanElemzes), nameof(FejlecInfo), nameof(NemSzereplokInfo))]
    [NotifyCanExecuteChangedFor(nameof(VegrehajtasCommand))]
    private ImportElemzes? _elemzes;

    [ObservableProperty]
    private bool _csakElteresek = true;

    [ObservableProperty]
    private string? _eredmeny;

    [ObservableProperty]
    private Jelzes _eredmenyJelzes;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HibakLetolteseCommand))]
    private ImportFutasSor? _kivalasztottFutas;

    /// <summary>A beírt vagy beillesztett út idézőjelek nélkül (a Windows „Elérési út másolása” idézőjelbe teszi).</summary>
    private string Fajl => FajlUt!.Trim().Trim('"');

    public bool VanElemzes => Elemzes is not null;

    public string? FejlecInfo => Elemzes is not { } e ? null
        : $"Munkalap: {e.Munkalap} · fejléc a(z) {e.FejlecSor}. sorban · felismert oszlopok: {string.Join(", ", e.Oszlopok.Select(o => o.Fejlec))}"
          + (e.IsmeretlenOszlopok.Count > 0 ? $" · nem importált oszlopok: {string.Join(", ", e.IsmeretlenOszlopok)}" : "");

    public string? NemSzereplokInfo => Elemzes is not { ForrasbanNemSzereploDarab: > 0 } e ? null
        : $"{e.ForrasbanNemSzereploDarab} nyilvántartott eszköz nem szerepel a fájlban. Ezeket az import nem törli (F-03); " +
          $"például: {string.Join(", ", e.ForrasbanNemSzereplok.Take(5).Select(n => $"{n.SapSzam} {n.Megnevezes}"))}.";

    protected override Task BetoltesAsync() => Engedelyezett ? NaploBetolteseAsync() : Task.CompletedTask;

    partial void OnFajlUtChanged(string? value) => Elemzes = null;
    partial void OnMunkalapChanged(string value) => Elemzes = null;
    partial void OnCsakElteresekChanged(bool value) => SorokFrissitese();
    partial void OnElemzesChanged(ImportElemzes? value) => SorokFrissitese();
    partial void OnKivalasztottFutasChanged(ImportFutasSor? value) => _ = FutasHibaiBetolteseAsync(value);

    [RelayCommand]
    private void Tallozas()
    {
        if (parbeszedek.ExcelMegnyitasa() is { } ut)
        {
            FajlUt = ut;
            Eredmeny = null;
        }
    }

    private bool VanFajl() => !string.IsNullOrWhiteSpace(FajlUt) && File.Exists(Fajl);

    [RelayCommand(CanExecute = nameof(VanFajl))]
    private async Task ProbafuttatasAsync()
    {
        Eredmeny = null;
        await FuttatasAsync(async () => Elemzes = await api.ImportProbafuttatasAsync(Fajl, Beallitasok()));
    }

    private bool Vegrehajthato() => Elemzes is not null;

    [RelayCommand(CanExecute = nameof(Vegrehajthato))]
    private async Task VegrehajtasAsync()
    {
        var o = Elemzes!.Osszesites;
        var modositasok = o.Modositott == 0 ? ""
            : Frissitesek ? $"{o.Modositott} meglévő eszköz frissül. " : $"{o.Modositott} meglévő eszköz módosítása kimarad (a frissítés ki van kapcsolva). ";
        var kerdes = $"{o.Uj} új eszköz kerül be. {modositasok}{(o.Hibas > 0 ? $"{o.Hibas} hibás sor kimarad. " : "")}" +
                     "\n\nA szerver a végrehajtás előtt újra ellenőrzi a fájlt. Folytatod?";
        if (!parbeszedek.Megerosites("Import végrehajtása", kerdes))
            return;

        await FuttatasAsync(async () =>
        {
            var e = await api.ImportVegrehajtasAsync(Fajl, Beallitasok());
            Elemzes = null;
            EredmenyJelzes = e.Statusz switch
            {
                ImportStatusz.Sikeres => Jelzes.Rendben,
                ImportStatusz.Reszleges => Jelzes.Figyelmeztetes,
                _ => Jelzes.Ismeretlen,
            };
            Eredmeny = $"Import kész ({ImportFutasSor.StatuszNev(e.Statusz)}): {e.Uj} új, {e.Frissitett} frissített, " +
                       $"{e.Valtozatlan} változatlan, {e.Kihagyott} kihagyott, {e.Hibas} hibás sor" +
                       (e.UjFelelosok > 0 ? $"; {e.UjFelelosok} új felelős személy." : ".") +
                       (e.Hibas > 0 ? " A hibás sorok listája lent, az importnaplóban látható és letölthető." : "");
            await NaploBetolteseAsync();
            KivalasztottFutas = Futasok.FirstOrDefault(f => f.Id == e.ImportFutasId);
        });
    }

    [RelayCommand]
    private Task NaploFrissiteseAsync() => NaploBetolteseAsync();

    private bool VanLetoltheto() => KivalasztottFutas is { Hibas: > 0 };

    [RelayCommand(CanExecute = nameof(VanLetoltheto))]
    private async Task HibakLetolteseAsync()
    {
        var futas = KivalasztottFutas!;
        if (parbeszedek.ExcelMentese($"importhibak_{futas.Id}.xlsx") is not { } cel)
            return;

        await FuttatasAsync(async () =>
        {
            var tartalom = await api.ImportHibakExcelAsync(futas.Id);
            try
            {
                await File.WriteAllBytesAsync(cel, tartalom);
            }
            catch (IOException ex)
            {
                throw new ApiHiba(null, $"A fájl nem menthető ({ex.Message}). Lehet, hogy nyitva van Excelben.");
            }
            catch (UnauthorizedAccessException)
            {
                throw new ApiHiba(null, "Ebbe a mappába nincs írási jogod; válassz másikat.");
            }
        });
    }

    private ImportBeallitasok Beallitasok() => new()
    {
        Munkalap = string.IsNullOrWhiteSpace(Munkalap) ? null : Munkalap.Trim(),
        FrissitesekAlkalmazasa = Frissitesek,
    };

    private void SorokFrissitese()
    {
        Sorok.Clear();
        if (Elemzes is null)
            return;
        foreach (var sor in Elemzes.Sorok.Select(ImportSor.Ebbol).Where(s => !CsakElteresek || s.Eltero))
            Sorok.Add(sor);
    }

    private async Task NaploBetolteseAsync()
    {
        try
        {
            var lista = await api.ImportNaploAsync(1, 20);
            Futasok.Clear();
            foreach (var f in lista.Elemek)
                Futasok.Add(ImportFutasSor.Ebbol(f));
        }
        catch (ApiHiba ex)
        {
            Hiba = ex.Message;
        }
    }

    private async Task FutasHibaiBetolteseAsync(ImportFutasSor? futas)
    {
        FutasHibai.Clear();
        if (futas is not { Hibas: > 0 })
            return;
        try
        {
            var reszletek = await api.ImportFutasAsync(futas.Id);
            if (futas != KivalasztottFutas)
                return;
            foreach (var h in reszletek.Hibak)
                FutasHibai.Add(h);
        }
        catch (ApiHiba ex)
        {
            Hiba = ex.Message;
        }
    }
}
