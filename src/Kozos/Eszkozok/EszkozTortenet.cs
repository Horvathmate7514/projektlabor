using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Kozos.Naplo;

namespace LeltarKezelo.Kozos.Eszkozok;

/// <summary>Egy eszköz teljes története: állapot, hely, felelős, leltárak és adatmódosítások (UC-14).</summary>
public sealed record EszkozTortenet(
    long EszkozId,
    string Megnevezes,
    DateTime Letrehozva,
    IReadOnlyList<AllapotTortenetElem> Allapotok,
    IReadOnlyList<HelyTortenetElem> Helyek,
    IReadOnlyList<FelelosTortenetElem> Felelosok,
    IReadOnlyList<LeltarTortenetElem> Leltarak,
    IReadOnlyList<NaploElem> Modositasok);

public sealed record AllapotTortenetElem(
    DateTime Idopont, string? RegiAllapot, string UjAllapot, bool Megszunt, string Indoklas, string? Ugyiratszam, string Felhasznalo);

/// <summary>A <see cref="Rogzitette"/> leltárból származó helynél a leltározó, kézi áthelyezésnél a rögzítő felhasználó.</summary>
public sealed record HelyTortenetElem(
    long HelyisegId,
    string Helyiseg,
    DateTime ErvenyesTol,
    DateTime? ErvenyesIg,
    ElhelyezesForras Forras,
    long? LeolvasasId,
    string? Rogzitette);

public sealed record FelelosTortenetElem(long FelelosId, string Nev, DateTime ErvenyesTol, DateTime? ErvenyesIg, string? Rogzitette);

public sealed record LeltarTortenetElem(
    long LeltarIdoszakId,
    string Megnevezes,
    LeltarIdoszakStatusz Statusz,
    DateOnly Kezdete,
    string? ElvartKorzetKod,
    int ElvartMennyiseg,
    int BeolvasottMennyiseg,
    TetelAllapot Allapot,
    IReadOnlyList<string> TalaltKorzetek,
    IReadOnlyList<HelyisegMennyiseg> Helyisegek,
    DateTime? UtolsoBeolvasas);

/// <summary>A leltárban egy helyiségben beolvasott mennyiség; helyiség nélküli beolvasásnál a név üres.</summary>
public sealed record HelyisegMennyiseg(long? HelyisegId, string? Helyiseg, int Mennyiseg);

/// <summary>Az eszköz állapota, helye és felelőse egy korábbi időpontban.</summary>
public sealed record EszkozPillanat(
    long EszkozId,
    DateTime Idopont,
    bool Letezett,
    string? Allapot,
    bool Megszunt,
    long? HelyisegId,
    string? Helyiseg,
    long? FelelosId,
    string? Felelos);
