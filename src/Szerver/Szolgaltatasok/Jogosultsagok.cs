using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Szerver.Adat;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

/// <summary>
/// A jogosultsági mátrix (<c>04_alkalom/01_Szerver_PoC.md</c>, 4.1) szerepkör-csoportjai az
/// <c>[Authorize(Roles = ...)]</c> jelöléshez. A körzetszintű korlátozást a szolgáltatások ellenőrzik.
/// </summary>
public static class Jogosultsagok
{
    /// <summary>Leltáridőszak nyitása, lezárása, más leolvasásának sztornózása.</summary>
    public const string LeltarKezeles = Szerepkorok.Admin + "," + Szerepkorok.Leltarfelelos;

    /// <summary>Beolvasás rögzítése, saját leolvasás sztornózása (a leltározó csak a jogosult körzeteiben).</summary>
    public const string Beolvasas = Szerepkorok.Admin + "," + Szerepkorok.Leltarfelelos + "," + Szerepkorok.Leltarozo;

    public static long FelhasznaloId(this ClaimsPrincipal felhasznalo) =>
        long.Parse(felhasznalo.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>Az adminisztrátor és a leltárfelelős minden körzetben dolgozhat, a leltározó csak a kijelöltekben.</summary>
    public static bool MindenKorzetben(this ClaimsPrincipal felhasznalo) =>
        felhasznalo.IsInRole(Szerepkorok.Admin) || felhasznalo.IsInRole(Szerepkorok.Leltarfelelos);
}

/// <summary>Leltárkörzet szintű jogosultság: a leltározó csak a hozzá rendelt körzetekben rögzíthet beolvasást.</summary>
public class KorzetJogosultsag(LeltarDbContext db)
{
    /// <returns>A felhasználó által leltározható körzetek; <c>null</c>, ha mindegyik.</returns>
    public async Task<IReadOnlySet<long>?> LeltarozhatoKorzetekAsync(ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        if (felhasznalo.MindenKorzetben())
            return null;
        if (!felhasznalo.IsInRole(Szerepkorok.Leltarozo))
            return new HashSet<long>();

        var id = felhasznalo.FelhasznaloId();
        var korzetek = await db.Felhasznalok.AsNoTracking()
            .Where(f => f.Id == id)
            .SelectMany(f => f.JogosultKorzetek.Select(k => k.Id))
            .ToListAsync(ct);
        return korzetek.ToHashSet();
    }

    public async Task<bool> LeltarozhatAsync(ClaimsPrincipal felhasznalo, long korzetId, CancellationToken ct) =>
        await LeltarozhatoKorzetekAsync(felhasznalo, ct) is not { } korzetek || korzetek.Contains(korzetId);
}
