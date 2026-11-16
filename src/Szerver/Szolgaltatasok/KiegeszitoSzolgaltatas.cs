using System.Security.Claims;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

/// <summary>
/// Fő eszköz és kiegészítő kapcsolata, valamint a kiegészítők ellenőrzése a leltárban (F-19–F-21, D-044).
/// A saját kódú kiegészítő attól lesz közvetlenül ellenőrzött, hogy a kódját beolvassák; a kód nélküli
/// tartozékot a leltározó kézzel erősítheti meg.
/// </summary>
public class KiegeszitoSzolgaltatas(LeltarDbContext db, KorzetJogosultsag jogosultsag, TimeProvider ido)
{
    public async Task<IReadOnlyList<KiegeszitoAdat>> EszkozKiegeszitoiAsync(
        long eszkozId, long? leltarIdoszakId, bool lezartakkal, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        await EszkozLathatosag.EllenorzesAsync(db, jogosultsag, eszkozId, felhasznalo, ct);

        var ellenorzesek = new Dictionary<long, KiegeszitoEllenorzesMod>();
        if (leltarIdoszakId is { } idoszakId)
        {
            if (!await db.LeltarIdoszakok.AnyAsync(i => i.Id == idoszakId, ct))
                throw UzletiHiba.NemTalalhato($"Nincs {idoszakId} azonosítójú leltáridőszak.");

            var sorok = await db.KiegeszitoEllenorzesek.AsNoTracking()
                .Where(e => e.Kiegeszito.FoEszkozId == eszkozId && e.Leolvasas.LeltarIdoszakId == idoszakId && !e.Leolvasas.Sztornozva)
                .Select(e => new { e.KiegeszitoId, e.Mod })
                .ToListAsync(ct);
            ellenorzesek = sorok.GroupBy(s => s.KiegeszitoId).ToDictionary(
                g => g.Key,
                g => g.Any(s => s.Mod == KiegeszitoEllenorzesMod.Kozvetlen)
                    ? KiegeszitoEllenorzesMod.Kozvetlen
                    : KiegeszitoEllenorzesMod.Feltetelezett);
        }

        var kiegeszitok = await Vetites(db.Kiegeszitok.AsNoTracking()
                .Where(k => k.FoEszkozId == eszkozId && (lezartakkal || k.ErvenyesIg == null))
                .OrderBy(k => k.Id))
            .ToListAsync(ct);
        return kiegeszitok
            .Select(k => k with { Ellenorzes = ellenorzesek.TryGetValue(k.Id, out var mod) ? mod : null })
            .ToList();
    }

    public async Task<IReadOnlyList<KiegeszitoAdat>> LeolvasasKiegeszitoiAsync(long leolvasasId, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var leolvasas = await db.Leolvasasok.AsNoTracking()
            .Where(l => l.Id == leolvasasId)
            .Select(l => new { l.AktivKorzetId, l.FelhasznaloId })
            .SingleOrDefaultAsync(ct)
            ?? throw UzletiHiba.NemTalalhato($"Nincs {leolvasasId} azonosítójú leolvasás.");
        if (leolvasas.FelhasznaloId != felhasznalo.FelhasznaloId()
            && !await jogosultsag.LeltarozhatAsync(felhasznalo, leolvasas.AktivKorzetId, ct))
            throw UzletiHiba.Tiltott("Nincs jogosultságod ennek a leolvasásnak a megtekintéséhez.");

        var sorok = await db.KiegeszitoEllenorzesek.AsNoTracking()
            .Where(e => e.LeolvasasId == leolvasasId)
            .OrderBy(e => e.KiegeszitoId)
            .Select(e => new
            {
                e.Mod,
                k = new KiegeszitoAdat(
                    e.Kiegeszito.Id,
                    e.Kiegeszito.FoEszkozId,
                    e.Kiegeszito.KiegeszitoEszkozId,
                    e.Kiegeszito.KiegeszitoEszkoz != null ? e.Kiegeszito.KiegeszitoEszkoz.Megnevezes : e.Kiegeszito.Leiras ?? "",
                    e.Kiegeszito.Mennyiseg,
                    e.Kiegeszito.ErvenyesTol,
                    e.Kiegeszito.ErvenyesIg,
                    null),
            })
            .ToListAsync(ct);
        return sorok.Select(s => s.k with { Ellenorzes = s.Mod }).ToList();
    }

