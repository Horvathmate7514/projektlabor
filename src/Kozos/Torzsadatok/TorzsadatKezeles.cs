namespace LeltarKezelo.Kozos.Torzsadatok;

/// <summary>Helyiség felvétele vagy módosítása; az épület és a szobaszám együtt egyedi (F-22).</summary>
public sealed record HelyisegMentes(
    string Epulet,
    string Szobaszam,
    string? Emelet = null,
    string? Megnevezes = null,
    string? Vonalkod = null,
    bool Aktiv = true)
{
    public const int MaxEpuletHossz = 50;
    public const int MaxEmeletHossz = 20;
    public const int MaxSzobaszamHossz = 20;
    public const int MaxMegnevezesHossz = 200;
    public const int MaxVonalkodHossz = 64;
}

public sealed record HelyisegKezelesAdat(
    long Id, string Epulet, string? Emelet, string Szobaszam, string? Megnevezes, string? Vonalkod, bool Aktiv, int Eszkozok);

public sealed record EszkozTipusMentes(string Nev, long? SzuloId = null, bool Aktiv = true)
{
    public const int MaxNevHossz = 100;
}

public sealed record EszkozTipusKezelesAdat(long Id, string Nev, long? SzuloId, bool Aktiv, int Eszkozok, int Altipusok);

public sealed record FelelosMentes(
    string Nev,
    string? Azonosito = null,
    string? Email = null,
    string? SzervezetiEgyseg = null,
    bool Aktiv = true)
{
    public const int MaxNevHossz = 200;
    public const int MaxAzonositoHossz = 50;
    public const int MaxEmailHossz = 200;
    public const int MaxEgysegHossz = 200;
}

public sealed record FelelosKezelesAdat(
    long Id, string Nev, string? Azonosito, string? Email, string? SzervezetiEgyseg, bool Aktiv, int Eszkozok);
