using System.Linq.Expressions;
using System.Security.Claims;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Kozos.Naplo;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

/// <summary>
/// Az adatok történetének lekérdezése (UC-14, M20): egy eszköz teljes története, az eszköz egy korábbi
/// időpontbeli állapota (D-047), valamint az audit napló (D-046).
/// </summary>
public class TortenetSzolgaltatas(LeltarDbContext db, KorzetJogosultsag jogosultsag)
{
    private static readonly Expression<Func<AuditNaplo, NaploElem>> NaploVetites = a => new NaploElem(
        a.Id, a.Idopont, a.Entitas, a.EntitasId, a.Muvelet, a.RegiErtekJson, a.UjErtekJson,
        a.Felhasznalo != null ? a.Felhasznalo.Nev : null);

    public async Task<EszkozTortenet> EszkozTortenetAsync(long eszkozId, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        await EszkozLathatosag.EllenorzesAsync(db, jogosultsag, eszkozId, felhasznalo, ct);

        var eszkoz = await db.Eszkozok.AsNoTracking()
            .Where(e => e.Id == eszkozId)
            .Select(e => new { e.Megnevezes, e.Letrehozva, e.ElvartMennyiseg })
            .SingleAsync(ct);

        var allapotok = await db.AllapotValtozasok.AsNoTracking()
            .Where(a => a.EszkozId == eszkozId)
            .OrderBy(a => a.Idopont).ThenBy(a => a.Id)
            .Select(a => new AllapotTortenetElem(
                a.Idopont,
                a.RegiAllapot != null ? a.RegiAllapot.Nev : null,
                a.UjAllapot.Nev,
                a.UjAllapot.Megszunt,
                a.Indoklas,
                a.Ugyiratszam,
                a.Felhasznalo.Nev))
            .ToListAsync(ct);

        var helyek = await db.Elhelyezesek.AsNoTracking()
            .Where(h => h.EszkozId == eszkozId)
            .OrderBy(h => h.ErvenyesTol).ThenBy(h => h.Id)
            .Select(h => new
            {
                h.Id,
                h.HelyisegId,
                Nev = h.Helyiseg.Epulet + " " + h.Helyiseg.Szobaszam,
                h.ErvenyesTol,
                h.ErvenyesIg,
                h.Forras,
                h.LeolvasasId,
                Leltarozo = h.Leolvasas != null ? h.Leolvasas.Felhasznalo.Nev : null,
            })
            .ToListAsync(ct);
        var helyRogzitok = await RogzitokAsync(nameof(Elhelyezes), helyek.Select(h => h.Id).ToList(), ct);

        var felelosok = await db.FelelosHozzarendelesek.AsNoTracking()
            .Where(f => f.EszkozId == eszkozId)
            .OrderBy(f => f.ErvenyesTol).ThenBy(f => f.Id)
            .Select(f => new { f.Id, f.FelelosId, f.Felelos.Nev, f.ErvenyesTol, f.ErvenyesIg })
            .ToListAsync(ct);
        var felelosRogzitok = await RogzitokAsync(nameof(FelelosHozzarendeles), felelosok.Select(f => f.Id).ToList(), ct);

        var kodIdk = await db.EszkozKodok.Where(k => k.EszkozId == eszkozId).Select(k => k.Id).ToListAsync(ct);
        var modositasok = await db.AuditNaplo.AsNoTracking()
            .Where(a => (a.Entitas == nameof(Eszkoz) && a.EntitasId == eszkozId)
                || (a.Entitas == nameof(EszkozKod) && kodIdk.Contains(a.EntitasId)))
            .OrderBy(a => a.Idopont).ThenBy(a => a.Id)
            .Select(NaploVetites)
            .ToListAsync(ct);

        return new EszkozTortenet(
            eszkozId,
            eszkoz.Megnevezes,
            eszkoz.Letrehozva,
            allapotok,
            helyek.Select(h => new HelyTortenetElem(h.HelyisegId, h.Nev, h.ErvenyesTol, h.ErvenyesIg, h.Forras, h.LeolvasasId,
                h.Leltarozo ?? helyRogzitok.GetValueOrDefault(h.Id))).ToList(),
            felelosok.Select(f => new FelelosTortenetElem(f.FelelosId, f.Nev, f.ErvenyesTol, f.ErvenyesIg,
                felelosRogzitok.GetValueOrDefault(f.Id))).ToList(),
            await LeltarakAsync(eszkozId, eszkoz.ElvartMennyiseg, ct),
            modositasok);
    }

