namespace LeltarKezelo.Kozos.Torzsadatok;

/// <param name="Leltarozhato">A bejelentkezett felhasználó rögzíthet-e beolvasást ebben a körzetben.</param>
public sealed record LeltarkorzetAdat(long Id, string Kod, string Nev, string? SzervezetiEgyseg, bool Leltarozhato);

public sealed record KodTipusAdat(long Id, string Nev, bool VonalkodkentHasznalhato);

public sealed record EszkozTipusAdat(long Id, string Nev, long? SzuloId);

/// <param name="Megszunt">Selejtezett, elveszett, ellopott, átadott – nem része az elvárt állománynak.</param>
public sealed record EszkozAllapotAdat(long Id, string Nev, bool Megszunt);

public sealed record HelyisegAdat(long Id, string Epulet, string? Emelet, string Szobaszam, string? Megnevezes, string? Vonalkod);

/// <summary>Csak a szűréshez szükséges adatok; elérhetőséget nem adunk ki (F-31).</summary>
public sealed record FelelosAdat(long Id, string Nev, string? SzervezetiEgyseg);
