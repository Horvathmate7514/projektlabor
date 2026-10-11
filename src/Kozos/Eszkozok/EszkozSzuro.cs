namespace LeltarKezelo.Kozos.Eszkozok;

public sealed record EszkozSzuro
{
    public const int MaxOldalMeret = 200;

    public string? Kereses { get; init; }
    public long? LeltarkorzetId { get; init; }
    /// <summary>A megadott típus és minden altípusa.</summary>
    public long? EszkozTipusId { get; init; }
    public long? EszkozAllapotId { get; init; }
    /// <summary>Az eszköz aktuális helye (érvényes elhelyezés).</summary>
    public long? HelyisegId { get; init; }
    /// <summary>Az eszköz aktuális felelőse (érvényes hozzárendelés).</summary>
    public long? FelelosId { get; init; }
    public int Oldal { get; init; } = 1;
    public int OldalMeret { get; init; } = 50;
}
