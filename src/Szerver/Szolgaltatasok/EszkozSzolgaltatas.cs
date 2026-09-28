using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Szerver.Adat;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public class EszkozSzolgaltatas(LeltarDbContext db)
{
    public async Task<LapozottLista<EszkozListaElem>> ListazasAsync(EszkozSzuro szuro, CancellationToken ct)
    {
        var oldal = Math.Max(1, szuro.Oldal);
        var meret = Math.Clamp(szuro.OldalMeret, 1, EszkozSzuro.MaxOldalMeret);

        var lekerdezes = db.Eszkozok.AsNoTracking();

        if (szuro.LeltarkorzetId is { } korzetId)
            lekerdezes = lekerdezes.Where(e => e.LeltarkorzetId == korzetId);

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
                e.Kodok.Where(k => k.Aktiv).OrderBy(k => k.KodTipusId).Select(k => k.Ertek).ToList()))
            .ToListAsync(ct);

        return new LapozottLista<EszkozListaElem>(elemek, oldal, meret, osszesen);
    }

    public Task<EszkozReszletek?> ReszletekAsync(long id, CancellationToken ct) =>
        db.Eszkozok.AsNoTracking()
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
