using ClosedXML.Excel;
using LeltarKezelo.Kozos.Import;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using LeltarKezelo.Szerver.Szolgaltatasok;
using LeltarKezelo.Szerver.Szolgaltatasok.Import;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Tesztek;

/// <summary>Az Excel-import tesztjei (03_alkalom/04_Excel_lekepezes.md, 5. pont; D-031–D-035).</summary>
public class ImportTesztek
{
    private static readonly DateTime Most = new(2026, 10, 11, 10, 0, 0, DateTimeKind.Utc);
    private const long Admin = 1;
    private const long RegiMonitor = 100;
    private const long MasikEszkoz = 101;

    private static readonly object?[] Fejlec =
        ["SAP szám", "Leltári szám 1", "Megnevezés", "Mennyiség", "Érték", "Beszerzés dátuma", "Leltárkörzet", "Eszköztípus", "Felelős"];

    private readonly string _adatbazis = Guid.NewGuid().ToString();

    public ImportTesztek()
    {
        using var db = Kontextus();
        Feltoltes(db);
    }

    // ------------------------------------------------------------------ új eszközök

    [Fact]
    public async Task Hibatlan_fajl_uj_eszkozoket_hoz_letre_kodokkal_es_felelossel()
    {
        var fajl = Fajl(Fejlec,
            ["41000001", "L-2024-0001", "Monitor 27\"", 1, 89000, new DateTime(2024, 5, 6), "MIK-A", "Monitor", "Dr. Kovács Anna"],
            ["41000002", "L-2024-0002", "Tárgyalószék", 6, 120000, new DateTime(2023, 1, 9), "GTK-G", "Szék", "Nagy Péter"]);

        var eredmeny = await Vegrehajtas(fajl);

        Assert.Equal(ImportStatusz.Sikeres, eredmeny.Statusz);
        Assert.Equal(2, eredmeny.Uj);
        await using var db = Kontextus();
        var monitor = await db.Eszkozok.Include(e => e.Kodok).SingleAsync(e => e.Megnevezes == "Monitor 27\"");
        Assert.Equal(["41000001", "L-2024-0001"], monitor.Kodok.OrderBy(k => k.KodTipusId).Select(k => k.Ertek));
        Assert.All(monitor.Kodok, k => Assert.Equal(k.Ertek.ToUpperInvariant(), k.ErtekNorm));
        Assert.Equal(89000m, monitor.Ertek);
        Assert.Equal(new DateOnly(2024, 5, 6), monitor.BeszerzesDatuma);
        Assert.Equal(Most, monitor.Letrehozva);
        Assert.Equal(Admin, monitor.LetrehoztaId);
        Assert.Equal(6, (await db.Eszkozok.SingleAsync(e => e.Megnevezes == "Tárgyalószék")).ElvartMennyiseg);
        Assert.True(await db.FelelosHozzarendelesek.AnyAsync(h => h.EszkozId == monitor.Id && h.FelelosId == 1 && h.ErvenyesIg == null));

        var futas = await db.ImportFutasok.Include(f => f.Hibak).SingleAsync();
        Assert.Equal((2, 2, 0), (futas.SorokSzama, futas.Sikeres, futas.Hibas));
        Assert.Empty(futas.Hibak);
    }

    [Fact]
    public async Task A_probafuttatas_nem_ir_az_adatbazisba()
    {
        var fajl = Fajl(Fejlec, ["41000001", null, "Monitor", 1, null, null, "MIK-A", null, null]);

        var elemzes = await Elemzes(fajl);

        Assert.Equal(1, elemzes.Osszesites.Uj);
        await using var db = Kontextus();
        Assert.Equal(2, await db.Eszkozok.CountAsync());
        Assert.Empty(await db.ImportFutasok.ToListAsync());
    }

