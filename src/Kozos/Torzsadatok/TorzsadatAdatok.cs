namespace LeltarKezelo.Kozos.Torzsadatok;

public sealed record LeltarkorzetAdat(long Id, string Kod, string Nev, string? SzervezetiEgyseg, bool Leltarozhato);

public sealed record KodTipusAdat(long Id, string Nev, bool VonalkodkentHasznalhato);

public sealed record EszkozTipusAdat(long Id, string Nev, long? SzuloId);

public sealed record EszkozAllapotAdat(long Id, string Nev, bool Megszunt);

public sealed record HelyisegAdat(long Id, string Epulet, string? Emelet, string Szobaszam, string? Megnevezes, string? Vonalkod);

public sealed record FelelosAdat(long Id, string Nev, string? SzervezetiEgyseg);
