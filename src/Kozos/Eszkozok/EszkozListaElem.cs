namespace LeltarKezelo.Kozos.Eszkozok;

public sealed record EszkozListaElem(
    long Id,
    string Megnevezes,
    string? EszkozTipus,
    string LeltarkorzetKod,
    string Allapot,
    int ElvartMennyiseg,
    string? MennyisegiEgyseg,
    IReadOnlyList<string> Kodok,
    string? Helyiseg = null,
    string? Felelos = null);