    [Fact]
    public async Task Hianyzo_megnevezes_csak_az_adott_sort_utasitja_el()
    {
        var fajl = Fajl(Fejlec,
            ["41000001", null, "Monitor", 1, null, null, "MIK-A", null, null],
            ["41000002", null, null, 1, null, null, "MIK-A", null, null]);

        var eredmeny = await Vegrehajtas(fajl);

        Assert.Equal(ImportStatusz.Reszleges, eredmeny.Statusz);
        Assert.Equal((1, 1), (eredmeny.Uj, eredmeny.Hibas));
        await using var db = Kontextus();
        var hiba = await db.ImportHibak.SingleAsync();
        Assert.Equal((3, "Megnevezés"), (hiba.Sor, hiba.Oszlop));
        Assert.Contains("megnevezés", hiba.Uzenet);
    }

    [Fact]
    public async Task Ha_minden_sor_hibas_az_import_sikertelen_de_naplozodik()
    {
        var fajl = Fajl(Fejlec, [null, null, "Monitor", 1, null, null, "MIK-A", null, null]);

        var eredmeny = await Vegrehajtas(fajl);

        Assert.Equal(ImportStatusz.Sikertelen, eredmeny.Statusz);
        await using var db = Kontextus();
        Assert.Equal(1, (await db.ImportFutasok.SingleAsync()).Hibas);
        Assert.Equal(2, await db.Eszkozok.CountAsync());
    }

    // ------------------------------------------------------------------ kódok

    [Fact]
    public async Task Utkozo_leltari_szam_ket_soron_mindket_sort_elutasitja()
    {
        var fajl = Fajl(Fejlec,
            ["41000001", "L-2024-0001", "Monitor", 1, null, null, "MIK-A", null, null],
            ["41000002", "l-2024-0001 ", "Szék", 1, null, null, "MIK-A", null, null]);

        var elemzes = await Elemzes(fajl);

        Assert.All(elemzes.Sorok, s => Assert.Equal(ImportSorAllapot.Hibas, s.Allapot));
        Assert.Contains(elemzes.Sorok[0].Uzenetek, u => u.Szoveg.Contains("3. sorban is szerepel"));
    }

    [Fact]
    public async Task Masik_eszkoz_kodja_hibat_okoz()
    {
        var fajl = Fajl(Fejlec, ["41000001", "L-2020-0101", "Monitor", 1, null, null, "MIK-A", null, null]);

        var sor = (await Elemzes(fajl)).Sorok.Single();

        Assert.Equal(ImportSorAllapot.Hibas, sor.Allapot);
        Assert.Contains(sor.Uzenetek, u => u.Mezo == ImportMezo.LeltariSzam1 && u.Szoveg.Contains($"azonosító: {MasikEszkoz}"));
    }

    [Fact]
    public async Task Vezeto_nullas_kodok_valtozatlanul_kerulnek_be()
    {
        var fajl = Fajl(["SAP szám", "Leltári szám 2", "Gyártási szám", "Megnevezés", "Leltárkörzet"],
            ["41000001", "000123", 777, "Monitor", "MIK-A"]);
        fajl = Formazott(fajl, lap => lap.Cell(2, 3).Style.NumberFormat.Format = "000000");

        await Vegrehajtas(fajl);

        await using var db = Kontextus();
        var kodok = await db.EszkozKodok.Where(k => k.Eszkoz.Megnevezes == "Monitor").Select(k => k.Ertek).ToListAsync();
        Assert.Contains("000123", kodok);
        Assert.Contains("000777", kodok);
    }

    [Fact]
    public async Task Szam_formatumu_SAP_szam_szovegkent_kerul_be()
    {
        var fajl = Fajl(Fejlec, [41000003, null, "Monitor", 1, null, null, "MIK-A", null, null]);

        await Vegrehajtas(fajl);

        await using var db = Kontextus();
        Assert.True(await db.EszkozKodok.AnyAsync(k => k.Ertek == "41000003" && k.KodTipusId == 1));
    }

    // ------------------------------------------------------------------ ismételt import

    [Fact]
    public async Task Ugyanaz_a_fajl_masodszor_valtozatlan()
    {
        object?[] sor = ["41000001", "L-2024-0001", "Monitor", 1, 89000, new DateTime(2024, 5, 6), "MIK-A", "Monitor", "Nagy Péter"];

        await Vegrehajtas(Fajl(Fejlec, sor));
        var masodik = await Vegrehajtas(Fajl(Fejlec, sor));

        Assert.Equal((0, 1), (masodik.Uj, masodik.Valtozatlan));
        await using var db = Kontextus();
        Assert.Equal(3, await db.Eszkozok.CountAsync());
    }

