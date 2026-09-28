using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public class AuthSzolgaltatas(
    LeltarDbContext db,
    IPasswordHasher<Felhasznalo> jelszoKezelo,
    TokenKeszito tokenKeszito,
    TimeProvider ido)
{
    public async Task<BejelentkezesValasz?> BejelentkezesAsync(BejelentkezesKeres keres, CancellationToken ct)
    {
        var nev = keres.Felhasznalonev.Trim();
        var felhasznalo = await db.Felhasznalok
            .Include(f => f.Szerepkorok)
            .SingleOrDefaultAsync(f => f.Felhasznalonev == nev, ct);

        if (felhasznalo is null || !felhasznalo.Aktiv)
            return null;

        var eredmeny = jelszoKezelo.VerifyHashedPassword(felhasznalo, felhasznalo.JelszoHash, keres.Jelszo);
        if (eredmeny == PasswordVerificationResult.Failed)
            return null;

        if (eredmeny == PasswordVerificationResult.SuccessRehashNeeded)
            felhasznalo.JelszoHash = jelszoKezelo.HashPassword(felhasznalo, keres.Jelszo);

        felhasznalo.UtolsoBelepes = ido.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        var szerepkorok = felhasznalo.Szerepkorok.Select(s => s.Nev).ToList();
        var (token, lejarat) = tokenKeszito.Keszit(felhasznalo, szerepkorok);

        return new BejelentkezesValasz(token, lejarat,
            new FelhasznaloAdatok(felhasznalo.Id, felhasznalo.Felhasznalonev, felhasznalo.Nev, szerepkorok));
    }
}
