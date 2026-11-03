using System.Security.Claims;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public class OsszesitesSzolgaltatas(LeltarDbContext db, KorzetJogosultsag jogosultsag)
{
    public sealed record Talalat(long KorzetId, int Mennyiseg, Minosites Minosites);

    private sealed record Olvasas(long? EszkozId, long KorzetId, string KorzetKod, int Mennyiseg, Minosites Minosites, DateTime Idopont);

    private sealed record Elvart(long EszkozId, long KorzetId, string KorzetKod, int Mennyiseg);

    public static (int Beolvasott, int SajatKorzetben) Szamol(int elvart, long? elvartKorzetId, IReadOnlyCollection<Talalat> talalatok)
    {
        if (elvart == 1)
        {
            static int Darab(IEnumerable<Talalat> t) =>
                t.Select(x => x.Minosites == Minosites.Ismetelt ? 1 : x.Mennyiseg).DefaultIfEmpty(0).Max();
            return (Darab(talalatok), Darab(talalatok.Where(t => t.KorzetId == elvartKorzetId)));
        }

        var szamitott = talalatok.Where(t => t.Minosites != Minosites.Ismetelt).ToList();
        return (szamitott.Sum(t => t.Mennyiseg), szamitott.Where(t => t.KorzetId == elvartKorzetId).Sum(t => t.Mennyiseg));
    }

    public static TetelAllapot Allapot(bool elvartTetel, int elvart, int beolvasott, int sajatKorzetben)
    {
        if (!elvartTetel)
            return TetelAllapot.NemElvart;
        if (beolvasott == 0)
            return TetelAllapot.Hianyzik;
        if (sajatKorzetben == 0)
            return TetelAllapot.MasKorzetben;
        if (beolvasott < elvart)
            return TetelAllapot.Reszleges;
        return beolvasott == elvart ? TetelAllapot.Rendben : TetelAllapot.Tobblet;
    }

    public async Task<IReadOnlyList<KorzetOsszesites>> KorzetekAsync(long idoszakId, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var (tetelek, olvasasok, lathato) = await BetoltesAsync(idoszakId, felhasznalo, ct);

        var korzetek = await db.Leltarkorzetek.AsNoTracking()
            .OrderBy(k => k.Kod)
            .Select(k => new { k.Id, k.Kod, k.Nev })
            .ToListAsync(ct);

        return korzetek
            .Where(k => lathato is null || lathato.Contains(k.Id))
            .Select(k =>
            {
                var sajat = tetelek.Where(t => t.ElvartKorzetId == k.Id).ToList();
                int Darab(TetelAllapot a) => sajat.Count(t => t.Allapot == a);
                return new KorzetOsszesites(
                    k.Id, k.Kod, k.Nev,
                    sajat.Count,
                    Darab(TetelAllapot.Rendben),
                    Darab(TetelAllapot.Reszleges),
                    Darab(TetelAllapot.Hianyzik),
                    Darab(TetelAllapot.Tobblet),
                    Darab(TetelAllapot.MasKorzetben),
                    tetelek.Count(t => t.ElvartKorzetId is not null && t.ElvartKorzetId != k.Id && t.TalaltKorzetek.Contains(k.Kod)),
                    tetelek.Count(t => t.Allapot == TetelAllapot.NemElvart && t.TalaltKorzetek.Contains(k.Kod)),
                    olvasasok.Count(o => o.EszkozId is null && o.KorzetId == k.Id),
                    sajat.Sum(t => t.ElvartMennyiseg),
                    sajat.Sum(t => t.BeolvasottMennyiseg));
            })
            .Where(k => k.ElvartTetelek > 0 || k.MasKorzetbolIdeKerult > 0 || k.NemElvart > 0 || k.IsmeretlenKodok > 0)
            .ToList();
    }

    public async Task<LapozottLista<OsszesitesTetel>> TetelekAsync(
        long idoszakId, OsszesitesSzuro szuro, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        if (szuro.Allapot is { } a && !Enum.IsDefined(a))
            throw UzletiHiba.Hibas("Ismeretlen tételállapot.");

        var (tetelek, _, _) = await BetoltesAsync(idoszakId, felhasznalo, ct);
        var oldal = Math.Max(1, szuro.Oldal);
        var meret = Math.Clamp(szuro.OldalMeret, 1, OsszesitesSzuro.MaxOldalMeret);

        IEnumerable<OsszesitesTetel> szurt = tetelek;
        if (szuro.LeltarkorzetId is { } korzetId)
        {
            var kod = await db.Leltarkorzetek.Where(k => k.Id == korzetId).Select(k => k.Kod).SingleOrDefaultAsync(ct);
            szurt = szurt.Where(t => t.ElvartKorzetId == korzetId || (kod is not null && t.TalaltKorzetek.Contains(kod)));
        }
        if (szuro.Allapot is { } allapot)
            szurt = szurt.Where(t => t.Allapot == allapot);
        if (szuro.CsakElteresek)
            szurt = szurt.Where(t => t.Allapot != TetelAllapot.Rendben);

        var lista = szurt.OrderBy(t => t.Allapot).ThenBy(t => t.Megnevezes).ThenBy(t => t.EszkozId).ToList();
        return new LapozottLista<OsszesitesTetel>(lista.Skip((oldal - 1) * meret).Take(meret).ToList(), oldal, meret, lista.Count);
    }

    public async Task<ElvartTetelAdat> UtolagosFelvetelAsync(long idoszakId, UtolagosTetelKeres keres, CancellationToken ct)
    {
        var statusz = await db.LeltarIdoszakok.Where(i => i.Id == idoszakId)
            .Select(i => (LeltarIdoszakStatusz?)i.Statusz).SingleOrDefaultAsync(ct)
            ?? throw UzletiHiba.NemTalalhato($"Nincs {idoszakId} azonosítójú leltáridőszak.");
        if (statusz != LeltarIdoszakStatusz.Nyitott)
            throw UzletiHiba.Utkozes("Elvárt tétel csak nyitott leltáridőszakhoz vehető fel utólag.");

        var eszkoz = await db.Eszkozok.AsNoTracking()
            .Where(e => e.Id == keres.EszkozId)
            .Select(e => new { e.Id, e.LeltarkorzetId, e.ElvartMennyiseg, e.EszkozAllapot.Megszunt })
            .SingleOrDefaultAsync(ct)
            ?? throw UzletiHiba.NemTalalhato($"Nincs {keres.EszkozId} azonosítójú eszköz.");
        if (eszkoz.Megszunt)
            throw UzletiHiba.Hibas("Megszűnt állapotú eszköz nem vehető fel az elvárt tételek közé.");
        if (await db.ElvartTetelek.AnyAsync(t => t.LeltarIdoszakId == idoszakId && t.EszkozId == eszkoz.Id, ct))
            throw UzletiHiba.Utkozes("Az eszköz már szerepel a leltáridőszak elvárt tételei között.");

        var tetel = new ElvartTetel
        {
            LeltarIdoszakId = idoszakId,
            EszkozId = eszkoz.Id,
            LeltarkorzetId = eszkoz.LeltarkorzetId,
            ElvartMennyiseg = eszkoz.ElvartMennyiseg,
            FelvitelOka = ElvartTetelFelvitelOka.Utolagos,
        };
        db.ElvartTetelek.Add(tetel);
        await db.SaveChangesAsync(ct);

        return await db.ElvartTetelek.AsNoTracking()
            .Where(t => t.Id == tetel.Id)
            .Select(t => new ElvartTetelAdat(t.Id, t.EszkozId, t.Eszkoz.Megnevezes, t.LeltarkorzetId, t.Leltarkorzet.Kod,
                t.ElvartMennyiseg, t.FelvitelOka))
            .SingleAsync(ct);
    }

    private async Task<(List<OsszesitesTetel> Tetelek, List<Olvasas> Olvasasok, IReadOnlySet<long>? Lathato)> BetoltesAsync(
        long idoszakId, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        if (!await db.LeltarIdoszakok.AnyAsync(i => i.Id == idoszakId, ct))
            throw UzletiHiba.NemTalalhato($"Nincs {idoszakId} azonosítójú leltáridőszak.");

        var lathato = await jogosultsag.LathatoKorzetekAsync(felhasznalo, ct);

        var elvartak = await db.ElvartTetelek.AsNoTracking()
            .Where(t => t.LeltarIdoszakId == idoszakId)
            .Select(t => new Elvart(t.EszkozId, t.LeltarkorzetId, t.Leltarkorzet.Kod, t.ElvartMennyiseg))
            .ToDictionaryAsync(t => t.EszkozId, ct);

        var olvasasok = await db.Leolvasasok.AsNoTracking()
            .Where(l => l.LeltarIdoszakId == idoszakId && !l.Sztornozva)
            .Select(l => new Olvasas(l.EszkozId, l.AktivKorzetId, l.AktivKorzet.Kod, l.Mennyiseg, l.Minosites, l.Idopont))
            .ToListAsync(ct);

        var eszkozIdk = elvartak.Keys.Concat(olvasasok.Where(o => o.EszkozId is not null).Select(o => o.EszkozId!.Value))
            .Distinct().ToList();
        var eszkozok = await db.Eszkozok.AsNoTracking()
            .Where(e => eszkozIdk.Contains(e.Id))
            .Select(e => new { e.Id, e.Megnevezes, e.MennyisegiEgyseg, e.ElvartMennyiseg })
            .ToDictionaryAsync(e => e.Id, ct);

        var eszkozOlvasasai = olvasasok.Where(o => o.EszkozId is not null).ToLookup(o => o.EszkozId!.Value);

        var tetelek = new List<OsszesitesTetel>();
        foreach (var eszkozId in eszkozIdk)
        {
            var e = eszkozok[eszkozId];
            var sajatOlvasasok = eszkozOlvasasai[eszkozId].ToList();
            elvartak.TryGetValue(eszkozId, out var elvart);
            var elvartMennyiseg = elvart?.Mennyiseg ?? 0;

            var (beolvasott, sajat) = Szamol(elvart?.Mennyiseg ?? e.ElvartMennyiseg, elvart?.KorzetId,
                sajatOlvasasok.Select(o => new Talalat(o.KorzetId, o.Mennyiseg, o.Minosites)).ToList());
            var talaltKorzetek = sajatOlvasasok.Select(o => o.KorzetKod).Distinct().Order().ToList();

            var lathatoTetel = lathato is null
                || (elvart is not null && lathato.Contains(elvart.KorzetId))
                || sajatOlvasasok.Any(o => lathato.Contains(o.KorzetId));
            if (!lathatoTetel)
                continue;

            tetelek.Add(new OsszesitesTetel(
                eszkozId,
                e.Megnevezes,
                e.MennyisegiEgyseg,
                elvart?.KorzetId,
                elvart?.KorzetKod,
                elvartMennyiseg,
                beolvasott,
                sajat,
                Math.Max(0, elvartMennyiseg - beolvasott),
                Math.Max(0, beolvasott - elvartMennyiseg),
                Allapot(elvart is not null, elvartMennyiseg, beolvasott, sajat),
                talaltKorzetek,
                sajatOlvasasok.Select(o => (DateTime?)o.Idopont).Max()));
        }

        return (tetelek, olvasasok, lathato);
    }
}