    [Fact]
    public async Task Ismetelt_import_mezonkent_mutatja_a_kulonbseget()
    {
        var fajl = Fajl(Fejlec, ["40000100", "L-2020-0999", "Monitor 24\" Dell", 1, 60000, null, "MIK-A", "Monitor", "Nagy Péter"]);

        var sor = (await Elemzes(fajl)).Sorok.Single();

        Assert.Equal(ImportSorAllapot.Modositott, sor.Allapot);
        Assert.Equal(RegiMonitor, sor.EszkozId);
        Assert.Contains(new MezoValtozas(ImportMezo.Megnevezes, "Régi monitor", "Monitor 24\" Dell"), sor.Valtozasok);
        Assert.Contains(new MezoValtozas(ImportMezo.Ertek, "50000", "60000"), sor.Valtozasok);
        Assert.Contains(new MezoValtozas(ImportMezo.LeltariSzam1, "L-2020-0100", "L-2020-0999"), sor.Valtozasok);
        Assert.Contains(new MezoValtozas(ImportMezo.FelelosSzemely, "Dr. Kovács Anna", "Nagy Péter"), sor.Valtozasok);
        Assert.Equal(4, sor.Valtozasok.Count);
    }

    [Fact]
    public async Task Jovahagyas_utan_frissul_es_a_tortenet_megmarad()
    {
        var fajl = Fajl(Fejlec, ["40000100", "L-2020-0999", "Monitor 24\" Dell", 1, 60000, null, "MIK-A", "Monitor", "Nagy Péter"]);

        var eredmeny = await Vegrehajtas(fajl);

        Assert.Equal(1, eredmeny.Frissitett);
        await using var db = Kontextus();
        var e = await db.Eszkozok.Include(x => x.Kodok).SingleAsync(x => x.Id == RegiMonitor);
        Assert.Equal(("Monitor 24\" Dell", 60000m, Most, Admin), (e.Megnevezes, e.Ertek!.Value, e.Modositva!.Value, e.ModositottaId!.Value));
        Assert.False(e.Kodok.Single(k => k.Ertek == "L-2020-0100").Aktiv);
        Assert.True(e.Kodok.Single(k => k.Ertek == "L-2020-0999").Aktiv);

        var felelosok = await db.FelelosHozzarendelesek.Where(h => h.EszkozId == RegiMonitor).OrderBy(h => h.Id).ToListAsync();
        Assert.Equal(2, felelosok.Count);
        Assert.Equal((1L, Most), (felelosok[0].FelelosId, felelosok[0].ErvenyesIg!.Value));
        Assert.Equal((2L, (DateTime?)null), (felelosok[1].FelelosId, felelosok[1].ErvenyesIg));
    }

    [Fact]
    public async Task Ures_cella_nem_torol_adatot()
    {
        var fajl = Fajl(Fejlec, ["40000100", null, "Régi monitor", null, null, null, "MIK-A", null, null]);

        var eredmeny = await Vegrehajtas(fajl);

        Assert.Equal(1, eredmeny.Valtozatlan);
        await using var db = Kontextus();
        var e = await db.Eszkozok.Include(x => x.Kodok).SingleAsync(x => x.Id == RegiMonitor);
        Assert.Equal(50000m, e.Ertek);
        Assert.True(e.Kodok.Single(k => k.Ertek == "L-2020-0100").Aktiv);
    }

    [Fact]
    public async Task Kikapcsolt_frissitesnel_csak_az_uj_eszkoz_kerul_be()
    {
        var fajl = Fajl(Fejlec,
            ["40000100", null, "Átnevezett monitor", 1, null, null, "MIK-A", null, null],
            ["41000001", null, "Új monitor", 1, null, null, "MIK-A", null, null]);

        var eredmeny = await Vegrehajtas(fajl, new ImportBeallitasok { FrissitesekAlkalmazasa = false });

        Assert.Equal((1, 0, 1), (eredmeny.Uj, eredmeny.Frissitett, eredmeny.Kihagyott));
        await using var db = Kontextus();
        Assert.Equal("Régi monitor", (await db.Eszkozok.SingleAsync(e => e.Id == RegiMonitor)).Megnevezes);
    }

