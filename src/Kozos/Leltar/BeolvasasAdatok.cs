namespace LeltarKezelo.Kozos.Leltar;

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

public sealed record DemoKod(string Kod, bool Ervenyes, long? EszkozId);
