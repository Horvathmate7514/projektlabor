using System.Globalization;
using LeltarKezelo.Kozos.Import;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;
using KodTipusok = LeltarKezelo.Szerver.Adat.KezdetiAdatok.KodTipusok;

namespace LeltarKezelo.Szerver.Szolgaltatasok.Import;

/// <summary>
/// Az Excel forrásadatok importálása (03_alkalom/04_Excel_lekepezes.md, D-031–D-035).
/// Két lépés, állapot nélkül: az <see cref="ElemzesAsync"/> próbafuttatás (nem ír az adatbázisba),
/// a <see cref="VegrehajtasAsync"/> ugyanazt az elemzést újra elvégzi, majd egyetlen mentéssel ír és naplóz.
/// </summary>
public class ImportSzolgaltatas(LeltarDbContext db, TimeProvider ido)
{
    public const int MaxKodHossz = 64;
    public const int MaxMegnevezesHossz = 200;
    public const int MaxEgysegHossz = 20;
    public const int MaxMegjegyzesHossz = 1000;
    public const int MaxNemSzereploLista = 500;

    private static readonly (ImportMezo Mezo, long Tipus)[] Kodmezok =
    [
        (ImportMezo.SapSzam, KodTipusok.SapSzam),
        (ImportMezo.LeltariSzam1, KodTipusok.LeltariSzam1),
        (ImportMezo.LeltariSzam2, KodTipusok.LeltariSzam2),
        (ImportMezo.GyartasiSzam, KodTipusok.GyartasiSzam),
    ];

    public async Task<ImportElemzes> ElemzesAsync(Stream fajl, string fajlnev, ImportBeallitasok? beallitasok, CancellationToken ct)
    {
        var terv = await TervezesAsync(fajl, fajlnev, beallitasok ?? new ImportBeallitasok(), ct);
        return terv.Elemzes;
    }

    public async Task<ImportEredmeny> VegrehajtasAsync(
        Stream fajl, string fajlnev, long felhasznaloId, ImportBeallitasok? beallitasok, CancellationToken ct)
    {
        beallitasok ??= new ImportBeallitasok();
        var terv = await TervezesAsync(fajl, fajlnev, beallitasok, ct);
        var most = ido.GetUtcNow().UtcDateTime;

        var aktivAllapot = await db.EszkozAllapotok.Where(a => !a.Megszunt).OrderBy(a => a.Sorrend).Select(a => a.Id).FirstAsync(ct);
        var ujFelelosok = new Dictionary<string, FelelosSzemely>();
        FelelosSzemely UjFelelos(string nev)
        {
            var kulcs = ImportErtekek.NormalFejlec(nev);
            if (!ujFelelosok.TryGetValue(kulcs, out var f))
            {
                f = new FelelosSzemely { Nev = nev, Aktiv = true };
                ujFelelosok[kulcs] = f;
                db.FelelosSzemelyek.Add(f);
            }
            return f;
        }

        int uj = 0, frissitett = 0, valtozatlan = 0, kihagyott = 0;
        foreach (var sor in terv.Sorok)
        {
            switch (sor.Allapot)
            {
                case ImportSorAllapot.Uj:
                    UjEszkoz(sor, aktivAllapot, felhasznaloId, most, UjFelelos);
                    uj++;
                    break;
                case ImportSorAllapot.Modositott when beallitasok.FrissitesekAlkalmazasa:
                    Frissites(sor, felhasznaloId, most, UjFelelos);
                    frissitett++;
                    break;
                case ImportSorAllapot.Modositott:
                    kihagyott++;
                    break;
                case ImportSorAllapot.Valtozatlan:
                    valtozatlan++;
                    break;
            }
        }

        var o = terv.Elemzes.Osszesites;
        var sikeres = o.Sorok - o.Hibas;
        var futas = new ImportFutas
        {
            Fajlnev = Rovidit(fajlnev, 260),
            Idopont = most,
            FelhasznaloId = felhasznaloId,
            SorokSzama = o.Sorok,
            Sikeres = sikeres,
            Hibas = o.Hibas,
            Statusz = o.Hibas == 0 ? ImportStatusz.Sikeres : sikeres == 0 ? ImportStatusz.Sikertelen : ImportStatusz.Reszleges,
            Hibak = terv.Elemzes.Sorok
                .SelectMany(s => s.Uzenetek)
                .Where(u => u.Sulyossag == UzenetSulyossag.Hiba)
                .Select(u => new ImportHiba
                {
                    Sor = u.Sor,
                    Oszlop = u.Mezo is { } m ? Rovidit(terv.Fejlec(m), 100) : null,
                    Uzenet = Rovidit(u.Szoveg, 500),
                    Ertek = u.Ertek is null ? null : Rovidit(u.Ertek, 500),
                })
                .ToList(),
        };
        db.ImportFutasok.Add(futas);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw UzletiHiba.Utkozes(
                "Az import nem sikerült, mert az adatbázis időközben megváltozott (például egy kódot közben máshol rögzítettek). " +
                "Semmi nem került be; futtasd újra a próbafuttatást.");
        }