    [Fact]
    public async Task A_forrasban_nem_szereplo_aktiv_eszkozok_listazva()
    {
        var fajl = Fajl(Fejlec, ["41000001", null, "Monitor", 1, null, null, "MIK-A", null, null]);

        var elemzes = await Elemzes(fajl);

        Assert.Equal(2, elemzes.ForrasbanNemSzereploDarab);
        Assert.Contains(elemzes.ForrasbanNemSzereplok, x => x.Id == RegiMonitor && x.SapSzam == "40000100");
    }

    // ------------------------------------------------------------------ törzsadatok

    [Theory]
    [InlineData("XYZ-9")]
    [InlineData("REGI")]
    public async Task Ismeretlen_vagy_inaktiv_korzet_a_sort_elutasitja(string korzet)
    {
        var fajl = Fajl(Fejlec, ["41000001", null, "Monitor", 1, null, null, korzet, null, null]);

        var sor = (await Elemzes(fajl)).Sorok.Single();

        Assert.Equal(ImportSorAllapot.Hibas, sor.Allapot);
        Assert.Contains(sor.Uzenetek, u => u.Mezo == ImportMezo.Leltarkorzet && u.Szoveg.Contains("Ismeretlen leltárkörzet"));
    }

    [Fact]
    public async Task A_korzet_nevvel_is_megadhato()
    {
        var fajl = Fajl(Fejlec, ["41000001", null, "Monitor", 1, null, null, "Gazdaságtudományi kar – G épület", null, null]);

        Assert.Equal(ImportSorAllapot.Uj, (await Elemzes(fajl)).Sorok.Single().Allapot);
    }

    [Fact]
    public async Task Ismeretlen_tipus_figyelmeztetes_es_az_uj_felelos_egyszer_jon_letre()
    {
        var fajl = Fajl(Fejlec,
            ["41000001", null, "Monitor", 1, null, null, "MIK-A", "Űrhajó", "Teszt  Elek"],
            ["41000002", null, "Szék", 1, null, null, "MIK-A", null, "teszt elek"]);

        var eredmeny = await Vegrehajtas(fajl);

        Assert.Equal((2, 1), (eredmeny.Uj, eredmeny.UjFelelosok));
        Assert.Contains(eredmeny.Elemzes.Sorok[0].Uzenetek,
            u => u.Mezo == ImportMezo.EszkozTipus && u.Sulyossag == UzenetSulyossag.Figyelmeztetes);
        await using var db = Kontextus();
        Assert.Null((await db.Eszkozok.SingleAsync(e => e.Megnevezes == "Monitor")).EszkozTipusId);
        var uj = await db.FelelosSzemelyek.SingleAsync(f => f.Nev == "Teszt Elek");
        Assert.Equal(2, await db.FelelosHozzarendelesek.CountAsync(h => h.FelelosId == uj.Id));
    }

    // ------------------------------------------------------------------ értékek

    [Fact]
    public async Task Ertek_es_datum_szovegbol_is_ertelmezheto_a_hibas_csak_figyelmeztet()
    {
        var fajl = Fajl(Fejlec,
            ["41000001", null, "A", 1, "12 500 Ft", "2021.03.15.", "MIK-A", null, null],
            ["41000002", null, "B", 1, "12.500,50", "2021-03-16", "MIK-A", null, null],
            ["41000003", null, "C", "nulla", "sok", "2021.13.45", "MIK-A", null, null]);

        var eredmeny = await Vegrehajtas(fajl);

        Assert.Equal(3, eredmeny.Uj);
        Assert.Equal(3, eredmeny.Elemzes.Sorok[2].Uzenetek.Count(u => u.Sulyossag == UzenetSulyossag.Figyelmeztetes));
        await using var db = Kontextus();
        var e = await db.Eszkozok.Where(x => x.Megnevezes.Length == 1).OrderBy(x => x.Megnevezes).ToListAsync();
        Assert.Equal((12500m, new DateOnly(2021, 3, 15)), (e[0].Ertek!.Value, e[0].BeszerzesDatuma!.Value));
        Assert.Equal((12500.50m, new DateOnly(2021, 3, 16)), (e[1].Ertek!.Value, e[1].BeszerzesDatuma!.Value));
        Assert.Equal((1, (decimal?)null, (DateOnly?)null), (e[2].ElvartMennyiseg, e[2].Ertek, e[2].BeszerzesDatuma));
    }

