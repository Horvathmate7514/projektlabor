namespace LeltarKezelo.Kozos.Leltar;

/// <summary>A szerver által egy beolvasáshoz rendelt minősítés (adatmodell v1, 3.2).</summary>
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
public enum BeviteliMod
{
    Olvaso,
    Kamera,
    Beillesztes,
    Kezi,
    Demo,
}

public enum LeltarIdoszakTipus
{
    Eves,
    Rendkivuli,
}

public enum LeltarIdoszakStatusz
{
    Tervezett,
    Nyitott,
    Lezart,
}

/// <summary>Az elvárt tétel a nyitáskori pillanatképből vagy utólagos bővítésből származik (D-003).</summary>
public enum ElvartTetelFelvitelOka
{
    Pillanatkep,
    Utolagos,
}

public enum ElhelyezesForras
{
    Leltar,
    Kezi,
}

/// <summary>A kiegészítő meglétét közvetlenül ellenőrizték, vagy csak a fő eszköz alapján feltételezték (F-20).</summary>
public enum KiegeszitoEllenorzesMod
{
    Kozvetlen,
    Feltetelezett,
}

public enum ImportStatusz
{
    Folyamatban,
    Sikeres,
    Reszleges,
    Sikertelen,
}
