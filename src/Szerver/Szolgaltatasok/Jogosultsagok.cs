using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Szerver.Adat;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public static class Jogosultsagok
{
    public const string LeltarKezeles = Szerepkorok.Admin + "," + Szerepkorok.Leltarfelelos;

    public const string Beolvasas = Szerepkorok.Admin + "," + Szerepkorok.Leltarfelelos + "," + Szerepkorok.Leltarozo;

    public static long FelhasznaloId(this ClaimsPrincipal felhasznalo) =>
        long.Parse(felhasznalo.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    public static bool MindenKorzetben(this ClaimsPrincipal felhasznalo) =>
        felhasznalo.IsInRole(Szerepkorok.Admin) || felhasznalo.IsInRole(Szerepkorok.Leltarfelelos);
}

public class KorzetJogosultsag(LeltarDbContext db)
{
    public async Task<IReadOnlySet<long>?> LeltarozhatoKorzetekAsync(ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        if (felhasznalo.MindenKorzetben())
            return null;
        if (!felhasznalo.IsInRole(Szerepkorok.Leltarozo))
            return new HashSet<long>();

        return (await HozzarendeltKorzetekAsync(felhasznalo.FelhasznaloId(), ct)).ToHashSet();
    }

    public async Task<bool> LeltarozhatAsync(ClaimsPrincipal felhasznalo, long korzetId, CancellationToken ct) =>
        await LeltarozhatoKorzetekAsync(felhasznalo, ct) is not { } korzetek || korzetek.Contains(korzetId);

    public async Task<IReadOnlySet<long>?> LathatoKorzetekAsync(ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        if (felhasznalo.MindenKorzetben())
            return null;

        var id = felhasznalo.FelhasznaloId();
        var lathato = new HashSet<long>();
        if (felhasznalo.IsInRole(Szerepkorok.Leltarozo))
            lathato.UnionWith(await HozzarendeltKorzetekAsync(id, ct));
        if (felhasznalo.IsInRole(Szerepkorok.Megtekinto))
        {
            var egyseg = await db.Felhasznalok.Where(f => f.Id == id).Select(f => f.SzervezetiEgyseg).SingleOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(egyseg))
                lathato.UnionWith(await db.Leltarkorzetek
                    .Where(k => k.SzervezetiEgyseg == egyseg)
                    .Select(k => k.Id)
                    .ToListAsync(ct));
        }
        return lathato;
    }

    private Task<List<long>> HozzarendeltKorzetekAsync(long felhasznaloId, CancellationToken ct) =>
        db.Felhasznalok.AsNoTracking()
            .Where(f => f.Id == felhasznaloId)
            .SelectMany(f => f.JogosultKorzetek.Select(k => k.Id))
            .ToListAsync(ct);
}
