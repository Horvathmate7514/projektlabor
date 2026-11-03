using System.Text.Json.Serialization;

namespace LeltarKezelo.Kozos.Leltar;

[JsonConverter(typeof(JsonStringEnumConverter<TetelAllapot>))]
public enum TetelAllapot
{
    Hianyzik,
    Reszleges,
    Rendben,
    Tobblet,
    MasKorzetben,
    NemElvart,
}

public sealed record KorzetOsszesites(
    long LeltarkorzetId,
    string Kod,
    string Nev,
    int ElvartTetelek,
    int Rendben,
    int Reszleges,
    int Hianyzik,
    int Tobblet,
    int MasKorzetben,
    int MasKorzetbolIdeKerult,
    int NemElvart,
    int IsmeretlenKodok,
    int ElvartMennyiseg,
    int BeolvasottMennyiseg)
{
    public double Keszultseg => ElvartTetelek == 0 ? 0 : (double)(Rendben + Tobblet) / ElvartTetelek;
}

public sealed record OsszesitesTetel(
    long EszkozId,
    string Megnevezes,
    string? MennyisegiEgyseg,
    long? ElvartKorzetId,
    string? ElvartKorzetKod,
    int ElvartMennyiseg,
    int BeolvasottMennyiseg,
    int SajatKorzetben,
    int Hiany,
    int Tobblet,
    TetelAllapot Allapot,
    IReadOnlyList<string> TalaltKorzetek,
    DateTime? UtolsoBeolvasas);

public sealed record OsszesitesSzuro
{
    public const int MaxOldalMeret = 200;

    public long? LeltarkorzetId { get; init; }
    public TetelAllapot? Allapot { get; init; }
    public bool CsakElteresek { get; init; }
    public int Oldal { get; init; } = 1;
    public int OldalMeret { get; init; } = 50;
}

public sealed record UtolagosTetelKeres(long EszkozId);

public sealed record ElvartTetelAdat(
    long Id,
    long EszkozId,
    string Megnevezes,
    long LeltarkorzetId,
    string LeltarkorzetKod,
    int ElvartMennyiseg,
    ElvartTetelFelvitelOka FelvitelOka);
