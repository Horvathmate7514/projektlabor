using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public static class KezdoAdmin
{
    public static async Task LetrehozasAsync(IServiceProvider szolgaltatasok)
    {
        using var scope = szolgaltatasok.CreateScope();
        var konfig = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var naplo = scope.ServiceProvider.GetRequiredService<ILogger<LeltarDbContext>>();

        var felhasznalonev = konfig["KezdoAdmin:Felhasznalonev"];
        var jelszo = konfig["KezdoAdmin:Jelszo"];
        if (string.IsNullOrWhiteSpace(felhasznalonev) || string.IsNullOrWhiteSpace(jelszo))
            return;

        var db = scope.ServiceProvider.GetRequiredService<LeltarDbContext>();
        try
        {
            if (await db.Felhasznalok.AnyAsync())
                return;

            var meglevok = await db.Szerepkorok.Select(s => s.Nev).ToListAsync();
            foreach (var nev in Szerepkorok.Mind.Except(meglevok))
                db.Szerepkorok.Add(new Szerepkor { Nev = nev });
            await db.SaveChangesAsync();

            var admin = new Felhasznalo
            {
                Felhasznalonev = felhasznalonev,
                Nev = "Rendszeradminisztrátor",
                Szerepkorok = await db.Szerepkorok.Where(s => s.Nev == Szerepkorok.Admin).ToListAsync(),
            };
            admin.JelszoHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Felhasznalo>>()
                .HashPassword(admin, jelszo);

            db.Felhasznalok.Add(admin);
            await db.SaveChangesAsync();
            naplo.LogInformation("Kezdő adminisztrátor létrehozva: {Felhasznalonev}", felhasznalonev);
        }
        catch (Exception ex) when (ex is DbUpdateException or SqlException)
        {
            naplo.LogWarning(ex, "A kezdő adminisztrátor nem hozható létre: az adatbázis nem érhető el, vagy nincs migrálva.");
        }
    }
}
