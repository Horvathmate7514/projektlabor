namespace LeltarKezelo.Kozos.Torzsadatok;

/// <param name="Leltarozhato">A bejelentkezett felhasználó rögzíthet-e beolvasást ebben a körzetben.</param>
public sealed record LeltarkorzetAdat(long Id, string Kod, string Nev, string? SzervezetiEgyseg, bool Leltarozhato);

public sealed record KodTipusAdat(long Id, string Nev, bool VonalkodkentHasznalhato);