    /// <summary>
    /// Az eszköz állapota, helye és felelőse a megadott időpontban, a történeti táblákból visszakeresve.
    /// Az időpont UTC-ben értendő (F-29).
    /// </summary>
    public async Task<EszkozPillanat> EszkozPillanatAsync(long eszkozId, DateTime idopont, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        await EszkozLathatosag.EllenorzesAsync(db, jogosultsag, eszkozId, felhasznalo, ct);
        if (idopont.Kind == DateTimeKind.Local)
            idopont = idopont.ToUniversalTime();

        var eszkoz = await db.Eszkozok.AsNoTracking()
            .Where(e => e.Id == eszkozId)
            .Select(e => new { e.Letrehozva, Allapot = e.EszkozAllapot.Nev, e.EszkozAllapot.Megszunt })
            .SingleAsync(ct);
        if (eszkoz.Letrehozva > idopont)
            return new EszkozPillanat(eszkozId, idopont, false, null, false, null, null, null, null);

        // Az utolsó váltás az időpont előtt; ha nincs ilyen, az első későbbi váltás régi állapota, különben a mostani.
        var valtasok = db.AllapotValtozasok.AsNoTracking().Where(a => a.EszkozId == eszkozId);
        var allapot = await valtasok.Where(a => a.Idopont <= idopont)
                .OrderByDescending(a => a.Idopont).ThenByDescending(a => a.Id)
                .Select(a => new { a.UjAllapot.Nev, a.UjAllapot.Megszunt })
                .FirstOrDefaultAsync(ct)
            ?? await valtasok.Where(a => a.Idopont > idopont && a.RegiAllapot != null)
                .OrderBy(a => a.Idopont).ThenBy(a => a.Id)
                .Select(a => new { a.RegiAllapot!.Nev, a.RegiAllapot.Megszunt })
                .FirstOrDefaultAsync(ct)
            ?? new { Nev = eszkoz.Allapot, eszkoz.Megszunt };

        var hely = await db.Elhelyezesek.AsNoTracking()
            .Where(h => h.EszkozId == eszkozId && h.ErvenyesTol <= idopont && (h.ErvenyesIg == null || h.ErvenyesIg > idopont))
            .OrderByDescending(h => h.ErvenyesTol)
            .Select(h => new { h.HelyisegId, Nev = h.Helyiseg.Epulet + " " + h.Helyiseg.Szobaszam })
            .FirstOrDefaultAsync(ct);
        var felelos = await db.FelelosHozzarendelesek.AsNoTracking()
            .Where(f => f.EszkozId == eszkozId && f.ErvenyesTol <= idopont && (f.ErvenyesIg == null || f.ErvenyesIg > idopont))
            .OrderByDescending(f => f.ErvenyesTol)
            .Select(f => new { f.FelelosId, f.Felelos.Nev })
            .FirstOrDefaultAsync(ct);

        return new EszkozPillanat(eszkozId, idopont, true, allapot.Nev, allapot.Megszunt,
            hely?.HelyisegId, hely?.Nev, felelos?.FelelosId, felelos?.Nev);
    }

    public async Task<LapozottLista<NaploElem>> NaploAsync(NaploSzuro szuro, CancellationToken ct)
    {
        var oldal = Math.Max(1, szuro.Oldal);
        var meret = Math.Clamp(szuro.OldalMeret, 1, NaploSzuro.MaxOldalMeret);

        var lekerdezes = db.AuditNaplo.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(szuro.Entitas))
        {
            var entitas = szuro.Entitas.Trim();
            lekerdezes = lekerdezes.Where(a => a.Entitas == entitas);
        }
        if (szuro.EntitasId is { } entitasId)
            lekerdezes = lekerdezes.Where(a => a.EntitasId == entitasId);
        if (szuro.FelhasznaloId is { } felhasznaloId)
            lekerdezes = lekerdezes.Where(a => a.FelhasznaloId == felhasznaloId);
        if (szuro.Tol is { } tol)
            lekerdezes = lekerdezes.Where(a => a.Idopont >= tol);
        if (szuro.Ig is { } ig)
            lekerdezes = lekerdezes.Where(a => a.Idopont < ig);

        var osszesen = await lekerdezes.CountAsync(ct);
        var elemek = await lekerdezes
            .OrderByDescending(a => a.Idopont).ThenByDescending(a => a.Id)
            .Skip((oldal - 1) * meret)
            .Take(meret)
            .Select(NaploVetites)
            .ToListAsync(ct);

