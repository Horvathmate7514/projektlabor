namespace LeltarKezelo.Kozos.Eszkozok;

public sealed record EszkozSzuro
{
    public const int MaxOldalMeret = 200;

    public string? Kereses { get; init; }
    public long? LeltarkorzetId { get; init; }
    public long? EszkozTipusId { get; init; }
    public long? EszkozAllapotId { get; init; }
    public long? HelyisegId { get; init; }
    public long? FelelosId { get; init; }
    public int Oldal { get; init; } = 1;
    public int OldalMeret { get; init; } = 50;
}
