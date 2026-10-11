using System.Text.Json.Serialization;

namespace LeltarKezelo.Kozos.Leltar;

/// <summary>A szerver által egy beolvasáshoz rendelt minősítés (adatmodell v1, 3.2).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Minosites>))]
public enum Minosites
{
    Ok,
    MasKorzet,
    Ismetelt,
    Tobblet,
    IsmeretlenKod,
    NemAktiv,
}

/// <summary>Hogyan került a kód a rendszerbe; a demó beolvasások így kiszűrhetők.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BeviteliMod>))]
public enum BeviteliMod
{
    Olvaso,
    Kamera,
    Beillesztes,
    Kezi,
    Demo,
}

[JsonConverter(typeof(JsonStringEnumConverter<LeltarIdoszakTipus>))]
public enum LeltarIdoszakTipus
{
    Eves,
    Rendkivuli,
}

[JsonConverter(typeof(JsonStringEnumConverter<LeltarIdoszakStatusz>))]
public enum LeltarIdoszakStatusz
{
    Tervezett,
    Nyitott,
    Lezart,
}

/// <summary>Az elvárt tétel a nyitáskori pillanatképből vagy utólagos bővítésből származik (D-003).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ElvartTetelFelvitelOka>))]
public enum ElvartTetelFelvitelOka
{
    Pillanatkep,
    Utolagos,
}

[JsonConverter(typeof(JsonStringEnumConverter<ElhelyezesForras>))]
public enum ElhelyezesForras
{
    Leltar,
    Kezi,
}

/// <summary>A kiegészítő meglétét közvetlenül ellenőrizték, vagy csak a fő eszköz alapján feltételezték (F-20).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<KiegeszitoEllenorzesMod>))]
public enum KiegeszitoEllenorzesMod
{
    Kozvetlen,
    Feltetelezett,
}

[JsonConverter(typeof(JsonStringEnumConverter<ImportStatusz>))]
public enum ImportStatusz
{
    Folyamatban,
    Sikeres,
    Reszleges,
    Sikertelen,
}