        return new LapozottLista<NaploElem>(elemek, oldal, meret, osszesen);
    }

    /// <summary>Az eszköz szereplése a leltárakban; a számítás ugyanaz, mint az összesítésben (D-036, D-037).</summary>
    private async Task<List<LeltarTortenetElem>> LeltarakAsync(long eszkozId, int torzsMennyiseg, CancellationToken ct)
    {
        var elvartak = await db.ElvartTetelek.AsNoTracking()
            .Where(t => t.EszkozId == eszkozId)
            .Select(t => new { t.LeltarIdoszakId, t.LeltarkorzetId, t.Leltarkorzet.Kod, t.ElvartMennyiseg })
            .ToListAsync(ct);
        var olvasasok = await db.Leolvasasok.AsNoTracking()
            .Where(l => l.EszkozId == eszkozId && !l.Sztornozva)
            .Select(l => new
            {
                l.LeltarIdoszakId,
                l.AktivKorzetId,
                KorzetKod = l.AktivKorzet.Kod,
                l.HelyisegId,
                Helyiseg = l.Helyiseg != null ? l.Helyiseg.Epulet + " " + l.Helyiseg.Szobaszam : null,
                l.Mennyiseg,
                l.Minosites,
                l.Idopont,
            })
            .ToListAsync(ct);

        var idoszakIdk = elvartak.Select(e => e.LeltarIdoszakId).Concat(olvasasok.Select(o => o.LeltarIdoszakId)).Distinct().ToList();
        var idoszakok = await db.LeltarIdoszakok.AsNoTracking()
            .Where(i => idoszakIdk.Contains(i.Id))
            .OrderBy(i => i.Kezdete).ThenBy(i => i.Id)
            .Select(i => new { i.Id, i.Megnevezes, i.Statusz, i.Kezdete })
            .ToListAsync(ct);

        var leltarak = new List<LeltarTortenetElem>();
        foreach (var idoszak in idoszakok)
        {
            var elvart = elvartak.SingleOrDefault(e => e.LeltarIdoszakId == idoszak.Id);
            var sajat = olvasasok.Where(o => o.LeltarIdoszakId == idoszak.Id).ToList();
            var (beolvasott, sajatKorzetben) = OsszesitesSzolgaltatas.Szamol(
                elvart?.ElvartMennyiseg ?? torzsMennyiseg,
                elvart?.LeltarkorzetId,
                sajat.Select(o => new OsszesitesSzolgaltatas.Talalat(o.AktivKorzetId, o.Mennyiseg, o.Minosites)).ToList());

            leltarak.Add(new LeltarTortenetElem(
                idoszak.Id,
                idoszak.Megnevezes,
                idoszak.Statusz,
                idoszak.Kezdete,
                elvart?.Kod,
                elvart?.ElvartMennyiseg ?? 0,
                beolvasott,
                OsszesitesSzolgaltatas.Allapot(elvart is not null, elvart?.ElvartMennyiseg ?? 0, beolvasott, sajatKorzetben),
                sajat.Select(o => o.KorzetKod).Distinct().Order().ToList(),
                sajat.Where(o => o.Minosites != Minosites.Ismetelt)
                    .GroupBy(o => new { o.HelyisegId, o.Helyiseg })
                    .OrderBy(g => g.Key.Helyiseg)
                    .Select(g => new HelyisegMennyiseg(g.Key.HelyisegId, g.Key.Helyiseg, g.Sum(o => o.Mennyiseg)))
                    .ToList(),
                sajat.Select(o => (DateTime?)o.Idopont).Max()));
        }
        return leltarak;
    }

    /// <summary>Ki rögzítette a kézi áthelyezést vagy a felelősváltást: az audit napló létrehozási sorából.</summary>
    private async Task<Dictionary<long, string?>> RogzitokAsync(string entitas, List<long> idk, CancellationToken ct)
    {
        if (idk.Count == 0)
            return [];
        var sorok = await db.AuditNaplo.AsNoTracking()
            .Where(a => a.Entitas == entitas && idk.Contains(a.EntitasId) && a.Muvelet == NaploMuveletek.Letrehozas)
            .Select(a => new { a.EntitasId, Nev = a.Felhasznalo != null ? a.Felhasznalo.Nev : null })
            .ToListAsync(ct);
        return sorok.GroupBy(s => s.EntitasId).ToDictionary(g => g.Key, g => g.First().Nev);
    }
}
