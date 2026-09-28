namespace LeltarKezelo.Kozos.Eszkozok;

public sealed record EszkozListaElem(
    long Id,
    string Megnevezes,
    string? EszkozTipus,
    string LeltarkorzetKod,
    string Allapot,
    int ElvartMennyiseg,
    string? MennyisegiEgyseg,
    IReadOnlyList<string> Kodok);