    [Theory]
    [InlineData("12500", 12500)]
    [InlineData("12 500", 12500)]
    [InlineData("12.500", 12500)]
    [InlineData("12.500,-Ft", 12500)]
    [InlineData("12,5", 12.5)]
    [InlineData("1,234,567.89", 1234567.89)]
    [InlineData("12.5", 12.5)]
    [InlineData("89 000 HUF", 89000)]
    public void Penzosszeg_szovegbol(string szoveg, double vart) =>
        Assert.Equal((decimal)vart, ImportErtekek.Penz(new ExcelErtek(szoveg, null, null, szoveg)));

    // ------------------------------------------------------------------ fájlszerkezet

    [Fact]
    public async Task A_fejlec_rugalmasan_es_nem_csak_az_elso_sorban_ismerheto_fel()
    {
        var fajl = Fajl(
            ["Leltári forrásadatok – 2026"],
            [],
            ["SAP-szám", " MEGNEVEZÉS ", "leltarkorzet", "Költséghely"],
            ["41000001", "Monitor", "MIK-A", "K-100"]);

        var elemzes = await Elemzes(fajl);

        Assert.Equal(3, elemzes.FejlecSor);
        Assert.Equal(["Költséghely"], elemzes.IsmeretlenOszlopok);
        Assert.Equal(ImportSorAllapot.Uj, elemzes.Sorok.Single().Allapot);
    }

    [Fact]
    public async Task Megadott_oszlop_hozzarendeles_felulirja_a_felismerest()
    {
        var fajl = Fajl(["Azonosító", "Megnevezés", "Leltárkörzet"], ["41000001", "Monitor", "MIK-A"]);
        var beallitasok = new ImportBeallitasok { Oszlopok = new Dictionary<ImportMezo, string> { [ImportMezo.SapSzam] = "Azonosító" } };

        var elemzes = await Elemzes(fajl, beallitasok);

        Assert.Equal("41000001", elemzes.Sorok.Single().SapSzam);
    }

    [Fact]
    public async Task Hianyzo_kotelezo_oszlop_ertheto_hibat_ad()
    {
        var fajl = Fajl(["Leltári szám 1", "Megnevezés", "Leltárkörzet"], ["L-1", "Monitor", "MIK-A"]);

        var hiba = await Assert.ThrowsAsync<UzletiHiba>(() => Elemzes(fajl));

        Assert.Contains("Hiányzó kötelező oszlop: SAP szám", hiba.Message);
    }

    [Fact]
    public async Task Ures_munkalap_ertheto_hibat_ad()
    {
        var hiba = await Assert.ThrowsAsync<UzletiHiba>(() => Elemzes(Fajl()));

        Assert.Contains("fejlécsor", hiba.Message);
    }

    [Fact]
    public async Task Regi_xls_formatum_ertheto_hibat_ad()
    {
        var xls = new MemoryStream([0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0, 0, 0, 0]);

        var hiba = await Assert.ThrowsAsync<UzletiHiba>(() => Elemzes(xls));

        Assert.Contains(".xls", hiba.Message);
    }

    // ------------------------------------------------------------------ segédek

    private LeltarDbContext Kontextus() =>
        new(new DbContextOptionsBuilder<LeltarDbContext>().UseInMemoryDatabase(_adatbazis).Options);

    private async Task<ImportElemzes> Elemzes(Stream fajl, ImportBeallitasok? beallitasok = null)
    {
        await using var db = Kontextus();
        return await new ImportSzolgaltatas(db, new RogzitettIdo(Most)).ElemzesAsync(fajl, "teszt.xlsx", beallitasok, default);
    }

    private async Task<ImportEredmeny> Vegrehajtas(Stream fajl, ImportBeallitasok? beallitasok = null)
    {
        await using var db = Kontextus();
        return await new ImportSzolgaltatas(db, new RogzitettIdo(Most)).VegrehajtasAsync(fajl, "teszt.xlsx", Admin, beallitasok, default);
    }

