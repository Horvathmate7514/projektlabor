using System.Security.Claims;
using LeltarKezelo.Kozos.Torzsadatok;
using LeltarKezelo.Szerver.Adat;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public class TorzsadatSzolgaltatas(LeltarDbContext db, KorzetJogosultsag jogosultsag)
{
    /// <summary>Az aktív leltárkörzetek a körzetválasztóhoz; a leltározó számára nem engedélyezettek is látszanak, jelölve.</summary>
    public async Task<IReadOnlyList<LeltarkorzetAdat>> LeltarkorzetekAsync(ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var leltarozhato = await jogosultsag.LeltarozhatoKorzetekAsync(felhasznalo, ct);

        var korzetek = await db.Leltarkorzetek.AsNoTracking()
            .Where(k => k.Aktiv)
            .OrderBy(k => k.Kod)
            .Select(k => new { k.Id, k.Kod, k.Nev, k.SzervezetiEgyseg })
            .ToListAsync(ct);

        return korzetek
            .Select(k => new LeltarkorzetAdat(k.Id, k.Kod, k.Nev, k.SzervezetiEgyseg, leltarozhato?.Contains(k.Id) ?? true))
            .ToList();
    }

    public async Task<IReadOnlyList<KodTipusAdat>> KodTipusokAsync(CancellationToken ct) =>
        await db.KodTipusok.AsNoTracking()
            .Where(t => t.Aktiv)
            .OrderBy(t => t.Id)
            .Select(t => new KodTipusAdat(t.Id, t.Nev, t.VonalkodkentHasznalhato))
            .ToListAsync(ct);
}
