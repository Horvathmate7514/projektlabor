using LeltarKezelo.Kozos.Leltar;

namespace Leltarkezelo.Client.ViewModels;

/// <summary>
/// A visszajelzés fajtája; a színét a <see cref="Konverterek.JelzesSzinKonverter"/> adja
/// (06_alkalom/01_Leltarozas_szerver.md, 4. pont; UC-06).
/// </summary>
public enum Jelzes
{
    Semleges,
    Info,
    Rendben,
    Ismetelt,
    MasKorzet,
    Figyelmeztetes,
    Ismeretlen,
    Hiba,
}

public static class Minositesek
{
    public static Jelzes Jelzes(Minosites minosites) => minosites switch
    {
        Minosites.Ok => ViewModels.Jelzes.Rendben,
        Minosites.Ismetelt => ViewModels.Jelzes.Ismetelt,
        Minosites.MasKorzet => ViewModels.Jelzes.MasKorzet,
        Minosites.Tobblet or Minosites.NemAktiv => ViewModels.Jelzes.Figyelmeztetes,
        Minosites.IsmeretlenKod => ViewModels.Jelzes.Ismeretlen,
        _ => ViewModels.Jelzes.Semleges,
    };

    public static string Nev(Minosites minosites) => minosites switch
    {
        Minosites.Ok => "Rendben",
        Minosites.Ismetelt => "Ismételt",
        Minosites.MasKorzet => "Más körzet",
        Minosites.Tobblet => "Többlet",
        Minosites.NemAktiv => "Nem aktív",
        Minosites.IsmeretlenKod => "Ismeretlen kód",
        _ => minosites.ToString(),
    };

    public static string Nev(BeviteliMod mod) => mod switch
    {
        BeviteliMod.Olvaso => "olvasó",
        BeviteliMod.Kamera => "kamera",
        BeviteliMod.Beillesztes => "beillesztés",
        BeviteliMod.Kezi => "kézi",
        BeviteliMod.Demo => "demó",
        _ => mod.ToString(),
    };

    /// <summary>A szerver UTC időpontot küld (a <c>Kind</c> nincs megadva); a felületen helyi idő látszik.</summary>
    public static DateTime HelyiIdo(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
}
