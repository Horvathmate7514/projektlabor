using System.Security.Claims;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

/// <summary>
/// Az eszköz helyének, felelősének, típusának és állapotának módosítása (UC-12, UC-13).
/// A hely és a felelős érvényességi időszakkal tárolódik (D-011), az állapotváltás külön eseményként (F-26).
/// </summary>
public class EszkozKezelesSzolgaltatas(LeltarDbContext db, KorzetJogosultsag jogosultsag, TimeProvider ido)
{
    public async Task<EszkozHozzarendelesek> HozzarendelesekAsync(long eszkozId, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        await EszkozLathatosag.EllenorzesAsync(db, jogosultsag, eszkozId, felhasznalo, ct);
        return await HozzarendelesekAsync(eszkozId, ct);
    }

    public async Task<EszkozHozzarendelesek> HelyAsync(long eszkozId, HelyKeres keres, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var eszkoz = await EszkozAsync(eszkozId, felhasznalo, ct);
        if (keres.HelyisegId is { } helyisegId)
        {
            if (eszkoz.EszkozAllapot.Megszunt)
                throw UzletiHiba.Hibas("Megszűnt állapotú eszköznek nem adható meg hely.");
            if (!await db.Helyisegek.AnyAsync(h => h.Id == helyisegId && h.Aktiv, ct))
                throw UzletiHiba.Hibas("A kiválasztott helyiség nem létezik vagy nem aktív.");
        }

        if (await LeolvasasKovetes.AthelyezesAsync(db, eszkozId, keres.HelyisegId, ElhelyezesForras.Kezi, null, Most(), ct))
            await db.SaveChangesAsync(ct);
        return await HozzarendelesekAsync(eszkozId, ct);
    }

    public async Task<EszkozHozzarendelesek> FelelosAsync(long eszkozId, FelelosKeres keres, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var eszkoz = await EszkozAsync(eszkozId, felhasznalo, ct);
        if (keres.FelelosId is { } felelosId)
        {
            if (eszkoz.EszkozAllapot.Megszunt)
                throw UzletiHiba.Hibas("Megszűnt állapotú eszközhöz nem rendelhető felelős.");
            if (!await db.FelelosSzemelyek.AnyAsync(f => f.Id == felelosId && f.Aktiv, ct))
                throw UzletiHiba.Hibas("A kiválasztott felelős személy nem létezik vagy nem aktív.");
        }

        var aktualis = await db.FelelosHozzarendelesek.SingleOrDefaultAsync(f => f.EszkozId == eszkozId && f.ErvenyesIg == null, ct);
        if (aktualis?.FelelosId != keres.FelelosId)
        {
            var most = Most();
            if (aktualis is not null)
            {
                most = Idopontok.Kovetkezo(aktualis.ErvenyesTol, most);
                aktualis.ErvenyesIg = most;
            }
            if (keres.FelelosId is { } uj)
                db.FelelosHozzarendelesek.Add(new FelelosHozzarendeles { EszkozId = eszkozId, FelelosId = uj, ErvenyesTol = most });
            await db.SaveChangesAsync(ct);
        }
        return await HozzarendelesekAsync(eszkozId, ct);
    }

    public async Task<EszkozHozzarendelesek> TipusAsync(long eszkozId, TipusKeres keres, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var eszkoz = await EszkozAsync(eszkozId, felhasznalo, ct);
        if (keres.EszkozTipusId is { } tipusId && !await db.EszkozTipusok.AnyAsync(t => t.Id == tipusId && t.Aktiv, ct))
            throw UzletiHiba.Hibas("A kiválasztott eszköztípus nem létezik vagy nem aktív.");

        if (eszkoz.EszkozTipusId != keres.EszkozTipusId)
        {
            eszkoz.EszkozTipusId = keres.EszkozTipusId;
            await db.SaveChangesAsync(ct);
        }
        return await HozzarendelesekAsync(eszkozId, ct);
    }

