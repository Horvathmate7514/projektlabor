namespace LeltarKezelo.Kozos.Eszkozok;

public sealed record EszkozReszletek(
    long Id,
    string Megnevezes,
    string? EszkozTipus,
    string LeltarkorzetKod,
    string LeltarkorzetNev,
    string Allapot,
    int ElvartMennyiseg,
    string? MennyisegiEgyseg,
    decimal? Ertek,
    DateOnly? BeszerzesDatuma,
    string? Megjegyzes,
    IReadOnlyList<EszkozKodAdat> Kodok);

public sealed record EszkozKodAdat(string KodTipus, string Ertek, bool Aktiv);
