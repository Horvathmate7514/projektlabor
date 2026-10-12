using System.Text.Json.Serialization;
using LeltarKezelo.Kozos.Leltar;

namespace LeltarKezelo.Kozos.Import;

/// <summary>A forrás Excel egy oszlopa, amelyet az import felismer (03_alkalom/04_Excel_lekepezes.md).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ImportMezo>))]
public enum ImportMezo
{
    SapSzam,
    LeltariSzam1,
    LeltariSzam2,
    GyartasiSzam,
    Megnevezes,
    Mennyiseg,
    MennyisegiEgyseg,
    Ertek,
    BeszerzesDatuma,
    Leltarkorzet,
    EszkozTipus,
    FelelosSzemely,
    Megjegyzes,
}

[JsonConverter(typeof(JsonStringEnumConverter<ImportSorAllapot>))]
public enum ImportSorAllapot
{
    Uj,
    Modositott,
    Valtozatlan,
    Hibas,
}

[JsonConverter(typeof(JsonStringEnumConverter<UzenetSulyossag>))]
public enum UzenetSulyossag
{
    Hiba,
    Figyelmeztetes,
}

public static class ImportMezok
{
    public static readonly IReadOnlyList<ImportMezo> Kotelezo = [ImportMezo.SapSzam, ImportMezo.Megnevezes, ImportMezo.Leltarkorzet];

    public static string Nev(ImportMezo mezo) => mezo switch
    {
        ImportMezo.SapSzam => "SAP szám",
        ImportMezo.LeltariSzam1 => "Leltári szám 1",
        ImportMezo.LeltariSzam2 => "Leltári szám 2",
        ImportMezo.GyartasiSzam => "Gyártási szám",
        ImportMezo.Megnevezes => "Megnevezés",
        ImportMezo.Mennyiseg => "Mennyiség",
        ImportMezo.MennyisegiEgyseg => "Mennyiségi egység",
        ImportMezo.Ertek => "Érték",
        ImportMezo.BeszerzesDatuma => "Beszerzés dátuma",
        ImportMezo.Leltarkorzet => "Leltárkörzet",
        ImportMezo.EszkozTipus => "Eszköztípus",
        ImportMezo.FelelosSzemely => "Felelős személy",
        ImportMezo.Megjegyzes => "Megjegyzés",
        _ => mezo.ToString(),
    };
}

/// <summary>
/// Az import beállításai. Ha az <see cref="Oszlopok"/> üres, az oszlopokat a fejléc alapján automatikusan ismeri fel;
/// egy megadott hozzárendelés felülírja a felismerést (pl. a kliens oszlop-hozzárendelő képernyőjéről).
/// </summary>
public sealed record ImportBeallitasok
{
    public string? Munkalap { get; init; }
    public IReadOnlyDictionary<ImportMezo, string>? Oszlopok { get; init; }

    /// <summary>Hamis esetén a már létező eszközök módosításai nem kerülnek be, csak az új eszközök.</summary>
    public bool FrissitesekAlkalmazasa { get; init; } = true;
}

public sealed record ImportOszlop(ImportMezo Mezo, int Oszlop, string Fejlec);

public sealed record ImportUzenet(int Sor, ImportMezo? Mezo, UzenetSulyossag Sulyossag, string Szoveg, string? Ertek);

public sealed record MezoValtozas(ImportMezo Mezo, string? Regi, string? Uj);

public sealed record ImportSorOsszegzes(
    int Sor,
    string? SapSzam,
    string? Megnevezes,
    ImportSorAllapot Allapot,
    long? EszkozId,
    IReadOnlyList<MezoValtozas> Valtozasok,
    IReadOnlyList<ImportUzenet> Uzenetek);

public sealed record ImportOsszesites(int Sorok, int Uj, int Modositott, int Valtozatlan, int Hibas, int Figyelmeztetesek);

public sealed record ForrasbanNemSzereploEszkoz(long Id, string SapSzam, string Megnevezes);

/// <summary>A próbafuttatás eredménye: az adatbázis nem változik.</summary>
public sealed record ImportElemzes(
    string Fajlnev,
    string Munkalap,
    int FejlecSor,
    IReadOnlyList<ImportOszlop> Oszlopok,
    IReadOnlyList<string> IsmeretlenOszlopok,
    ImportOsszesites Osszesites,
    IReadOnlyList<ImportSorOsszegzes> Sorok,
    int ForrasbanNemSzereploDarab,
    IReadOnlyList<ForrasbanNemSzereploEszkoz> ForrasbanNemSzereplok);

public sealed record ImportEredmeny(
    long ImportFutasId,
    ImportStatusz Statusz,
    int Uj,
    int Frissitett,
    int Valtozatlan,
    int Kihagyott,
    int Hibas,
    int UjFelelosok,
    ImportElemzes Elemzes);
