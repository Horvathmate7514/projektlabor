using System.Text.Json.Serialization;

namespace LeltarKezelo.Kozos.Leltar;

/// <summary>Hogyan változott egy eszköz tétele a korábbi leltáridőszakhoz képest.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TetelValtozas>))]
public enum TetelValtozas
{
    UjHiany,
    TartosHiany,
    Megkerult,
    AllapotValtozott,
    UjTetel,
    KikerultTetel,
    Valtozatlan,
}

public sealed record OsszevetesTetel(
    long EszkozId,
    string Megnevezes,
    TetelAllapot? KorabbiAllapot,
    TetelAllapot? AktualisAllapot,
    int KorabbiBeolvasott,
    int AktualisBeolvasott,
    TetelValtozas Valtozas);

public sealed record OsszevetesSzuro
{
    public const int MaxOldalMeret = 200;

    public long KorabbiIdoszakId { get; init; }
    public TetelValtozas? Valtozas { get; init; }
    public bool CsakValtozasok { get; init; }
    public int Oldal { get; init; } = 1;
    public int OldalMeret { get; init; } = 50;
}

public sealed record IdoszakOsszevetes(
    long KorabbiIdoszakId,
    long AktualisIdoszakId,
    IReadOnlyDictionary<TetelValtozas, int> Darabok,
    LapozottLista<OsszevetesTetel> Tetelek);
