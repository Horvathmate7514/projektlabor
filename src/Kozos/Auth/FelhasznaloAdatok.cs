namespace LeltarKezelo.Kozos.Auth;

public sealed record FelhasznaloAdatok(long Id, string Felhasznalonev, string Nev, IReadOnlyList<string> Szerepkorok);
