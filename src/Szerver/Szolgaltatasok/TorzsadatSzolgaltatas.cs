using System.Security.Claims;
using LeltarKezelo.Kozos.Torzsadatok;
using LeltarKezelo.Szerver.Adat;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public class TorzsadatSzolgaltatas(LeltarDbContext db, KorzetJogosultsag jogosultsag)
{
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

    public async Task<IReadOnlyList<EszkozTipusAdat>> EszkozTipusokAsync(CancellationToken ct) =>
        await db.EszkozTipusok.AsNoTracking()
            .Where(t => t.Aktiv)
            .OrderBy(t => t.Nev)
            .Select(t => new EszkozTipusAdat(t.Id, t.Nev, t.SzuloId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EszkozAllapotAdat>> EszkozAllapotokAsync(CancellationToken ct) =>
        await db.EszkozAllapotok.AsNoTracking()
            .OrderBy(a => a.Sorrend)
            .Select(a => new EszkozAllapotAdat(a.Id, a.Nev, a.Megszunt))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HelyisegAdat>> HelyisegekAsync(CancellationToken ct) =>
        await db.Helyisegek.AsNoTracking()
            .Where(h => h.Aktiv)
            .OrderBy(h => h.Epulet).ThenBy(h => h.Szobaszam)
            .Select(h => new HelyisegAdat(h.Id, h.Epulet, h.Emelet, h.Szobaszam, h.Megnevezes, h.Vonalkod))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FelelosAdat>> FelelosokAsync(CancellationToken ct) =>
        await db.FelelosSzemelyek.AsNoTracking()
            .Where(f => f.Aktiv)
            .OrderBy(f => f.Nev)
            .Select(f => new FelelosAdat(f.Id, f.Nev, f.SzervezetiEgyseg))
            .ToListAsync(ct);
}
