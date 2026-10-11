namespace LeltarKezelo.Kozos.Leltar;

/// <summary>Egy beolvasott kód a leltározó képernyőről. A minősítést mindig a szerver adja.</summary>
public sealed record BeolvasasKeres(
    string Kod,
    long AktivKorzetId,
    BeviteliMod BeviteliMod = BeviteliMod.Olvaso,
    int Mennyiseg = 1,
    long? HelyisegId = null)
{
    public const int MaxKodHossz = 64;
    public const int MaxMennyiseg = 10_000;
}

/// <summary>
/// A beolvasás eredménye, amelyből a kliens a visszajelzést (szín, szöveg) készíti.
/// A leolvasás minden minősítésnél rögzül, az ismeretlen kód is (F-15).
/// </summary>
public sealed record BeolvasasEredmeny(
    long LeolvasasId,
    Minosites Minosites,
    string Uzenet,
    BeolvasottEszkoz? Eszkoz,
    int? ElvartMennyiseg,
    int BeolvasottMennyiseg,
    DateTime? KorabbiBeolvasas);

public sealed record BeolvasottEszkoz(
    long Id,
    string Megnevezes,
    string KodTipus,
    string LeltarkorzetKod,
    string LeltarkorzetNev,
    string Allapot,
    string? MennyisegiEgyseg);

public sealed record LeolvasasListaElem(
    long Id,
    DateTime Idopont,
    string BeolvasottKod,
    Minosites Minosites,
    long? EszkozId,
    string? EszkozMegnevezes,
    string AktivKorzetKod,
    int Mennyiseg,
    BeviteliMod BeviteliMod,
    string Felhasznalo,
    bool Sztornozva);

public sealed record LeolvasasSzuro
{
    public const int MaxOldalMeret = 200;

    public long? LeltarkorzetId { get; init; }
    public bool CsakSajat { get; init; }
    public bool SztornozottakkalEgyutt { get; init; }
    public int Oldal { get; init; } = 1;
    public int OldalMeret { get; init; } = 50;
}

public sealed record SztornoKeres(string Indoklas)
{
    public const int MaxHossz = 500;
}

/// <summary>Tesztkód a vonalkódolvasó nélküli bemutatóhoz (UC-06 5c).</summary>
public sealed record DemoKod(string Kod, bool Ervenyes, long? EszkozId);
