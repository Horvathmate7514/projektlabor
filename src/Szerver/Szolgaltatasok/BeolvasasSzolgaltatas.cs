using System.Security.Claims;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

/// <summary>
/// A leltározás alapfolyamata (UC-06): a beolvasott kód azonosítása bármely aktív eszközkód alapján, a leolvasás
/// minősítése és rögzítése, valamint a sztornó (D-002, F-10 – F-15).
/// </summary>
public class BeolvasasSzolgaltatas(LeltarDbContext db, KorzetJogosultsag jogosultsag, TimeProvider ido)
{
    /// <summary>Ugyanaz a szabály, mint az adatbázis <c>ErtekNorm</c> oszlopáé: szóközök le, nagybetű (D-012, F-08).</summary>
    public static string Normalizal(string? kod) => (kod ?? "").Trim().ToUpperInvariant();

    /// <summary>
    /// A minősítés szabályai, elsőbbségi sorrendben (D-027). Az <paramref name="eddigBeolvasott"/> a korábbi,
    /// nem sztornózott és nem <c>ISMETELT</c> leolvasások mennyiségének összege (D-025).
    /// </summary>
    public static Minosites Minosit(bool megszunt, bool masKorzet, int elvart, int eddigBeolvasott, int mennyiseg)
    {
        if (elvart == 1 && eddigBeolvasott >= 1)
            return Minosites.Ismetelt; // F-12: egyedi eszköz nem számolható kétszer
        if (megszunt)
            return Minosites.NemAktiv; // UC-06 6d: rögzítjük, a leltárfelelős dönt
        if (masKorzet)
            return Minosites.MasKorzet; // F-14: megerősítés nélkül, jelölve
        if (eddigBeolvasott + mennyiseg > elvart)
            return Minosites.Tobblet; // F-13: nem utasítjuk el
        return Minosites.Ok;
    }