        return new ImportEredmeny(futas.Id, futas.Statusz, uj, frissitett, valtozatlan, kihagyott, o.Hibas, ujFelelosok.Count, terv.Elemzes);
    }

    // ---------------------------------------------------------------- tervezés (próbafuttatás)

    private async Task<Terv> TervezesAsync(Stream fajl, string fajlnev, ImportBeallitasok beallitasok, CancellationToken ct)
    {
        var tabla = ExcelOlvaso.Beolvasas(fajl, beallitasok);
        var adatok = await AdatokBetoltese(tabla, ct);

        var sorok = tabla.Sorok.Select(s => SorFeldolgozasa(s, adatok)).ToList();
        FajlonBeluliUtkozesek(sorok);
        foreach (var sor in sorok)
            Besorolas(sor, adatok);

        var lefedettSap = sorok.Where(s => s.Sap is not null).Select(s => ImportErtekek.KodNorm(s.Sap!)).ToHashSet();
        var nemSzereplok = adatok.AktivSapok
            .Where(x => !lefedettSap.Contains(x.Norm))
            .OrderBy(x => x.Sap)
            .Select(x => new ForrasbanNemSzereploEszkoz(x.EszkozId, x.Sap, x.Megnevezes))
            .ToList();

        var osszegzes = sorok.Select(s => new ImportSorOsszegzes(
            s.Sor, s.Sap, s.Megnevezes, s.Allapot, s.Meglevo?.Id, s.Valtozasok, s.Uzenetek)).ToList();

        var elemzes = new ImportElemzes(
            fajlnev,
            tabla.Munkalap,
            tabla.FejlecSor,
            tabla.Oszlopok,
            tabla.IsmeretlenOszlopok,
            new ImportOsszesites(
                sorok.Count,
                sorok.Count(s => s.Allapot == ImportSorAllapot.Uj),
                sorok.Count(s => s.Allapot == ImportSorAllapot.Modositott),
                sorok.Count(s => s.Allapot == ImportSorAllapot.Valtozatlan),
                sorok.Count(s => s.Allapot == ImportSorAllapot.Hibas),
                sorok.Sum(s => s.Uzenetek.Count(u => u.Sulyossag == UzenetSulyossag.Figyelmeztetes))),
            osszegzes,
            nemSzereplok.Count,
            nemSzereplok.Take(MaxNemSzereploLista).ToList());

        return new Terv(tabla, sorok, elemzes);
    }

    private async Task<Adatok> AdatokBetoltese(ExcelTabla tabla, CancellationToken ct)
    {
        var korzetek = await db.Leltarkorzetek.AsNoTracking().Where(k => k.Aktiv).ToListAsync(ct);
        var tipusok = await db.EszkozTipusok.AsNoTracking().Where(t => t.Aktiv).ToListAsync(ct);
        var felelosok = await db.FelelosSzemelyek.AsNoTracking().Where(f => f.Aktiv).ToListAsync(ct);

        var aktivKodok = await db.EszkozKodok.AsNoTracking()
            .Where(k => k.Aktiv)
            .Select(k => new { k.ErtekNorm, k.EszkozId, k.KodTipusId })
            .ToListAsync(ct);
        var kodok = aktivKodok
            .GroupBy(k => k.ErtekNorm)
            .ToDictionary(g => g.Key, g => (g.First().EszkozId, g.First().KodTipusId));

        var fajlSapjai = tabla.Sorok
            .Select(s => s.Ertek(ImportMezo.SapSzam)?.KodSzoveg)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => ImportErtekek.KodNorm(s!))
            .ToHashSet();
        var meglevoIdk = kodok
            .Where(k => k.Value.KodTipusId == KodTipusok.SapSzam && fajlSapjai.Contains(k.Key))
            .Select(k => k.Value.EszkozId)
            .Distinct()
            .ToList();

        var meglevok = await db.Eszkozok
            .Include(e => e.Kodok)
            .Include(e => e.EszkozTipus)
            .Include(e => e.Leltarkorzet)
            .Where(e => meglevoIdk.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        var aktualisFelelosok = await db.FelelosHozzarendelesek
            .Include(h => h.Felelos)
            .Where(h => h.ErvenyesIg == null && meglevoIdk.Contains(h.EszkozId))
            .ToDictionaryAsync(h => h.EszkozId, ct);

        var aktivSapok = await db.EszkozKodok.AsNoTracking()
            .Where(k => k.Aktiv && k.KodTipusId == KodTipusok.SapSzam && !k.Eszkoz.EszkozAllapot.Megszunt)
            .Select(k => new SapEszkoz(k.ErtekNorm, k.Ertek, k.EszkozId, k.Eszkoz.Megnevezes))
            .ToListAsync(ct);

        return new Adatok(
            Szotar(korzetek, k => [k.Kod, k.Nev]),
            Szotar(tipusok, t => [t.Nev]),
            Szotar(felelosok, f => [f.Azonosito, f.Nev]),
            kodok, meglevok, aktualisFelelosok, aktivSapok);
    }

    private static Dictionary<string, T> Szotar<T>(IEnumerable<T> elemek, Func<T, string?[]> kulcsok)
    {
        var szotar = new Dictionary<string, T>();
        foreach (var e in elemek)
            foreach (var k in kulcsok(e))
                if (!string.IsNullOrWhiteSpace(k))
                    szotar.TryAdd(ImportErtekek.NormalFejlec(k), e);
        return szotar;
    }

    private static SorTerv SorFeldolgozasa(ExcelSor excel, Adatok adatok)
    {
        var s = new SorTerv(excel.Sor);
        void Hiba(ImportMezo? m, string szoveg, string? ertek = null) =>
            s.Uzenetek.Add(new ImportUzenet(s.Sor, m, UzenetSulyossag.Hiba, szoveg, ertek));
        void Figy(ImportMezo? m, string szoveg, string? ertek = null) =>
            s.Uzenetek.Add(new ImportUzenet(s.Sor, m, UzenetSulyossag.Figyelmeztetes, szoveg, ertek));

        // Kódok: SAP kötelező; a leltári számok ütközése hiba, a gyártási számé csak figyelmeztetés.
        foreach (var (mezo, tipus) in Kodmezok)
        {
            if (excel.Ertek(mezo) is not { } e || e.KodSzoveg.Length == 0)
            {
                if (mezo == ImportMezo.SapSzam)
                    Hiba(mezo, "Hiányzó kötelező mező: SAP szám.");
                continue;
            }
            var kod = e.KodSzoveg.Trim();
            if (kod.Length > MaxKodHossz)
            {
                if (mezo == ImportMezo.GyartasiSzam)
                    Figy(mezo, $"A gyártási szám hosszabb {MaxKodHossz} karakternél, kimarad.", kod);
                else
                    Hiba(mezo, $"A(z) {ImportMezok.Nev(mezo)} legfeljebb {MaxKodHossz} karakter lehet.", kod);
                continue;
            }
            if (s.Kodok.Any(k => k.Norm == ImportErtekek.KodNorm(kod)))
            {
                Figy(mezo, $"A(z) {ImportMezok.Nev(mezo)} megegyezik a sor egy másik kódjával, kimarad.", kod);
                continue;
            }
            s.Kodok.Add(new SorKod(mezo, tipus, kod, ImportErtekek.KodNorm(kod)));
        }
        s.Sap = s.Kodok.FirstOrDefault(k => k.Mezo == ImportMezo.SapSzam)?.Ertek;

        var megnevezes = excel.Ertek(ImportMezo.Megnevezes) is { } me ? ImportErtekek.Tisztitott(me.Szoveg) : "";
        if (megnevezes.Length == 0)
            Hiba(ImportMezo.Megnevezes, "Hiányzó kötelező mező: megnevezés.");
        else if (megnevezes.Length > MaxMegnevezesHossz)
            Hiba(ImportMezo.Megnevezes, $"A megnevezés legfeljebb {MaxMegnevezesHossz} karakter lehet.", megnevezes);
        else
            s.Megnevezes = megnevezes;

        if (excel.Ertek(ImportMezo.Leltarkorzet) is not { } ke || ke.Szoveg.Length == 0)
            Hiba(ImportMezo.Leltarkorzet, "Hiányzó kötelező mező: leltárkörzet.");
        else if (adatok.Korzetek.TryGetValue(ImportErtekek.NormalFejlec(ke.Szoveg), out var korzet))
            s.Korzet = korzet;
        else
            Hiba(ImportMezo.Leltarkorzet,
                $"Ismeretlen leltárkörzet: „{ke.Szoveg}”. Előbb vedd fel a körzetet a törzsadatok között, majd importáld újra.",
                ke.Szoveg);

        if (excel.Ertek(ImportMezo.Mennyiseg) is { } mny)
        {
            if (ImportErtekek.Egesz(mny) is { } m && m > 0)
                s.Mennyiseg = m;
            else
                Figy(ImportMezo.Mennyiseg, "A mennyiség nem pozitív egész szám; az alapérték (1) lesz érvényes.", mny.Szoveg);
        }

        if (excel.Ertek(ImportMezo.MennyisegiEgyseg) is { } egy && egy.Szoveg.Length > 0)
        {
            s.Egyseg = egy.Szoveg;
            if (s.Egyseg.Length > MaxEgysegHossz)
            {
                Figy(ImportMezo.MennyisegiEgyseg, $"A mennyiségi egység {MaxEgysegHossz} karakterre rövidül.", s.Egyseg);
                s.Egyseg = s.Egyseg[..MaxEgysegHossz];
            }
        }

        if (excel.Ertek(ImportMezo.Ertek) is { } ert)
        {
            if (ImportErtekek.Penz(ert) is { } p && p >= 0)
                s.Ertek = p;
            else
                Figy(ImportMezo.Ertek, "Az érték nem értelmezhető vagy negatív; üresen marad.", ert.Szoveg);
        }

        if (excel.Ertek(ImportMezo.BeszerzesDatuma) is { } dat)
        {
            var datum = ImportErtekek.Datum(dat);
            if (datum is null)
                Figy(ImportMezo.BeszerzesDatuma, "A beszerzés dátuma nem értelmezhető; üresen marad.", dat.Szoveg);
            else if (datum > DateOnly.FromDateTime(DateTime.UtcNow))
                Figy(ImportMezo.BeszerzesDatuma, "A beszerzés dátuma a jövőben van; üresen marad.", dat.Szoveg);
            else
                s.Beszerzes = datum;
        }

        if (excel.Ertek(ImportMezo.EszkozTipus) is { } tip && tip.Szoveg.Length > 0)
        {
            if (adatok.Tipusok.TryGetValue(ImportErtekek.NormalFejlec(tip.Szoveg), out var tipus))
                s.Tipus = tipus;
            else
                Figy(ImportMezo.EszkozTipus,
                    $"Ismeretlen eszköztípus: „{tip.Szoveg}”; a típus üresen marad. Vedd fel a törzsadatok között, majd importáld újra.",
                    tip.Szoveg);
        }

        if (excel.Ertek(ImportMezo.FelelosSzemely) is { } fel && fel.Szoveg.Length > 0)
        {
            var nev = ImportErtekek.Tisztitott(fel.Szoveg);
            if (adatok.Felelosok.TryGetValue(ImportErtekek.NormalFejlec(nev), out var felelos))
                s.Felelos = felelos;
            else
            {
                s.UjFelelosNev = nev.Length > 200 ? nev[..200] : nev;
                Figy(ImportMezo.FelelosSzemely, $"Új felelős személy kerül felvételre: „{s.UjFelelosNev}”.", nev);
            }
        }

        if (excel.Ertek(ImportMezo.Megjegyzes) is { } megj && megj.Szoveg.Length > 0)
        {
            s.Megjegyzes = megj.Szoveg;
            if (s.Megjegyzes.Length > MaxMegjegyzesHossz)
            {
                Figy(ImportMezo.Megjegyzes, $"A megjegyzés {MaxMegjegyzesHossz} karakterre rövidül.");
                s.Megjegyzes = s.Megjegyzes[..MaxMegjegyzesHossz];
            }
        }

        return s;
    }

    /// <summary>Ugyanaz a kód két sorban: SAP és leltári szám esetén mindkét sor hibás, gyártási számnál figyelmeztetés.</summary>
    private static void FajlonBeluliUtkozesek(List<SorTerv> sorok)
    {
        var elofordulasok = sorok
            .SelectMany(s => s.Kodok.Select(k => (Sor: s, Kod: k)))
            .GroupBy(x => x.Kod.Norm)
            .Where(g => g.Select(x => x.Sor.Sor).Distinct().Count() > 1);

        foreach (var csoport in elofordulasok)
        {
            var sorszamok = csoport.Select(x => x.Sor.Sor).Distinct().ToList();
            foreach (var (sor, kod) in csoport)
            {
                var masik = string.Join(", ", sorszamok.Where(n => n != sor.Sor));
                if (kod.Mezo == ImportMezo.GyartasiSzam)
                {
                    sor.Kodok.Remove(kod);
                    sor.Uzenetek.Add(new ImportUzenet(sor.Sor, kod.Mezo, UzenetSulyossag.Figyelmeztetes,
                        $"A gyártási szám a(z) {masik}. sorban is szerepel; kimarad.", kod.Ertek));
                }
                else
                {
                    sor.Uzenetek.Add(new ImportUzenet(sor.Sor, kod.Mezo, UzenetSulyossag.Hiba,
                        $"Ütköző kód: „{kod.Ertek}” a(z) {masik}. sorban is szerepel.", kod.Ertek));
                }
            }
        }
    }

    /// <summary>Az adatbázissal való összevetés: új, módosított, változatlan vagy hibás sor.</summary>
    private static void Besorolas(SorTerv s, Adatok adatok)
    {
        if (s.Sap is { } sap && adatok.Kodok.TryGetValue(ImportErtekek.KodNorm(sap), out var sapTalalat))
        {
            if (sapTalalat.KodTipusId == KodTipusok.SapSzam && adatok.Meglevok.TryGetValue(sapTalalat.EszkozId, out var e))
                s.Meglevo = e;
            else
                s.Uzenetek.Add(new ImportUzenet(s.Sor, ImportMezo.SapSzam, UzenetSulyossag.Hiba,
                    $"A(z) „{sap}” érték már egy másik eszköz nem SAP típusú kódja.", sap));
        }

        foreach (var kod in s.Kodok.Where(k => k.Mezo != ImportMezo.SapSzam).ToList())
        {
            if (!adatok.Kodok.TryGetValue(kod.Norm, out var talalat))
                continue;
            if (s.Meglevo is not null && talalat.EszkozId == s.Meglevo.Id)
            {
                if (talalat.KodTipusId != kod.Tipus)
                {
                    s.Kodok.Remove(kod);
                    s.Uzenetek.Add(new ImportUzenet(s.Sor, kod.Mezo, UzenetSulyossag.Figyelmeztetes,
                        $"A(z) „{kod.Ertek}” már az eszköz egy másik típusú kódja; kimarad.", kod.Ertek));
                }
                continue;
            }
            if (kod.Mezo == ImportMezo.GyartasiSzam)
            {
                s.Kodok.Remove(kod);
                s.Uzenetek.Add(new ImportUzenet(s.Sor, kod.Mezo, UzenetSulyossag.Figyelmeztetes,
                    $"A gyártási szám már egy másik eszközhöz tartozik (azonosító: {talalat.EszkozId}); kimarad.", kod.Ertek));
            }
            else
            {
                s.Uzenetek.Add(new ImportUzenet(s.Sor, kod.Mezo, UzenetSulyossag.Hiba,
                    $"A(z) „{kod.Ertek}” kód már egy másik eszközhöz tartozik (azonosító: {talalat.EszkozId}).", kod.Ertek));
            }
        }

        if (s.Uzenetek.Any(u => u.Sulyossag == UzenetSulyossag.Hiba))
        {
            s.Allapot = ImportSorAllapot.Hibas;
            return;
        }
        if (s.Meglevo is null)
        {
            s.Allapot = ImportSorAllapot.Uj;
            return;
        }

        Kulonbsegek(s, adatok);
        s.Allapot = s.Valtozasok.Count > 0 ? ImportSorAllapot.Modositott : ImportSorAllapot.Valtozatlan;
    }

    /// <summary>Mezőnkénti különbség a meglévő eszközhöz képest. Az Excel üres cellája nem töröl adatot (D-033).</summary>
    private static void Kulonbsegek(SorTerv s, Adatok adatok)
    {
        var e = s.Meglevo!;
        void Valtozas(ImportMezo m, string? regi, string? uj) => s.Valtozasok.Add(new MezoValtozas(m, regi, uj));

        if (s.Megnevezes != e.Megnevezes)
            Valtozas(ImportMezo.Megnevezes, e.Megnevezes, s.Megnevezes);
        if (s.Korzet!.Id != e.LeltarkorzetId)
            Valtozas(ImportMezo.Leltarkorzet, e.Leltarkorzet.Kod, s.Korzet.Kod);
        if (s.Mennyiseg is { } m && m != e.ElvartMennyiseg)
            Valtozas(ImportMezo.Mennyiseg, e.ElvartMennyiseg.ToString(CultureInfo.InvariantCulture), m.ToString(CultureInfo.InvariantCulture));
        if (s.Egyseg is { } egy && egy != e.MennyisegiEgyseg)
            Valtozas(ImportMezo.MennyisegiEgyseg, e.MennyisegiEgyseg, egy);
        if (s.Ertek is { } ert && ert != e.Ertek)
            Valtozas(ImportMezo.Ertek, e.Ertek?.ToString("0.##", CultureInfo.InvariantCulture), ert.ToString("0.##", CultureInfo.InvariantCulture));
        if (s.Beszerzes is { } b && b != e.BeszerzesDatuma)
            Valtozas(ImportMezo.BeszerzesDatuma, e.BeszerzesDatuma?.ToString("yyyy-MM-dd"), b.ToString("yyyy-MM-dd"));
        if (s.Tipus is { } t && t.Id != e.EszkozTipusId)
            Valtozas(ImportMezo.EszkozTipus, e.EszkozTipus?.Nev, t.Nev);
        if (s.Megjegyzes is { } megj && megj != e.Megjegyzes)
            Valtozas(ImportMezo.Megjegyzes, e.Megjegyzes, megj);

        adatok.AktualisFelelosok.TryGetValue(e.Id, out var aktualis);
        if (s.Felelos is { } f && f.Id != aktualis?.FelelosId)
            Valtozas(ImportMezo.FelelosSzemely, aktualis?.Felelos.Nev, f.Nev);
        else if (s.UjFelelosNev is { } ujNev)
            Valtozas(ImportMezo.FelelosSzemely, aktualis?.Felelos.Nev, ujNev);

        foreach (var kod in s.Kodok.Where(k => k.Mezo != ImportMezo.SapSzam))
        {
            var jelenlegi = e.Kodok.FirstOrDefault(k => k.Aktiv && k.KodTipusId == kod.Tipus);
            if (jelenlegi is null || ImportErtekek.KodNorm(jelenlegi.Ertek) != kod.Norm)
                Valtozas(kod.Mezo, jelenlegi?.Ertek, kod.Ertek);
        }
    }

    // ---------------------------------------------------------------- végrehajtás

    private void UjEszkoz(SorTerv s, long aktivAllapot, long felhasznaloId, DateTime most, Func<string, FelelosSzemely> ujFelelos)
    {
        var eszkoz = new Eszkoz
        {
            Megnevezes = s.Megnevezes!,
            EszkozTipusId = s.Tipus?.Id,
            LeltarkorzetId = s.Korzet!.Id,
            ElvartMennyiseg = s.Mennyiseg ?? 1,
            MennyisegiEgyseg = s.Egyseg,
            Ertek = s.Ertek,
            BeszerzesDatuma = s.Beszerzes,
            EszkozAllapotId = aktivAllapot,
            Megjegyzes = s.Megjegyzes,
            Letrehozva = most,
            LetrehoztaId = felhasznaloId,
            Kodok = s.Kodok.Select(k => new EszkozKod { KodTipusId = k.Tipus, Ertek = k.Ertek, ErtekNorm = k.Norm, Aktiv = true }).ToList(),
        };
        db.Eszkozok.Add(eszkoz);

        if (s.Felelos is not null || s.UjFelelosNev is not null)
            db.FelelosHozzarendelesek.Add(new FelelosHozzarendeles
            {
                Eszkoz = eszkoz,
                FelelosId = s.Felelos?.Id ?? 0,
                Felelos = s.Felelos is null ? ujFelelos(s.UjFelelosNev!) : null!,
                ErvenyesTol = most,
            });
    }

    private void Frissites(SorTerv s, long felhasznaloId, DateTime most, Func<string, FelelosSzemely> ujFelelos)
    {
        var e = s.Meglevo!;
        foreach (var v in s.Valtozasok)
        {
            switch (v.Mezo)
            {
                case ImportMezo.Megnevezes: e.Megnevezes = s.Megnevezes!; break;
                case ImportMezo.Leltarkorzet: e.LeltarkorzetId = s.Korzet!.Id; break;
                case ImportMezo.Mennyiseg: e.ElvartMennyiseg = s.Mennyiseg!.Value; break;
                case ImportMezo.MennyisegiEgyseg: e.MennyisegiEgyseg = s.Egyseg; break;
                case ImportMezo.Ertek: e.Ertek = s.Ertek; break;
                case ImportMezo.BeszerzesDatuma: e.BeszerzesDatuma = s.Beszerzes; break;
                case ImportMezo.EszkozTipus: e.EszkozTipusId = s.Tipus!.Id; break;
                case ImportMezo.Megjegyzes: e.Megjegyzes = s.Megjegyzes; break;
                case ImportMezo.FelelosSzemely:
                    if (db.FelelosHozzarendelesek.Local.FirstOrDefault(h => h.EszkozId == e.Id && h.ErvenyesIg == null) is { } regi)
                        regi.ErvenyesIg = most;
                    db.FelelosHozzarendelesek.Add(new FelelosHozzarendeles
                    {
                        EszkozId = e.Id,
                        FelelosId = s.Felelos?.Id ?? 0,
                        Felelos = s.Felelos is null ? ujFelelos(s.UjFelelosNev!) : null!,
                        ErvenyesTol = most,
                    });
                    break;
                default:
                    var kod = s.Kodok.First(k => k.Mezo == v.Mezo);
                    foreach (var regiKod in e.Kodok.Where(k => k.Aktiv && k.KodTipusId == kod.Tipus))
                        regiKod.Aktiv = false;
                    e.Kodok.Add(new EszkozKod { KodTipusId = kod.Tipus, Ertek = kod.Ertek, ErtekNorm = kod.Norm, Aktiv = true });
                    break;
            }
        }
        e.Modositva = most;
        e.ModositottaId = felhasznaloId;
    }

    private static string Rovidit(string s, int hossz) => s.Length <= hossz ? s : s[..hossz];

    // ---------------------------------------------------------------- belső típusok

    private sealed record SorKod(ImportMezo Mezo, long Tipus, string Ertek, string Norm);

    private sealed record SapEszkoz(string Norm, string Sap, long EszkozId, string Megnevezes);

    private sealed record Adatok(
        Dictionary<string, Leltarkorzet> Korzetek,
        Dictionary<string, EszkozTipus> Tipusok,
        Dictionary<string, FelelosSzemely> Felelosok,
        Dictionary<string, (long EszkozId, long KodTipusId)> Kodok,
        Dictionary<long, Eszkoz> Meglevok,
        Dictionary<long, FelelosHozzarendeles> AktualisFelelosok,
        List<SapEszkoz> AktivSapok);

    private sealed class SorTerv(int sor)
    {
        public int Sor { get; } = sor;
        public List<SorKod> Kodok { get; } = [];
        public List<ImportUzenet> Uzenetek { get; } = [];
        public List<MezoValtozas> Valtozasok { get; } = [];
        public ImportSorAllapot Allapot { get; set; }
        public string? Sap { get; set; }
        public string? Megnevezes { get; set; }
        public Leltarkorzet? Korzet { get; set; }
        public int? Mennyiseg { get; set; }
        public string? Egyseg { get; set; }
        public decimal? Ertek { get; set; }
        public DateOnly? Beszerzes { get; set; }
        public EszkozTipus? Tipus { get; set; }
        public FelelosSzemely? Felelos { get; set; }
        public string? UjFelelosNev { get; set; }
        public string? Megjegyzes { get; set; }
        public Eszkoz? Meglevo { get; set; }
    }

    private sealed record Terv(ExcelTabla Tabla, List<SorTerv> Sorok, ImportElemzes Elemzes)
    {
        public string Fejlec(ImportMezo mezo) =>
            Tabla.Oszlopok.FirstOrDefault(o => o.Mezo == mezo)?.Fejlec ?? ImportMezok.Nev(mezo);
    }
}