    /// <summary>Kód nélküli tartozék kézi megerősítése (vagy a megerősítés visszavonása) a leltározó saját leolvasásánál.</summary>
    public async Task<IReadOnlyList<KiegeszitoAdat>> EllenorzesAsync(
        long leolvasasId, long kiegeszitoId, KiegeszitoEllenorzesKeres keres, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var sor = await db.KiegeszitoEllenorzesek
            .Include(e => e.Leolvasas).ThenInclude(l => l.LeltarIdoszak)
            .Include(e => e.Kiegeszito)
            .SingleOrDefaultAsync(e => e.LeolvasasId == leolvasasId && e.KiegeszitoId == kiegeszitoId, ct)
            ?? throw UzletiHiba.NemTalalhato("A leolvasáshoz nem tartozik ilyen kiegészítő.");

        if (sor.Leolvasas.FelhasznaloId != felhasznalo.FelhasznaloId() && !felhasznalo.MindenKorzetben())
            throw UzletiHiba.Tiltott("Csak a saját leolvasásodnál erősíthetsz meg kiegészítőt.");
        if (sor.Leolvasas.LeltarIdoszak.Statusz != LeltarIdoszakStatusz.Nyitott)
            throw UzletiHiba.Utkozes("A lezárt leltár csak olvasható; a kiegészítők ellenőrzése már nem módosítható.");
        if (sor.Leolvasas.Sztornozva)
            throw UzletiHiba.Utkozes("Sztornózott leolvasás kiegészítője nem módosítható.");
        if (sor.Kiegeszito.KiegeszitoEszkozId is not null)
            throw UzletiHiba.Hibas("Ennek a kiegészítőnek saját kódja van: akkor lesz közvetlenül ellenőrzött, ha a kódját beolvasod.");

        sor.Mod = keres.Kozvetlen ? KiegeszitoEllenorzesMod.Kozvetlen : KiegeszitoEllenorzesMod.Feltetelezett;
        await db.SaveChangesAsync(ct);
        return await LeolvasasKiegeszitoiAsync(leolvasasId, felhasznalo, ct);
    }