    public async Task<BeolvasasEredmeny> BeolvasasAsync(
        long idoszakId, BeolvasasKeres keres, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var kod = Normalizal(keres.Kod);
        if (kod.Length == 0)
            throw UzletiHiba.Hibas("Üres kód nem olvasható be.");
        if (kod.Length > BeolvasasKeres.MaxKodHossz)
            throw UzletiHiba.Hibas($"A kód legfeljebb {BeolvasasKeres.MaxKodHossz} karakter lehet.");
        if (keres.Mennyiseg is < 1 or > BeolvasasKeres.MaxMennyiseg)
            throw UzletiHiba.Hibas($"A mennyiség 1 és {BeolvasasKeres.MaxMennyiseg} között lehet.");
        if (!Enum.IsDefined(keres.BeviteliMod))
            throw UzletiHiba.Hibas("Ismeretlen beviteli mód.");

        await NyitottIdoszakEllenorzeseAsync(idoszakId, ct);

        var korzet = await db.Leltarkorzetek.AsNoTracking().SingleOrDefaultAsync(k => k.Id == keres.AktivKorzetId, ct);
        if (korzet is not { Aktiv: true })
            throw UzletiHiba.Hibas("A kiválasztott leltárkörzet nem létezik vagy nem aktív.");
        if (!await jogosultsag.LeltarozhatAsync(felhasznalo, korzet.Id, ct))
            throw UzletiHiba.Tiltott($"Nincs jogosultságod a(z) {korzet.Kod} körzetben leltározni.");
        if (keres.HelyisegId is { } helyisegId && !await db.Helyisegek.AnyAsync(h => h.Id == helyisegId && h.Aktiv, ct))
            throw UzletiHiba.Hibas("A kiválasztott helyiség nem létezik vagy nem aktív.");

        var leolvasas = new Leolvasas
        {
            LeltarIdoszakId = idoszakId,
            BeolvasottKod = keres.Kod!.Trim(),
            AktivKorzetId = korzet.Id,
            HelyisegId = keres.HelyisegId,
            Mennyiseg = keres.Mennyiseg,
            BeviteliMod = keres.BeviteliMod,
            FelhasznaloId = felhasznalo.FelhasznaloId(),
            Idopont = ido.GetUtcNow().UtcDateTime,
        };

        // Az aktív kódok normalizált értéke egyedi (szűrt egyedi index), így legfeljebb egy találat lehet (F-07).
        var talalat = await db.EszkozKodok.AsNoTracking()
            .Where(k => k.Aktiv && k.ErtekNorm == kod)
            .Select(k => new
            {
                KodId = k.Id,
                k.EszkozId,
                KodTipus = k.KodTipus.Nev,
                k.Eszkoz.Megnevezes,
                k.Eszkoz.LeltarkorzetId,
                KorzetKod = k.Eszkoz.Leltarkorzet.Kod,
                KorzetNev = k.Eszkoz.Leltarkorzet.Nev,
                Allapot = k.Eszkoz.EszkozAllapot.Nev,
                k.Eszkoz.EszkozAllapot.Megszunt,
                k.Eszkoz.ElvartMennyiseg,
                k.Eszkoz.MennyisegiEgyseg,
            })
            .SingleOrDefaultAsync(ct);

        if (talalat is null)
        {
            leolvasas.Minosites = Minosites.IsmeretlenKod; // F-15: ez is rögzül, később kivizsgálható
            await MentesAsync(leolvasas, ct);
            return new BeolvasasEredmeny(leolvasas.Id, Minosites.IsmeretlenKod,
                $"Ismeretlen kód: {leolvasas.BeolvasottKod}. A leolvasás rögzítve, később kivizsgálható.",
                null, null, 0, null);
        }

        // Az elvárt körzet és mennyiség a nyitáskori pillanatképből jön (D-003); a nyitás után felvett eszköznél
        // a jelenlegi törzsadatból.
        var elvart = await db.ElvartTetelek.AsNoTracking()
            .Where(t => t.LeltarIdoszakId == idoszakId && t.EszkozId == talalat.EszkozId)
            .Select(t => new { t.LeltarkorzetId, t.Leltarkorzet.Kod, t.Leltarkorzet.Nev, t.ElvartMennyiseg })
            .SingleOrDefaultAsync(ct)
            ?? new { talalat.LeltarkorzetId, Kod = talalat.KorzetKod, Nev = talalat.KorzetNev, talalat.ElvartMennyiseg };

        var szamitott = db.Leolvasasok.AsNoTracking().Where(l =>
            l.LeltarIdoszakId == idoszakId && l.EszkozId == talalat.EszkozId &&
            !l.Sztornozva && l.Minosites != Minosites.Ismetelt);
        var eddigBeolvasott = await szamitott.SumAsync(l => l.Mennyiseg, ct);
        var korabbi = await szamitott.OrderBy(l => l.Idopont).Select(l => (DateTime?)l.Idopont).FirstOrDefaultAsync(ct);

        var minosites = Minosit(talalat.Megszunt, elvart.LeltarkorzetId != korzet.Id,
            elvart.ElvartMennyiseg, eddigBeolvasott, keres.Mennyiseg);

        leolvasas.EszkozId = talalat.EszkozId;
        leolvasas.EszkozKodId = talalat.KodId;
        leolvasas.Minosites = minosites;
        await MentesAsync(leolvasas, ct);

        var beolvasott = eddigBeolvasott + (minosites == Minosites.Ismetelt ? 0 : keres.Mennyiseg);
        var egyseg = talalat.MennyisegiEgyseg ?? "db";
        var uzenet = minosites switch
        {
            Minosites.Ok => $"Rendben: {talalat.Megnevezes} ({beolvasott}/{elvart.ElvartMennyiseg} {egyseg}).",
            Minosites.Ismetelt => $"Már beolvasva ebben a leltárban: {talalat.Megnevezes}. A darabszám nem nőtt.",
            Minosites.NemAktiv => $"Nem aktív eszköz került elő ({talalat.Allapot}): {talalat.Megnevezes}. " +
                                  "A leolvasás rögzítve, az állapotáról a leltárfelelős dönt.",
            Minosites.MasKorzet => $"Az eszköz a(z) {elvart.Kod} – {elvart.Nev} körzetbe tartozik: {talalat.Megnevezes}. " +
                                   "A leolvasás más körzetként rögzítve.",
            Minosites.Tobblet => $"Többlet: {talalat.Megnevezes} – elvárt {elvart.ElvartMennyiseg} {egyseg}, " +
                                 $"beolvasva {beolvasott} {egyseg}.",
            _ => throw new InvalidOperationException($"Váratlan minősítés: {minosites}"),
        };

        return new BeolvasasEredmeny(
            leolvasas.Id,
            minosites,
            uzenet,
            new BeolvasottEszkoz(talalat.EszkozId, talalat.Megnevezes, talalat.KodTipus, elvart.Kod, elvart.Nev,
                talalat.Allapot, talalat.MennyisegiEgyseg),
            elvart.ElvartMennyiseg,
            beolvasott,
            korabbi);
    }

