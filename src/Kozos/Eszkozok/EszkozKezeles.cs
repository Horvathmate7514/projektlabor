using LeltarKezelo.Kozos.Leltar;

namespace LeltarKezelo.Kozos.Eszkozok;

/// <summary>Kézi áthelyezés; üres helyiséggel az eszköz helye ismeretlenné válik (UC-13).</summary>
public sealed record HelyKeres(long? HelyisegId);

/// <summary>Felelős hozzárendelése vagy cseréje; üres felelőssel a hozzárendelés megszűnik (UC-13).</summary>
public sealed record FelelosKeres(long? FelelosId);

public sealed record TipusKeres(long? EszkozTipusId);

/// <summary>Állapotváltás kötelező indoklással (F-26, UC-12).</summary>
public sealed record AllapotKeres(long EszkozAllapotId, string Indoklas, string? Ugyiratszam = null)
{
    public const int MaxIndoklasHossz = 500;
    public const int MaxUgyiratszamHossz = 50;
}

/// <summary>Az eszköz aktuális típusa, helye, felelőse és állapota.</summary>
public sealed record EszkozHozzarendelesek(
    long EszkozId,
    long? EszkozTipusId,
    string? EszkozTipus,
    long? HelyisegId,
    string? Helyiseg,
    DateTime? HelyErvenyesTol,
    ElhelyezesForras? HelyForras,
    long? FelelosId,
    string? Felelos,
    DateTime? FelelosErvenyesTol,
    long EszkozAllapotId,
    string Allapot,
    bool Megszunt);
