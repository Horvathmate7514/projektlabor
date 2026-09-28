namespace LeltarKezelo.Kozos.Eszkozok;

public sealed record EszkozSzuro
{
    public const int MaxOldalMeret = 200;

    public string? Kereses { get; init; }
    public long? LeltarkorzetId { get; init; }
    public int Oldal { get; init; } = 1;
    public int OldalMeret { get; init; } = 50;
}
