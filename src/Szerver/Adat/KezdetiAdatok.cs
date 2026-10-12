using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Adat;

/// <summary>
/// Minden telepítésben szükséges alapadatok, amelyek a migrációval együtt kerülnek az adatbázisba.
/// A demóadatok ezzel szemben a <c>db/demoadatok.sql</c> szkriptben vannak.
/// Az azonosítók rögzítettek: a migrációk és a demószkript is ezekre hivatkoznak, ezért nem módosíthatók.
/// </summary>
public static class KezdetiAdatok
{
    /// <summary>A kódtípusok rögzített azonosítói (D-022); az import is ezekre hivatkozik.</summary>
    public static class KodTipusok
    {
        public const long SapSzam = 1;
        public const long LeltariSzam1 = 2;
        public const long LeltariSzam2 = 3;
        public const long GyartasiSzam = 4;
        public const long BelsoVonalkod = 5;
    }

    public static void Feltoltes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KodTipus>().HasData(
            new KodTipus { Id = KodTipusok.SapSzam, Nev = "SAP szám" },
            new KodTipus { Id = KodTipusok.LeltariSzam1, Nev = "Leltári szám 1" },
            new KodTipus { Id = KodTipusok.LeltariSzam2, Nev = "Leltári szám 2" },
            new KodTipus { Id = KodTipusok.GyartasiSzam, Nev = "Gyártási szám" },
            new KodTipus { Id = KodTipusok.BelsoVonalkod, Nev = "Belső vonalkód" });

        modelBuilder.Entity<EszkozAllapot>().HasData(
            new EszkozAllapot { Id = 1, Nev = "aktív", Sorrend = 1 },
            new EszkozAllapot { Id = 2, Nev = "javítás alatt", Sorrend = 2 },
            new EszkozAllapot { Id = 3, Nev = "kölcsönadva", Sorrend = 3 },
            new EszkozAllapot { Id = 4, Nev = "selejtezésre javasolt", Sorrend = 4 },
            new EszkozAllapot { Id = 5, Nev = "selejtezett", Megszunt = true, Sorrend = 5 },
            new EszkozAllapot { Id = 6, Nev = "elveszett", Megszunt = true, Sorrend = 6 },
            new EszkozAllapot { Id = 7, Nev = "ellopott", Megszunt = true, Sorrend = 7 },
            new EszkozAllapot { Id = 8, Nev = "átadva", Megszunt = true, Sorrend = 8 });

        modelBuilder.Entity<Szerepkor>().HasData(
            new Szerepkor { Id = 1, Nev = Szerepkorok.Admin },
            new Szerepkor { Id = 2, Nev = Szerepkorok.Leltarfelelos },
            new Szerepkor { Id = 3, Nev = Szerepkorok.Leltarozo },
            new Szerepkor { Id = 4, Nev = Szerepkorok.Megtekinto });
    }
}
