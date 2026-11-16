using System.Security.Claims;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

/// <summary>
/// Ami egy beolvasásból a leolvasáson túl következik: az eszköz helytörténete (F-23, D-042) és a kiegészítők
/// ellenőrzése (F-20, D-044). A változásokat a hívó menti, a leolvasással egy mentésben.
/// </summary>
public static class LeolvasasKovetes
{
    /// <summary>A leolvasás bizonyítja-e, hogy az eszköz megvan (az ismeretlen kód és a megszűnt eszköz nem).</summary>
    public static bool EszkoztIgazol(Minosites minosites) =>
        minosites is Minosites.Ok or Minosites.MasKorzet or Minosites.Tobblet or Minosites.Ismetelt;

    public static async Task BeolvasaskorAsync(LeltarDbContext db, Leolvasas leolvasas, CancellationToken ct)
    {
        if (leolvasas.EszkozId is not { } eszkozId || !EszkoztIgazol(leolvasas.Minosites))
            return;

        if (leolvasas.HelyisegId is { } helyisegId)
            await AthelyezesAsync(db, eszkozId, helyisegId, ElhelyezesForras.Leltar, leolvasas, leolvasas.Idopont, ct);

        var idoszakId = leolvasas.LeltarIdoszakId;

        // A beolvasott eszköz mint fő eszköz: a kiegészítői feltételezetten megvannak, kivéve, amelyiket már beolvasták.
        var kiegeszitok = await db.Kiegeszitok.AsNoTracking()
            .Where(k => k.FoEszkozId == eszkozId && k.ErvenyesIg == null)
            .Select(k => new { k.Id, k.KiegeszitoEszkozId })
            .ToListAsync(ct);
        if (kiegeszitok.Count > 0)
        {
            var eszkozIdk = kiegeszitok.Where(k => k.KiegeszitoEszkozId != null).Select(k => k.KiegeszitoEszkozId!.Value).ToList();
            var beolvasottak = eszkozIdk.Count == 0
                ? []
                : await IgazoloLeolvasasok(db, idoszakId)
                    .Where(l => eszkozIdk.Contains(l.EszkozId!.Value))
                    .Select(l => l.EszkozId!.Value)
                    .Distinct()
                    .ToListAsync(ct);

            foreach (var k in kiegeszitok)
                db.KiegeszitoEllenorzesek.Add(new KiegeszitoEllenorzes
                {
                    Leolvasas = leolvasas,
                    KiegeszitoId = k.Id,
                    Mod = k.KiegeszitoEszkozId is { } sajat && beolvasottak.Contains(sajat)
                        ? KiegeszitoEllenorzesMod.Kozvetlen
                        : KiegeszitoEllenorzesMod.Feltetelezett,
                });
        }

        // A beolvasott eszköz mint kiegészítő: ami eddig csak feltételezett volt, közvetlenül ellenőrzött lesz.
        var feltetelezettek = await db.KiegeszitoEllenorzesek
            .Where(e => e.Mod == KiegeszitoEllenorzesMod.Feltetelezett
                && e.Kiegeszito.KiegeszitoEszkozId == eszkozId
                && e.Leolvasas.LeltarIdoszakId == idoszakId)
            .ToListAsync(ct);
        foreach (var e in feltetelezettek)
            e.Mod = KiegeszitoEllenorzesMod.Kozvetlen;
    }