    public async Task<KiegeszitoAdat> FelvetelAsync(long eszkozId, KiegeszitoFelvetel keres, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        db.AktualisFelhasznaloId ??= felhasznalo.FelhasznaloId();

        var leiras = string.IsNullOrWhiteSpace(keres.Leiras) ? null : keres.Leiras.Trim();
        if (leiras is { Length: > KiegeszitoFelvetel.MaxLeirasHossz })
            throw UzletiHiba.Hibas($"A leírás legfeljebb {KiegeszitoFelvetel.MaxLeirasHossz} karakter lehet.");
        if (keres.KiegeszitoEszkozId is null && leiras is null)
            throw UzletiHiba.Hibas("Add meg a kiegészítő eszközt vagy a tartozék leírását.");
        if (keres.Mennyiseg is < 1 or > KiegeszitoFelvetel.MaxMennyiseg)
            throw UzletiHiba.Hibas($"A mennyiség 1 és {KiegeszitoFelvetel.MaxMennyiseg} között lehet.");

        var megszunt = await db.Eszkozok.Where(e => e.Id == eszkozId).Select(e => (bool?)e.EszkozAllapot.Megszunt).SingleOrDefaultAsync(ct)
            ?? throw UzletiHiba.NemTalalhato($"Nincs {eszkozId} azonosítójú eszköz.");
        if (megszunt)
            throw UzletiHiba.Hibas("Megszűnt állapotú eszközhöz nem vehető fel kiegészítő.");

        if (keres.KiegeszitoEszkozId is { } kiegeszitoEszkozId)
        {
            if (kiegeszitoEszkozId == eszkozId)
                throw UzletiHiba.Hibas("Az eszköz nem lehet saját maga kiegészítője.");
            if (keres.Mennyiseg != 1)
                throw UzletiHiba.Hibas("Saját kódú kiegészítő eszköznél a mennyiség 1.");
            var kiegeszitoMegszunt = await db.Eszkozok.Where(e => e.Id == kiegeszitoEszkozId)
                .Select(e => (bool?)e.EszkozAllapot.Megszunt).SingleOrDefaultAsync(ct)
                ?? throw UzletiHiba.Hibas("A kiválasztott kiegészítő eszköz nem létezik.");
            if (kiegeszitoMegszunt)
                throw UzletiHiba.Hibas("Megszűnt állapotú eszköz nem vehető fel kiegészítőként.");
            if (await db.Kiegeszitok.AnyAsync(k => k.KiegeszitoEszkozId == kiegeszitoEszkozId && k.ErvenyesIg == null, ct))
                throw UzletiHiba.Utkozes("Az eszköz már egy fő eszköz kiegészítője; előbb zárd le azt a kapcsolatot (F-21).");
            if (await db.Kiegeszitok.AnyAsync(k => k.FoEszkozId == kiegeszitoEszkozId && k.KiegeszitoEszkozId == eszkozId && k.ErvenyesIg == null, ct))
                throw UzletiHiba.Hibas("A két eszköz nem lehet egymás kiegészítője.");
        }

        var kiegeszito = new Kiegeszito
        {
            FoEszkozId = eszkozId,
            KiegeszitoEszkozId = keres.KiegeszitoEszkozId,
            Leiras = leiras,
            Mennyiseg = keres.Mennyiseg,
            ErvenyesTol = Most(),
        };
        db.Kiegeszitok.Add(kiegeszito);
        await db.SaveChangesAsync(ct);

        return await Vetites(db.Kiegeszitok.AsNoTracking().Where(k => k.Id == kiegeszito.Id)).SingleAsync(ct);
    }

    /// <summary>A kapcsolat nem törlődik, csak lezárul: a korábbi leltárak ellenőrzései így is értelmezhetők maradnak.</summary>
    public async Task<KiegeszitoAdat> LezarasAsync(long kiegeszitoId, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        db.AktualisFelhasznaloId ??= felhasznalo.FelhasznaloId();

        var kiegeszito = await db.Kiegeszitok.SingleOrDefaultAsync(k => k.Id == kiegeszitoId, ct)
            ?? throw UzletiHiba.NemTalalhato($"Nincs {kiegeszitoId} azonosítójú kiegészítő-kapcsolat.");
        if (kiegeszito.ErvenyesIg is not null)
            throw UzletiHiba.Utkozes("A kiegészítő-kapcsolat már le van zárva.");

        kiegeszito.ErvenyesIg = Idopontok.Kovetkezo(kiegeszito.ErvenyesTol, Most());
        await db.SaveChangesAsync(ct);

        return await Vetites(db.Kiegeszitok.AsNoTracking().Where(k => k.Id == kiegeszitoId)).SingleAsync(ct);
    }

    private DateTime Most() => Idopontok.Ezredre(ido.GetUtcNow().UtcDateTime);

    private static IQueryable<KiegeszitoAdat> Vetites(IQueryable<Kiegeszito> kiegeszitok) =>
        kiegeszitok.Select(k => new KiegeszitoAdat(
            k.Id,
            k.FoEszkozId,
            k.KiegeszitoEszkozId,
            k.KiegeszitoEszkoz != null ? k.KiegeszitoEszkoz.Megnevezes : k.Leiras ?? "",
            k.Mennyiseg,
            k.ErvenyesTol,
            k.ErvenyesIg,
            null));
}
