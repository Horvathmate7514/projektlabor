using LeltarKezelo.Kozos.Leltar;

namespace LeltarKezelo.Kozos.Import;

/// <summary>
/// A feltöltés (<see cref="ApiUtvonalak.ImportProbafuttatas"/>, <see cref="ApiUtvonalak.Importok"/>) multipart/form-data mezői.
/// </summary>
public static class ImportUrlap
{
    /// <summary>Az .xlsx fájl (kötelező).</summary>
    public const string Fajl = "fajl";

    /// <summary>A munkalap neve; ha hiányzik, az első olyan munkalap, amelyen fejlécsor található.</summary>
    public const string Munkalap = "munkalap";

    /// <summary>Kézi oszlop-hozzárendelés JSON objektumként, mezőnév → Excel-fejléc, pl. <c>{"Megnevezes":"Tétel neve"}</c>.</summary>
    public const string Oszlopok = "oszlopok";

    /// <summary><c>false</c> esetén a már létező eszközök módosításai kimaradnak (alapértelmezés: <c>true</c>).</summary>
    public const string Frissitesek = "frissitesek";

    public const long MaxFajlMeret = 10 * 1024 * 1024;
}

public sealed record ImportFutasListaElem(
    long Id,
    string Fajlnev,
    DateTime Idopont,
    string Felhasznalo,
    int SorokSzama,
    int Sikeres,
    int Hibas,
    ImportStatusz Statusz);

public sealed record ImportHibaAdat(int Sor, string? Oszlop, string Uzenet, string? Ertek);

public sealed record ImportFutasReszletek(
    long Id,
    string Fajlnev,
    DateTime Idopont,
    string Felhasznalo,
    int SorokSzama,
    int Sikeres,
    int Hibas,
    ImportStatusz Statusz,
    IReadOnlyList<ImportHibaAdat> Hibak);

public sealed record ImportNaploSzuro
{
    public const int MaxOldalMeret = 200;

    public ImportStatusz? Statusz { get; init; }
    public int Oldal { get; init; } = 1;
    public int OldalMeret { get; init; } = 50;
}