    /// <summary>Az időszak leolvasásai, a legújabb elöl. A leltározó csak a jogosult körzeteiben rögzítetteket látja.</summary>
    public async Task<LapozottLista<LeolvasasListaElem>> ListazasAsync(
        long idoszakId, LeolvasasSzuro szuro, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        if (!await db.LeltarIdoszakok.AnyAsync(i => i.Id == idoszakId, ct))
            throw UzletiHiba.NemTalalhato($"Nincs {idoszakId} azonosítójú leltáridőszak.");

        var oldal = Math.Max(1, szuro.Oldal);
        var meret = Math.Clamp(szuro.OldalMeret, 1, LeolvasasSzuro.MaxOldalMeret);

        var lekerdezes = db.Leolvasasok.AsNoTracking().Where(l => l.LeltarIdoszakId == idoszakId);

        if (await jogosultsag.LeltarozhatoKorzetekAsync(felhasznalo, ct) is { } jogosult)
        {
            var korzetek = jogosult.ToList();
            lekerdezes = lekerdezes.Where(l => korzetek.Contains(l.AktivKorzetId));
        }
        if (szuro.LeltarkorzetId is { } korzetId)
            lekerdezes = lekerdezes.Where(l => l.AktivKorzetId == korzetId);
        if (szuro.CsakSajat)
        {
            var sajatId = felhasznalo.FelhasznaloId();
            lekerdezes = lekerdezes.Where(l => l.FelhasznaloId == sajatId);
        }
        if (!szuro.SztornozottakkalEgyutt)
            lekerdezes = lekerdezes.Where(l => !l.Sztornozva);

        var osszesen = await lekerdezes.CountAsync(ct);
        var elemek = await lekerdezes
            .OrderByDescending(l => l.Idopont).ThenByDescending(l => l.Id)
            .Skip((oldal - 1) * meret)
            .Take(meret)
            .Select(l => new LeolvasasListaElem(
                l.Id,
                l.Idopont,
                l.BeolvasottKod,
                l.Minosites,
                l.EszkozId,
                l.Eszkoz != null ? l.Eszkoz.Megnevezes : null,
                l.AktivKorzet.Kod,
                l.Mennyiseg,
                l.BeviteliMod,
                l.Felhasznalo.Nev,
                l.Sztornozva))
            .ToListAsync(ct);

        return new LapozottLista<LeolvasasListaElem>(elemek, oldal, meret, osszesen);
    }

