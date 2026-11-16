namespace LeltarKezelo.Kozos.Leltar;

/// <summary>
/// Fő eszköz és kiegészítője (F-19, F-21). Az <see cref="Ellenorzes"/> a kért leltáridőszakra vagy leolvasásra
/// vonatkozik: üres, ha a kiegészítőt abban a leltárban még nem ellenőrizték (F-20).
/// </summary>
public sealed record KiegeszitoAdat(
    long Id,
    long FoEszkozId,
    long? KiegeszitoEszkozId,
    string Megnevezes,
    int Mennyiseg,
    DateTime ErvenyesTol,
    DateTime? ErvenyesIg,
    KiegeszitoEllenorzesMod? Ellenorzes = null);

public sealed record KiegeszitoFelvetel(long? KiegeszitoEszkozId, string? Leiras = null, int Mennyiseg = 1)
{
    public const int MaxLeirasHossz = 200;
    public const int MaxMennyiseg = 10_000;
}

/// <summary>Saját kód nélküli tartozék kézi megerősítése egy leolvasásnál (F-20).</summary>
public sealed record KiegeszitoEllenorzesKeres(bool Kozvetlen);