    /// <summary>
    /// Állapotváltás indoklással. Megszűnt állapotba lépéskor az eszköz aktuális helye és felelőse lezárul (D-045):
    /// a történet megmarad, de egy selejtezett vagy elveszett eszköz nem áll helyiségben, és nincs gazdája.
    /// </summary>
    public async Task<EszkozHozzarendelesek> AllapotAsync(long eszkozId, AllapotKeres keres, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var indoklas = keres.Indoklas?.Trim() ?? "";
        if (indoklas.Length == 0)
            throw UzletiHiba.Hibas("Az állapotváltozás indoklása kötelező.");
        if (indoklas.Length > AllapotKeres.MaxIndoklasHossz)
            throw UzletiHiba.Hibas($"Az indoklás legfeljebb {AllapotKeres.MaxIndoklasHossz} karakter lehet.");
        var ugyiratszam = string.IsNullOrWhiteSpace(keres.Ugyiratszam) ? null : keres.Ugyiratszam.Trim();
        if (ugyiratszam is { Length: > AllapotKeres.MaxUgyiratszamHossz })
            throw UzletiHiba.Hibas($"Az ügyiratszám legfeljebb {AllapotKeres.MaxUgyiratszamHossz} karakter lehet.");

        var eszkoz = await EszkozAsync(eszkozId, felhasznalo, ct);
        var uj = await db.EszkozAllapotok.AsNoTracking().SingleOrDefaultAsync(a => a.Id == keres.EszkozAllapotId, ct)
            ?? throw UzletiHiba.Hibas("A kiválasztott eszközállapot nem létezik.");
        if (uj.Id == eszkoz.EszkozAllapotId)
            throw UzletiHiba.Utkozes($"Az eszköz már „{uj.Nev}” állapotban van.");

        var most = Most();
        var felhasznaloId = felhasznalo.FelhasznaloId();
        db.AllapotValtozasok.Add(new AllapotValtozas
        {
            EszkozId = eszkozId,
            RegiAllapotId = eszkoz.EszkozAllapotId,
            UjAllapotId = uj.Id,
            Indoklas = indoklas,
            Ugyiratszam = ugyiratszam,
            FelhasznaloId = felhasznaloId,
            Idopont = most,
        });
        eszkoz.EszkozAllapotId = uj.Id;
        eszkoz.Modositva = most;
        eszkoz.ModositottaId = felhasznaloId;

        if (uj.Megszunt)
        {
            await LeolvasasKovetes.AthelyezesAsync(db, eszkozId, null, ElhelyezesForras.Kezi, null, most, ct);
            if (await db.FelelosHozzarendelesek.SingleOrDefaultAsync(f => f.EszkozId == eszkozId && f.ErvenyesIg == null, ct) is { } felelos)
                felelos.ErvenyesIg = Idopontok.Kovetkezo(felelos.ErvenyesTol, most);
        }

        await db.SaveChangesAsync(ct);
        return await HozzarendelesekAsync(eszkozId, ct);
    }

    private DateTime Most() => Idopontok.Ezredre(ido.GetUtcNow().UtcDateTime);

    private async Task<Eszkoz> EszkozAsync(long eszkozId, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        db.AktualisFelhasznaloId ??= felhasznalo.FelhasznaloId();
        return await db.Eszkozok.Include(e => e.EszkozAllapot).SingleOrDefaultAsync(e => e.Id == eszkozId, ct)
            ?? throw UzletiHiba.NemTalalhato($"Nincs {eszkozId} azonosítójú eszköz.");
    }

    private async Task<EszkozHozzarendelesek> HozzarendelesekAsync(long eszkozId, CancellationToken ct)
    {
        var eszkoz = await db.Eszkozok.AsNoTracking()
            .Where(e => e.Id == eszkozId)
            .Select(e => new
            {
                e.EszkozTipusId,
                Tipus = e.EszkozTipus != null ? e.EszkozTipus.Nev : null,
                e.EszkozAllapotId,
                Allapot = e.EszkozAllapot.Nev,
                e.EszkozAllapot.Megszunt,
            })
            .SingleAsync(ct);
        var hely = await db.Elhelyezesek.AsNoTracking()
            .Where(h => h.EszkozId == eszkozId && h.ErvenyesIg == null)
            .Select(h => new { h.HelyisegId, Nev = h.Helyiseg.Epulet + " " + h.Helyiseg.Szobaszam, h.ErvenyesTol, h.Forras })
            .SingleOrDefaultAsync(ct);
        var felelos = await db.FelelosHozzarendelesek.AsNoTracking()
            .Where(f => f.EszkozId == eszkozId && f.ErvenyesIg == null)
            .Select(f => new { f.FelelosId, f.Felelos.Nev, f.ErvenyesTol })
            .SingleOrDefaultAsync(ct);

        return new EszkozHozzarendelesek(
            eszkozId,
            eszkoz.EszkozTipusId, eszkoz.Tipus,
            hely?.HelyisegId, hely?.Nev, hely?.ErvenyesTol, hely?.Forras,
            felelos?.FelelosId, felelos?.Nev, felelos?.ErvenyesTol,
            eszkoz.EszkozAllapotId, eszkoz.Allapot, eszkoz.Megszunt);
    }
}