    public static async Task SztornokorAsync(LeltarDbContext db, Leolvasas leolvasas, DateTime most, CancellationToken ct)
    {
        if (leolvasas.EszkozId is not { } eszkozId)
            return;

        // Ha az eszköz még azon a helyen áll, ahová ez a leolvasás tette, visszakerül az előző helyére.
        var hely = await db.Elhelyezesek.SingleOrDefaultAsync(e => e.LeolvasasId == leolvasas.Id && e.ErvenyesIg == null, ct);
        if (hely is not null)
        {
            var elozo = await db.Elhelyezesek.AsNoTracking()
                .Where(e => e.EszkozId == eszkozId && e.Id != hely.Id && e.ErvenyesIg == hely.ErvenyesTol && e.Helyiseg.Aktiv)
                .OrderByDescending(e => e.Id)
                .FirstOrDefaultAsync(ct);

            var valtas = Idopontok.Kovetkezo(hely.ErvenyesTol, most);
            hely.ErvenyesIg = valtas;
            if (elozo is not null)
                db.Elhelyezesek.Add(new Elhelyezes
                {
                    EszkozId = eszkozId,
                    HelyisegId = elozo.HelyisegId,
                    ErvenyesTol = valtas,
                    Forras = elozo.Forras,
                    LeolvasasId = elozo.LeolvasasId,
                });
        }

        // Ha a kiegészítőnek nem maradt érvényes leolvasása, a közvetlen ellenőrzés visszaáll feltételezettre.
        var maradt = await IgazoloLeolvasasok(db, leolvasas.LeltarIdoszakId)
            .AnyAsync(l => l.Id != leolvasas.Id && l.EszkozId == eszkozId, ct);
        if (maradt)
            return;

        var kozvetlenek = await db.KiegeszitoEllenorzesek
            .Where(e => e.Mod == KiegeszitoEllenorzesMod.Kozvetlen
                && e.Kiegeszito.KiegeszitoEszkozId == eszkozId
                && e.Leolvasas.LeltarIdoszakId == leolvasas.LeltarIdoszakId)
            .ToListAsync(ct);
        foreach (var e in kozvetlenek)
            e.Mod = KiegeszitoEllenorzesMod.Feltetelezett;
    }

    /// <summary>
    /// Lezárja az eszköz aktuális helyét, és megnyitja az újat; üres helyiséggel csak lezár.
    /// Hamis, ha az eszköz már a kért helyen van.
    /// </summary>
    public static async Task<bool> AthelyezesAsync(
        LeltarDbContext db, long eszkozId, long? helyisegId, ElhelyezesForras forras, Leolvasas? leolvasas, DateTime most,
        CancellationToken ct)
    {
        var aktualis = await db.Elhelyezesek.SingleOrDefaultAsync(e => e.EszkozId == eszkozId && e.ErvenyesIg == null, ct);
        if (aktualis?.HelyisegId == helyisegId)
            return false;

        most = Idopontok.Ezredre(most);
        if (aktualis is not null)
        {
            most = Idopontok.Kovetkezo(aktualis.ErvenyesTol, most);
            aktualis.ErvenyesIg = most;
        }
        if (helyisegId is { } uj)
            db.Elhelyezesek.Add(new Elhelyezes
            {
                EszkozId = eszkozId,
                HelyisegId = uj,
                ErvenyesTol = most,
                Forras = forras,
                Leolvasas = leolvasas,
            });
        return true;
    }

    private static IQueryable<Leolvasas> IgazoloLeolvasasok(LeltarDbContext db, long idoszakId) =>
        db.Leolvasasok.Where(l => l.LeltarIdoszakId == idoszakId && !l.Sztornozva && l.EszkozId != null
            && l.Minosites != Minosites.IsmeretlenKod && l.Minosites != Minosites.NemAktiv);
}

/// <summary>Az érvényességi időszakok időpontjai: az adatbázis ezredmásodpercre tárol, és a vég mindig későbbi a kezdetnél.</summary>
public static class Idopontok
{
    public static DateTime Ezredre(DateTime idopont) =>
        new(idopont.Ticks - idopont.Ticks % TimeSpan.TicksPerMillisecond, idopont.Kind);

    /// <summary>A váltás időpontja: a mostani idő, de legalább egy ezredmásodperccel a lezárt időszak kezdete után.</summary>
    public static DateTime Kovetkezo(DateTime regiKezdet, DateTime most)
    {
        most = Ezredre(most);
        return most > regiKezdet ? most : Ezredre(regiKezdet).AddMilliseconds(1);
    }
}

internal static class EszkozLathatosag
{
    /// <summary>404, ha nincs ilyen eszköz; 403, ha a felhasználó nem láthatja a körzetét (jogosultsági mátrix).</summary>
    public static async Task EllenorzesAsync(
        LeltarDbContext db, KorzetJogosultsag jogosultsag, long eszkozId, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var korzetId = await db.Eszkozok.Where(e => e.Id == eszkozId).Select(e => (long?)e.LeltarkorzetId).SingleOrDefaultAsync(ct)
            ?? throw UzletiHiba.NemTalalhato($"Nincs {eszkozId} azonosítójú eszköz.");
        if (await jogosultsag.LathatoKorzetekAsync(felhasznalo, ct) is { } lathato && !lathato.Contains(korzetId))
            throw UzletiHiba.Tiltott("Nincs jogosultságod ennek az eszköznek a megtekintéséhez.");
    }
}
