using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;

namespace LeltarKezelo.Szerver.Tesztek;

/// <summary>
/// A <see cref="SzerverGyar"/> adatai kiegészítve a leltározás eseteivel: mennyiséges és selejtezett eszköz,
/// inaktivált kód, valamint leltárfelelős, körzethez rendelt leltározó és megtekintő felhasználó.
/// </summary>
public class LeltarGyar : SzerverGyar
{
    public const string Felelos = "felelos";
    /// <summary>Csak a MIK-01 (1-es) körzetben leltározhat.</summary>
    public const string Leltarozo = "leltarozo";
    public const string Megtekinto = "megtekinto";

    public const long MikKorzet = 1;
    public const long GtkKorzet = 2;

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

        var leltarozo = Felhasznalo(Leltarozo, "Leltár Ozó", true, db.Szerepkorok.Single(s => s.Nev == Szerepkorok.Leltarozo));
        leltarozo.JogosultKorzetek = [mik];
        db.Felhasznalok.AddRange(
            Felhasznalo(Felelos, "Leltár Felelős", true, new Szerepkor { Nev = Szerepkorok.Leltarfelelos }),
            leltarozo,
            Felhasznalo(Megtekinto, "Meg Tekintő", true, new Szerepkor { Nev = Szerepkorok.Megtekinto }));

        db.SaveChanges();
    }
}
