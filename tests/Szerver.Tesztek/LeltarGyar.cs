using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;

namespace LeltarKezelo.Szerver.Tesztek;

public class LeltarGyar : SzerverGyar
{
    public const string Felelos = "felelos";
    public const string Leltarozo = "leltarozo";
    public const string Megtekinto = "megtekinto";
    public const string KorzetNelkuli = "korzetnelkuli";

    public const long MikKorzet = 1;
    public const long GtkKorzet = 2;

    public const long SzamitastechnikaTipus = 10;
    public const long MonitorTipus = 11;
    public const long Terem101 = 1;
    public const long KovacsAnna = 1;
    public const string Gtk = "Gazdaságtudományi Kar";

    protected override void Feltoltes(LeltarDbContext db)
    {
        base.Feltoltes(db);

        var mik = db.Leltarkorzetek.Find(MikKorzet)!;
        var aktiv = db.EszkozAllapotok.Find(1L)!;
        var sap = db.KodTipusok.Find(1L)!;
        var selejtezett = new EszkozAllapot { Id = 5, Nev = "selejtezett", Megszunt = true, Sorrend = 5 };
        var belso = new KodTipus { Id = 5, Nev = "Belső vonalkód" };
        db.AddRange(selejtezett, belso);

        var szekek = Eszkoz(4, "Tárgyalószék", mik, aktiv, Kod(belso, "SZEK-30"));
        szekek.ElvartMennyiseg = 3;
        szekek.MennyisegiEgyseg = "db";
        db.Eszkozok.AddRange(szekek, Eszkoz(5, "Régi nyomtató", mik, selejtezett, Kod(sap, "SAP-0005")));

        var regiKod = Kod(sap, "REGI-0001");
        regiKod.Aktiv = false;
        regiKod.Eszkoz = db.Eszkozok.Find(1L)!;
        db.EszkozKodok.Add(regiKod);

        var gep = db.Eszkozok.Find(1L)!;
        var monitor = db.Eszkozok.Find(2L)!;
        var szamtech = new EszkozTipus { Id = SzamitastechnikaTipus, Nev = "Számítástechnika" };
        var monitorTipus = new EszkozTipus { Id = MonitorTipus, Nev = "Monitor", Szulo = szamtech };
        gep.EszkozTipus = szamtech;
        monitor.EszkozTipus = monitorTipus;

        var terem = new Helyiseg { Id = Terem101, Epulet = "I", Szobaszam = "101" };
        var regiTerem = new Helyiseg { Id = 2, Epulet = "I", Szobaszam = "102" };
        var kezdet = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Elhelyezesek.AddRange(
            new Elhelyezes { Eszkoz = gep, Helyiseg = terem, ErvenyesTol = kezdet },
            new Elhelyezes { Eszkoz = monitor, Helyiseg = terem, ErvenyesTol = kezdet, ErvenyesIg = kezdet.AddMonths(6) },
            new Elhelyezes { Eszkoz = monitor, Helyiseg = regiTerem, ErvenyesTol = kezdet.AddMonths(6) });

        var kovacs = new FelelosSzemely { Id = KovacsAnna, Nev = "Kovács Anna" };
        db.FelelosHozzarendelesek.Add(new FelelosHozzarendeles { Eszkoz = monitor, Felelos = kovacs, ErvenyesTol = kezdet });

        db.AddRange(szamtech, monitorTipus, terem, regiTerem, kovacs);
        db.Leltarkorzetek.Find(GtkKorzet)!.SzervezetiEgyseg = Gtk;

        var leltarozo = Felhasznalo(Leltarozo, "Leltár Ozó", true, db.Szerepkorok.Local.Single(s => s.Nev == Szerepkorok.Leltarozo));
        leltarozo.JogosultKorzetek = [mik];
        db.Felhasznalok.AddRange(
            Felhasznalo(Felelos, "Leltár Felelős", true, new Szerepkor { Nev = Szerepkorok.Leltarfelelos }),
            leltarozo,
            Felhasznalo(KorzetNelkuli, "Körzet Nélküli", true, db.Szerepkorok.Local.Single(s => s.Nev == Szerepkorok.Leltarozo)));
        var megtekinto = Felhasznalo(Megtekinto, "Meg Tekintő", true, new Szerepkor { Nev = Szerepkorok.Megtekinto });
        megtekinto.SzervezetiEgyseg = Gtk;
        db.Felhasznalok.Add(megtekinto);
    }
}
