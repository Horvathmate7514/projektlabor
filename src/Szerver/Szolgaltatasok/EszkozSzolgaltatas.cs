using System.Security.Claims;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Szerver.Adat;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public class EszkozSzolgaltatas(LeltarDbContext db, KorzetJogosultsag jogosultsag)
{
    public async Task<LapozottLista<EszkozListaElem>> ListazasAsync(EszkozSzuro szuro, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var oldal = Math.Max(1, szuro.Oldal);
        var meret = Math.Clamp(szuro.OldalMeret, 1, EszkozSzuro.MaxOldalMeret);

        var lekerdezes = db.Eszkozok.AsNoTracking();

        if (await jogosultsag.LathatoKorzetekAsync(felhasznalo, ct) is { } lathato)
        {
            var korzetek = lathato.ToList();
            lekerdezes = lekerdezes.Where(e => korzetek.Contains(e.LeltarkorzetId));
        }

        if (szuro.LeltarkorzetId is { } korzetId)
            lekerdezes = lekerdezes.Where(e => e.LeltarkorzetId == korzetId);

        if (szuro.EszkozTipusId is { } tipusId)
        {
            var tipusok = await TipusEsAltipusaiAsync(tipusId, ct);
            lekerdezes = lekerdezes.Where(e => e.EszkozTipusId != null && tipusok.Contains(e.EszkozTipusId.Value));
        }

        if (szuro.EszkozAllapotId is { } allapotId)
            lekerdezes = lekerdezes.Where(e => e.EszkozAllapotId == allapotId);

        if (szuro.HelyisegId is { } helyisegId)
            lekerdezes = lekerdezes.Where(e => db.Elhelyezesek.Any(h =>
                h.EszkozId == e.Id && h.ErvenyesIg == null && h.HelyisegId == helyisegId));

        if (szuro.FelelosId is { } felelosId)
            lekerdezes = lekerdezes.Where(e => db.FelelosHozzarendelesek.Any(f =>
                f.EszkozId == e.Id && f.ErvenyesIg == null && f.FelelosId == felelosId));

        if (!string.IsNullOrWhiteSpace(szuro.Kereses))
        {
            var szoveg = szuro.Kereses.Trim();
            var kod = szoveg.ToUpperInvariant();
            lekerdezes = lekerdezes.Where(e =>
                e.Megnevezes.Contains(szoveg) ||
                e.Kodok.Any(k => k.Aktiv && k.ErtekNorm.StartsWith(kod)));
        }

        var osszesen = await lekerdezes.CountAsync(ct);

        var elemek = await lekerdezes
            .OrderBy(e => e.Megnevezes).ThenBy(e => e.Id)
            .Skip((oldal - 1) * meret)
            .Take(meret)
            .Select(e => new EszkozListaElem(
                e.Id,
                e.Megnevezes,
                e.EszkozTipus != null ? e.EszkozTipus.Nev : null,
                e.Leltarkorzet.Kod,
                e.EszkozAllapot.Nev,
                e.ElvartMennyiseg,
                e.MennyisegiEgyseg,
                e.Kodok.Where(k => k.Aktiv).OrderBy(k => k.KodTipusId).Select(k => k.Ertek).ToList(),
                db.Elhelyezesek.Where(h => h.EszkozId == e.Id && h.ErvenyesIg == null)
                    .Select(h => h.Helyiseg.Epulet + " " + h.Helyiseg.Szobaszam).FirstOrDefault(),
                db.FelelosHozzarendelesek.Where(f => f.EszkozId == e.Id && f.ErvenyesIg == null)
                    .Select(f => f.Felelos.Nev).FirstOrDefault()))
            .ToListAsync(ct);

        return new LapozottLista<EszkozListaElem>(elemek, oldal, meret, osszesen);
    }

    public async Task<EszkozReszletek?> ReszletekAsync(long id, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var korzetId = await db.Eszkozok.Where(e => e.Id == id).Select(e => (long?)e.LeltarkorzetId).SingleOrDefaultAsync(ct);
        if (korzetId is null)
            return null;
        if (await jogosultsag.LathatoKorzetekAsync(felhasznalo, ct) is { } lathato && !lathato.Contains(korzetId.Value))
            throw UzletiHiba.Tiltott("Nincs jogosultságod ennek az eszköznek a megtekintéséhez.");

        return await db.Eszkozok.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EszkozReszletek(
                e.Id,
                e.Megnevezes,
                e.EszkozTipus != null ? e.EszkozTipus.Nev : null,
                e.Leltarkorzet.Kod,
                e.Leltarkorzet.Nev,
                e.EszkozAllapot.Nev,
                e.ElvartMennyiseg,
                e.MennyisegiEgyseg,
                e.Ertek,
                e.BeszerzesDatuma,
                e.Megjegyzes,
                e.Kodok.OrderBy(k => k.KodTipusId)
                    .Select(k => new EszkozKodAdat(k.KodTipus.Nev, k.Ertek, k.Aktiv)).ToList()))
            .SingleOrDefaultAsync(ct);
    }

    private async Task<List<long>> TipusEsAltipusaiAsync(long tipusId, CancellationToken ct)
    {
        var fa = await db.EszkozTipusok.AsNoTracking().Select(t => new { t.Id, t.SzuloId }).ToListAsync(ct);
        var eredmeny = new List<long> { tipusId };
        for (var i = 0; i < eredmeny.Count; i++)
            eredmeny.AddRange(fa.Where(t => t.SzuloId == eredmeny[i] && !eredmeny.Contains(t.Id)).Select(t => t.Id));
        return eredmeny;
    }
}
