using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LeltarKezelo.Szerver.Tesztek;

public class SzerverGyar : WebApplicationFactory<Program>
{
    public const string Felhasznalonev = "teszt";
    public const string Jelszo = "Teszt-Jelszo-123";

    private readonly string _adatbazisNev = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Kulcs", "teszt-kulcs-ami-legalabb-harminckettő-karakter");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LeltarDbContext>>();
            services.AddDbContext<LeltarDbContext>(o => o.UseInMemoryDatabase(_adatbazisNev));

            using var scope = services.BuildServiceProvider().CreateScope();
            Feltoltes(scope.ServiceProvider.GetRequiredService<LeltarDbContext>());
        });
    }

    private static void Feltoltes(LeltarDbContext db)
    {
        var sap = new KodTipus { Id = 1, Nev = "SAP szám" };
        var leltari = new KodTipus { Id = 2, Nev = "Leltári szám 1" };
        var korzet = new Leltarkorzet { Id = 1, Kod = "MIK-01", Nev = "Mérnöki Kar, I. épület" };
        var masikKorzet = new Leltarkorzet { Id = 2, Kod = "GTK-01", Nev = "Gazdaságtudományi Kar" };
        var aktiv = new EszkozAllapot { Id = 1, Nev = "aktív", Sorrend = 1 };

        db.AddRange(sap, leltari, korzet, masikKorzet, aktiv);
        db.Eszkozok.AddRange(
            Eszkoz(1, "Asztali számítógép", korzet, aktiv, Kod(sap, "SAP-0001"), Kod(leltari, "L-1001")),
            Eszkoz(2, "Monitor 24\"", korzet, aktiv, Kod(sap, "SAP-0002")),
            Eszkoz(3, "Irodai szék", masikKorzet, aktiv, Kod(sap, "SAP-0003")));

        var szerepkor = new Szerepkor { Nev = Szerepkorok.Leltarozo };
        db.Felhasznalok.AddRange(
            Felhasznalo(Felhasznalonev, "Teszt Elek", true, szerepkor),
            Felhasznalo("inaktiv", "Inaktív Felhasználó", false, szerepkor));

        db.SaveChanges();
    }

    private static Felhasznalo Felhasznalo(string felhasznalonev, string nev, bool aktiv, Szerepkor szerepkor)
    {
        var f = new Felhasznalo { Felhasznalonev = felhasznalonev, Nev = nev, Aktiv = aktiv, Szerepkorok = [szerepkor] };
        f.JelszoHash = new PasswordHasher<Felhasznalo>().HashPassword(f, Jelszo);
        return f;
    }

    private static Eszkoz Eszkoz(long id, string nev, Leltarkorzet korzet, EszkozAllapot allapot, params EszkozKod[] kodok) =>
        new() { Id = id, Megnevezes = nev, Leltarkorzet = korzet, EszkozAllapot = allapot, Kodok = kodok };

    private static EszkozKod Kod(KodTipus tipus, string ertek) =>
        new() { KodTipus = tipus, Ertek = ertek, ErtekNorm = ertek.Trim().ToUpperInvariant() };
}