    /// <summary>A leolvasás nem törölhető, csak indoklással sztornózható (F-11, UC-08).</summary>
    public async Task SztornoAsync(long leolvasasId, SztornoKeres keres, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var indoklas = keres.Indoklas?.Trim() ?? "";
        if (indoklas.Length == 0)
            throw UzletiHiba.Hibas("A sztornó indoklása kötelező.");
        if (indoklas.Length > SztornoKeres.MaxHossz)
            throw UzletiHiba.Hibas($"Az indoklás legfeljebb {SztornoKeres.MaxHossz} karakter lehet.");

        var leolvasas = await db.Leolvasasok.Include(l => l.LeltarIdoszak).SingleOrDefaultAsync(l => l.Id == leolvasasId, ct)
            ?? throw UzletiHiba.NemTalalhato($"Nincs {leolvasasId} azonosítójú leolvasás.");

        var sajatId = felhasznalo.FelhasznaloId();
        if (leolvasas.FelhasznaloId != sajatId && !felhasznalo.MindenKorzetben())
            throw UzletiHiba.Tiltott("Csak a saját leolvasásodat sztornózhatod.");
        if (leolvasas.LeltarIdoszak.Statusz != LeltarIdoszakStatusz.Nyitott)
            throw UzletiHiba.Utkozes("Csak nyitott leltáridőszak leolvasása sztornózható; a lezárt leltár csak olvasható.");
        if (leolvasas.Sztornozva)
            throw UzletiHiba.Utkozes("A leolvasás már sztornózva van.");

        leolvasas.Sztornozva = true;
        leolvasas.SztornoIndoklas = indoklas;
        leolvasas.SztornoIdopont = ido.GetUtcNow().UtcDateTime;
        leolvasas.SztornoztaId = sajatId;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Tesztkód a vonalkódolvasó nélküli bemutatóhoz (UC-06 5c). Érvényes kódnál lehetőleg a körzet egy még be
    /// nem olvasott eszközét adja; érvénytelennél olyan kódot, amely egyetlen eszközhöz sem tartozik.
    /// </summary>
    public async Task<DemoKod> DemoKodAsync(long idoszakId, long? korzetId, bool ervenyes, CancellationToken ct)
    {
        if (!await db.LeltarIdoszakok.AnyAsync(i => i.Id == idoszakId, ct))
            throw UzletiHiba.NemTalalhato($"Nincs {idoszakId} azonosítójú leltáridőszak.");

        if (!ervenyes)
        {
            string kod;
            do kod = $"DEMO-{Random.Shared.Next(0x1000000):X6}";
            while (await db.EszkozKodok.AnyAsync(k => k.ErtekNorm == kod, ct));
            return new DemoKod(kod, false, null);
        }

        var kodok = db.EszkozKodok.AsNoTracking()
            .Where(k => k.Aktiv && !k.Eszkoz.EszkozAllapot.Megszunt);
        if (korzetId is { } k)
            kodok = kodok.Where(x => x.Eszkoz.LeltarkorzetId == k);

        var beolvasatlan = kodok.Where(x => !db.Leolvasasok.Any(l =>
            l.LeltarIdoszakId == idoszakId && l.EszkozId == x.EszkozId && !l.Sztornozva));

        var valasztott = await VeletlenAsync(beolvasatlan, ct) ?? await VeletlenAsync(kodok, ct)
            ?? throw UzletiHiba.NemTalalhato("Nincs beolvasható eszköz a kiválasztott körzetben.");
        return new DemoKod(valasztott.Ertek, true, valasztott.EszkozId);
    }

    private static async Task<EszkozKod?> VeletlenAsync(IQueryable<EszkozKod> kodok, CancellationToken ct)
    {
        var darab = await kodok.CountAsync(ct);
        return darab == 0 ? null : await kodok.OrderBy(x => x.Id).Skip(Random.Shared.Next(darab)).FirstAsync(ct);
    }

    private async Task NyitottIdoszakEllenorzeseAsync(long idoszakId, CancellationToken ct)
    {
        var statusz = await db.LeltarIdoszakok.Where(i => i.Id == idoszakId)
            .Select(i => (LeltarIdoszakStatusz?)i.Statusz).SingleOrDefaultAsync(ct)
            ?? throw UzletiHiba.NemTalalhato($"Nincs {idoszakId} azonosítójú leltáridőszak.");

        if (statusz == LeltarIdoszakStatusz.Tervezett)
            throw UzletiHiba.Utkozes("A leltáridőszak még nincs megnyitva, ezért nem rögzíthető beolvasás.");
        if (statusz == LeltarIdoszakStatusz.Lezart)
            throw UzletiHiba.Utkozes("A leltáridőszak lezárva, csak olvasható; beolvasás nem rögzíthető.");
    }

    private async Task MentesAsync(Leolvasas leolvasas, CancellationToken ct)
    {
        db.Leolvasasok.Add(leolvasas);
        await db.SaveChangesAsync(ct);
    }
}
