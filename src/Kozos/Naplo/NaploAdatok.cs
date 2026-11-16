namespace LeltarKezelo.Kozos.Naplo;

public static class NaploMuveletek
{
    public const string Letrehozas = "LETREHOZAS";
    public const string Modositas = "MODOSITAS";
    public const string Torles = "TORLES";
}

/// <summary>Az audit napló egy sora; a régi és az új érték a megváltozott mezők JSON-objektuma.</summary>
public sealed record NaploElem(
    long Id,
    DateTime Idopont,
    string Entitas,
    long EntitasId,
    string Muvelet,
    string? RegiErtekJson,
    string? UjErtekJson,
    string? Felhasznalo);

public sealed record NaploSzuro
{
    public const int MaxOldalMeret = 200;

    public string? Entitas { get; init; }
    public long? EntitasId { get; init; }
    public long? FelhasznaloId { get; init; }
    public DateTime? Tol { get; init; }
    public DateTime? Ig { get; init; }
    public int Oldal { get; init; } = 1;
    public int OldalMeret { get; init; } = 50;
}