    private static MemoryStream Fajl(params object?[][] sorok)
    {
        using var munkafuzet = new XLWorkbook();
        var lap = munkafuzet.AddWorksheet("Adatok");
        for (var r = 0; r < sorok.Length; r++)
            for (var c = 0; c < sorok[r].Length; c++)
            {
                var cella = lap.Cell(r + 1, c + 1);
                switch (sorok[r][c])
                {
                    case null: break;
                    case string s: cella.Value = s; break;
                    case DateTime d: cella.Value = d; break;
                    case var szam: cella.Value = Convert.ToDouble(szam); break;
                }
            }
        var ms = new MemoryStream();
        munkafuzet.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream Formazott(MemoryStream fajl, Action<IXLWorksheet> formazas)
    {
        using var munkafuzet = new XLWorkbook(fajl);
        formazas(munkafuzet.Worksheet(1));
        var ms = new MemoryStream();
        munkafuzet.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static void Feltoltes(LeltarDbContext db)
    {
        db.KodTipusok.AddRange(
            new KodTipus { Id = 1, Nev = "SAP szám" }, new KodTipus { Id = 2, Nev = "Leltári szám 1" },
            new KodTipus { Id = 3, Nev = "Leltári szám 2" }, new KodTipus { Id = 4, Nev = "Gyártási szám" },
            new KodTipus { Id = 5, Nev = "Belső vonalkód" });
        var aktiv = new EszkozAllapot { Id = 1, Nev = "aktív", Sorrend = 1 };
        db.EszkozAllapotok.AddRange(aktiv, new EszkozAllapot { Id = 5, Nev = "selejtezett", Megszunt = true, Sorrend = 5 });

        var mik = new Leltarkorzet { Id = 1, Kod = "MIK-A", Nev = "Mérnöki Kar – A épület" };
        db.Leltarkorzetek.AddRange(mik,
            new Leltarkorzet { Id = 2, Kod = "GTK-G", Nev = "Gazdaságtudományi Kar – G épület" },
            new Leltarkorzet { Id = 3, Kod = "REGI", Nev = "Megszüntetett körzet", Aktiv = false });

        var monitor = new EszkozTipus { Id = 11, Nev = "Monitor" };
        db.EszkozTipusok.AddRange(monitor, new EszkozTipus { Id = 6, Nev = "Szék" });

        var kovacs = new FelelosSzemely { Id = 1, Nev = "Dr. Kovács Anna", Azonosito = "D0001" };
        db.FelelosSzemelyek.AddRange(kovacs, new FelelosSzemely { Id = 2, Nev = "Nagy Péter", Azonosito = "D0002" });
        db.Felhasznalok.Add(new Felhasznalo { Id = Admin, Felhasznalonev = "admin", Nev = "Admin", JelszoHash = "-" });

        db.Eszkozok.AddRange(
            new Eszkoz
            {
                Id = RegiMonitor, Megnevezes = "Régi monitor", Leltarkorzet = mik, EszkozAllapot = aktiv, EszkozTipus = monitor,
                Ertek = 50000, Letrehozva = Most.AddYears(-2),
                Kodok = [Kod(1, "40000100"), Kod(2, "L-2020-0100")],
            },
            new Eszkoz
            {
                Id = MasikEszkoz, Megnevezes = "Másik eszköz", Leltarkorzet = mik, EszkozAllapot = aktiv,
                Letrehozva = Most.AddYears(-2),
                Kodok = [Kod(1, "40000101"), Kod(2, "L-2020-0101")],
            });
        db.FelelosHozzarendelesek.Add(new FelelosHozzarendeles
            { EszkozId = RegiMonitor, Felelos = kovacs, ErvenyesTol = Most.AddYears(-1) });
        db.SaveChanges();
    }

    private static EszkozKod Kod(long tipus, string ertek) =>
        new() { KodTipusId = tipus, Ertek = ertek, ErtekNorm = ertek.ToUpperInvariant(), Aktiv = true };

    private sealed class RogzitettIdo(DateTime most) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(most);
    }
}
