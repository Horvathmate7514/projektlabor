namespace LeltarKezelo.Kozos.Leltar;

public sealed record LeltarIdoszakAdat(
    long Id,
    string Megnevezes,
    LeltarIdoszakTipus Tipus,
    DateOnly Kezdete,
    DateOnly? Vege,
    LeltarIdoszakStatusz Statusz,
    DateTime? Lezarva,
    int ElvartTetelekSzama);

public sealed record LeltarIdoszakLetrehozas(
    string Megnevezes,
    LeltarIdoszakTipus Tipus,
    DateOnly Kezdete,
    DateOnly? Vege = null);
