using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Kozos.Import;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Szolgaltatasok;
using LeltarKezelo.Szerver.Szolgaltatasok.Import;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Tesztek;

/// <summary>Olyan teszt, amely csak valódi SQL Serverrel fut; a változó nélkül kimarad (a CI-ban sincs SQL Server).</summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerAdatbazis.Valtozo)))
            Skip = $"Csak SQL Serverrel fut: add meg a kapcsolatot a {SqlServerAdatbazis.Valtozo} környezeti változóban.";
    }
}

/// <summary>Ideiglenes adatbázis a migrációkkal és a demóadatokkal; a tesztosztály végén törlődik.</summary>
public sealed class SqlServerAdatbazis : IAsyncLifetime
{
    public const string Valtozo = "LELTAR_TESZT_SQLSERVER";

    private string? _kapcsolat;

    public LeltarDbContext Kontextus() =>
        new(new DbContextOptionsBuilder<LeltarDbContext>().UseSqlServer(_kapcsolat).Options);

    public async Task InitializeAsync()
    {
        var alap = Environment.GetEnvironmentVariable(Valtozo);
        if (string.IsNullOrWhiteSpace(alap))
            return;

        _kapcsolat = new SqlConnectionStringBuilder(alap) { InitialCatalog = $"LeltarKezelo_Teszt_{Guid.NewGuid():N}" }.ConnectionString;
        await using var db = Kontextus();
        await db.Database.MigrateAsync();
        await SzkriptAsync("db/demoadatok.sql");
    }

    public async Task DisposeAsync()
    {
        if (_kapcsolat is null)
            return;
        SqlConnection.ClearAllPools();
        await using var db = Kontextus();
        await db.Database.EnsureDeletedAsync();
    }

    /// <summary>A repó egy SQL-szkriptjének futtatása; visszaadja az összes eredményhalmazt.</summary>
    public async Task<List<List<Dictionary<string, object?>>>> SzkriptAsync(string relativUt)
    {
        await using var kapcsolat = new SqlConnection(_kapcsolat);
        await kapcsolat.OpenAsync();
        await using var parancs = new SqlCommand(await File.ReadAllTextAsync(RepoUt(relativUt)), kapcsolat) { CommandTimeout = 180 };
        await using var olvaso = await parancs.ExecuteReaderAsync();

        var halmazok = new List<List<Dictionary<string, object?>>>();
        do
        {
            var sorok = new List<Dictionary<string, object?>>();
            while (await olvaso.ReadAsync())
                sorok.Add(Enumerable.Range(0, olvaso.FieldCount).ToDictionary(olvaso.GetName, i => olvaso.IsDBNull(i) ? null : olvaso.GetValue(i)));
            halmazok.Add(sorok);
        } while (await olvaso.NextResultAsync());
        return halmazok;
    }

    public static string RepoUt(string relativUt)
    {
        var mappa = new DirectoryInfo(AppContext.BaseDirectory);
        while (mappa is not null && !File.Exists(Path.Combine(mappa.FullName, "LeltarKezelo.sln")))
            mappa = mappa.Parent;
        return Path.Combine(mappa?.FullName ?? throw new DirectoryNotFoundException("A repó gyökere nem található."), relativUt);
    }
}

/// <summary>
/// Amit a memóriabeli adatbázis nem tud ellenőrizni: a lekérdezések SQL-re fordítása, a számított oszlop, a megszorítások
/// és a demóadatok. Futtatás: <c>$env:LELTAR_TESZT_SQLSERVER = "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True"</c>,
/// majd <c>dotnet test</c>. A saját, ideiglenes adatbázisán dolgozik; az író tesztek a végén visszagörgetnek.
/// </summary>
public class SqlServerTesztek(SqlServerAdatbazis adatbazis) : IClassFixture<SqlServerAdatbazis>
{
    private const long Lezart2025 = 1;
    internal const long Nyitott2026 = 2;

    internal static readonly ClaimsPrincipal Admin = new(new ClaimsIdentity(
        [new Claim(JwtRegisteredClaimNames.Sub, "1"), new Claim(ClaimTypes.Role, Szerepkorok.Admin)], "teszt"));

    private static string Szoveg(Dictionary<string, object?> sor, string oszlop) => Convert.ToString(sor[oszlop])!;

    [SqlServerFact]
    public Task Az_ellenorzo_szkript_minden_probaja_megfelelt() => SzkriptMegfelelAsync(adatbazis);

    [SqlServerFact]
    public Task Az_osszesito_vegpont_szolgaltatasa_egyezik_a_fuggetlen_SQL_szamitassal() => EgyezikAzSqlSzamitassalAsync(adatbazis);

    internal static async Task SzkriptMegfelelAsync(SqlServerAdatbazis adatbazis)
    {
        var halmazok = await adatbazis.SzkriptAsync("db/ellenorzes.sql");

        Assert.Equal(3, halmazok.Count);
        Assert.Equal(15, halmazok[0].Count);
        Assert.Equal(10, halmazok[2].Count);
        Assert.All(halmazok[0].Concat(halmazok[2]), sor => Assert.True(Szoveg(sor, "Eredmeny") == "MEGFELELT", Szoveg(sor, "Ellenorzes")));
    }

    internal static async Task EgyezikAzSqlSzamitassalAsync(SqlServerAdatbazis adatbazis)
    {
        var sqlSorok = (await adatbazis.SzkriptAsync("db/ellenorzes.sql"))[1];
        await using var db = adatbazis.Kontextus();
        var szolgaltatas = new OsszesitesSzolgaltatas(db, new KorzetJogosultsag(db));

        foreach (var idoszak in new[] { Lezart2025, Nyitott2026 })
        {
            var kod = await szolgaltatas.KorzetekAsync(idoszak, Admin, default);
            var sql = sqlSorok.Where(s => Convert.ToInt64(s["Idoszak"]) == idoszak && Szoveg(s, "Korzet") != "Összesen").ToList();

            Assert.Equal(
                sql.Select(s => string.Join('|', Szoveg(s, "Korzet"), s["ElvartTetelek"], s["Rendben"], s["Reszleges"], s["Hianyzik"], s["Tobblet"],
                    s["MasKorzetben"], s["IdeKerult"], s["NemElvart"], s["IsmeretlenKod"], s["ElvartMennyiseg"], s["BeolvasottMennyiseg"])),
                kod.Select(k => string.Join('|', k.Kod, k.ElvartTetelek, k.Rendben, k.Reszleges, k.Hianyzik, k.Tobblet,
                    k.MasKorzetben, k.MasKorzetbolIdeKerult, k.NemElvart, k.IsmeretlenKodok, k.ElvartMennyiseg, k.BeolvasottMennyiseg)));
        }
    }

    [SqlServerFact]
    public async Task A_demo_leltar_mennyisegi_esetei_a_vart_allapotban_vannak()
    {
        await using var db = adatbazis.Kontextus();
        var szolgaltatas = new OsszesitesSzolgaltatas(db, new KorzetJogosultsag(db));
        var tetelek = new Dictionary<long, OsszesitesTetel>();
        for (var oldal = 1; ; oldal++)
        {
            var lap = await szolgaltatas.TetelekAsync(Nyitott2026, new OsszesitesSzuro { Oldal = oldal, OldalMeret = OsszesitesSzuro.MaxOldalMeret }, Admin, default);
            foreach (var t in lap.Elemek)
                tetelek[t.EszkozId] = t;
            if (oldal >= lap.OldalakSzama)
                break;
        }

        (long Id, TetelAllapot Allapot, int Elvart, int Beolvasott, int Sajat)[] vart =
        [
            (5, TetelAllapot.Reszleges, 30, 27, 27),
            (149, TetelAllapot.Tobblet, 30, 33, 33),
            (13, TetelAllapot.Rendben, 10, 10, 10),
            (29, TetelAllapot.Rendben, 30, 30, 30),
            (31, TetelAllapot.Reszleges, 15, 13, 9),
            (151, TetelAllapot.MasKorzetben, 15, 15, 0),
            (45, TetelAllapot.Tobblet, 6, 8, 8),
            (7, TetelAllapot.Hianyzik, 15, 0, 0),
            (9, TetelAllapot.Tobblet, 1, 2, 2),
            (12, TetelAllapot.Rendben, 1, 1, 1),
            (6, TetelAllapot.Rendben, 1, 1, 1),
            (8, TetelAllapot.Rendben, 1, 1, 1),
            (50, TetelAllapot.MasKorzetben, 1, 1, 0),
            (74, TetelAllapot.NemElvart, 0, 1, 0),
            (238, TetelAllapot.Rendben, 1, 1, 1),
            (240, TetelAllapot.NemElvart, 0, 1, 0),
        ];
        foreach (var v in vart)
        {
            var t = tetelek[v.Id];
            Assert.Equal((v.Id, v.Allapot, v.Elvart, v.Beolvasott, v.Sajat), (t.EszkozId, t.Allapot, t.ElvartMennyiseg, t.BeolvasottMennyiseg, t.SajatKorzetben));
        }

        Assert.Equal(["MIK-A", "MIK-B"], tetelek[31].TalaltKorzetek);
        Assert.Equal(["MIK-A", "MIK-B"], tetelek[12].TalaltKorzetek);
        Assert.Equal(230, tetelek.Count);
    }

    [SqlServerFact]
    public async Task Beolvasas_es_sztorno_a_szamitott_kodoszlopon_es_a_megszoritasokon_at()
    {
        await using var db = adatbazis.Kontextus();
        await using var tranzakcio = await db.Database.BeginTransactionAsync();
        var jogosultsag = new KorzetJogosultsag(db);
        var beolvasas = new BeolvasasSzolgaltatas(db, jogosultsag, TimeProvider.System);
        var osszesites = new OsszesitesSzolgaltatas(db, jogosultsag);
        const long multimeterKeszlet = 61;
        const long mikI = 3;

        async Task<OsszesitesTetel> Tetel() =>
            (await osszesites.TetelekAsync(Nyitott2026, new OsszesitesSzuro { LeltarkorzetId = mikI, OldalMeret = 200 }, Admin, default))
                .Elemek.Single(t => t.EszkozId == multimeterKeszlet);

        // Szóközzel és más írásmóddal beolvasva is azonosít (ErtekNorm, D-012).
        var elso = await beolvasas.BeolvasasAsync(Nyitott2026, new BeolvasasKeres(" 40000061 ", mikI, BeviteliMod.Kezi, 7), Admin, default);
        var reszleges = await Tetel();
        var masodik = await beolvasas.BeolvasasAsync(Nyitott2026, new BeolvasasKeres("40000061", mikI, BeviteliMod.Olvaso, 5), Admin, default);
        var tobblet = await Tetel();
        await beolvasas.SztornoAsync(masodik.LeolvasasId, new SztornoKeres("Teszt"), Admin, default);
        var sztorno = await Tetel();

        Assert.Equal((Minosites.Ok, 7), (elso.Minosites, elso.BeolvasottMennyiseg));
        Assert.Equal((TetelAllapot.Reszleges, 3), (reszleges.Allapot, reszleges.Hiany));
        Assert.Equal((Minosites.Tobblet, 12), (masodik.Minosites, masodik.BeolvasottMennyiseg));
        Assert.Equal((TetelAllapot.Tobblet, 2), (tobblet.Allapot, tobblet.Tobblet));
        Assert.Equal((TetelAllapot.Reszleges, 7), (sztorno.Allapot, sztorno.BeolvasottMennyiseg));

        await tranzakcio.RollbackAsync();
    }

    [SqlServerFact]
    public async Task Az_import_a_helyes_mintafajllal_es_ismetelve()
    {
        await using var db = adatbazis.Kontextus();
        await using var tranzakcio = await db.Database.BeginTransactionAsync();

        var elso = await ImportAsync(db, "forrasadatok_minta.xlsx");
        db.ChangeTracker.Clear();
        var masodik = await ImportAsync(db, "forrasadatok_minta.xlsx");

        Assert.Equal((ImportStatusz.Sikeres, 25, 4, 1, 0), (elso.Statusz, elso.Uj, elso.Frissitett, elso.Valtozatlan, elso.Hibas));
        Assert.Equal((0, 0, 30), (masodik.Uj, masodik.Frissitett, masodik.Valtozatlan));

        await tranzakcio.RollbackAsync();
    }

    [SqlServerFact]
    public async Task Az_import_a_hibas_mintafajllal_csak_a_jo_sorokat_viszi_be_es_naploz()
    {
        await using var db = adatbazis.Kontextus();
        await using var tranzakcio = await db.Database.BeginTransactionAsync();

        var eredmeny = await ImportAsync(db, "forrasadatok_hibas.xlsx");
        var hibak = await db.ImportHibak.CountAsync(h => h.ImportFutasId == eredmeny.ImportFutasId);

        Assert.Equal((ImportStatusz.Reszleges, 4, 6, 1), (eredmeny.Statusz, eredmeny.Uj, eredmeny.Hibas, eredmeny.UjFelelosok));
        Assert.Equal(6, hibak);

        await tranzakcio.RollbackAsync();
    }

    [SqlServerFact]
    public async Task A_mennyisegi_mintafajl_a_nyitott_leltar_pillanatkepet_nem_valtoztatja()
    {
        await using var db = adatbazis.Kontextus();
        await using var tranzakcio = await db.Database.BeginTransactionAsync();
        const long szekkeszlet = 5;

        var eredmeny = await ImportAsync(db, "forrasadatok_mennyisegek.xlsx");
        db.ChangeTracker.Clear();

        var o = eredmeny.Elemzes.Osszesites;
        Assert.Equal((6, 1, 2, 0, 2), (o.Uj, o.Modositott, o.Valtozatlan, o.Hibas, o.Figyelmeztetesek));
        Assert.Equal(32, (await db.Eszkozok.SingleAsync(e => e.Id == szekkeszlet)).ElvartMennyiseg);
        Assert.Equal(30, (await db.ElvartTetelek.SingleAsync(t => t.LeltarIdoszakId == Nyitott2026 && t.EszkozId == szekkeszlet)).ElvartMennyiseg);
        var uj = await db.Eszkozok.SingleAsync(e => e.Megnevezes == "Előadói székkészlet");
        Assert.Equal((120, "szett"), (uj.ElvartMennyiseg, uj.MennyisegiEgyseg));
        Assert.Equal(10, (await db.Eszkozok.SingleAsync(e => e.Megnevezes == "Multiméter-készlet (hallgatói labor)" && e.Id == 13)).ElvartMennyiseg);

        await tranzakcio.RollbackAsync();
    }

    private static async Task<ImportEredmeny> ImportAsync(LeltarDbContext db, string fajlnev)
    {
        await using var fajl = File.OpenRead(SqlServerAdatbazis.RepoUt(Path.Combine("db", "minta", fajlnev)));
        return await new ImportSzolgaltatas(db, TimeProvider.System).VegrehajtasAsync(fajl, fajlnev, 2, null, default);
    }
}

/// <summary>A darabszám szabálya olyan leolvasásokkal, amelyek megmaradnak; ezért saját ideiglenes adatbázisa van (D-041).</summary>
public class SqlServerDarabszamTesztek(SqlServerAdatbazis adatbazis) : IClassFixture<SqlServerAdatbazis>
{
    [SqlServerFact]
    public async Task Ismetelt_leolvasas_mennyisege_es_sztornoja_utan_is_egyezik_a_fuggetlen_SQL_szamitassal()
    {
        const long idoszak = SqlServerTesztek.Nyitott2026;
        const long egyediRendben = 6;
        const long egyediNemElvart = 240;
        var admin = SqlServerTesztek.Admin;
        await using var db = adatbazis.Kontextus();
        var jogosultsag = new KorzetJogosultsag(db);
        var beolvasas = new BeolvasasSzolgaltatas(db, jogosultsag, TimeProvider.System);
        var osszesites = new OsszesitesSzolgaltatas(db, jogosultsag);

        async Task<(long Id, string Kod, long Korzet)> ElsoLeolvasas(long eszkozId)
        {
            var l = await db.Leolvasasok.AsNoTracking()
                .Where(l => l.LeltarIdoszakId == idoszak && l.EszkozId == eszkozId)
                .OrderBy(l => l.Id)
                .Select(l => new { l.Id, l.BeolvasottKod, l.AktivKorzetId })
                .FirstAsync();
            return (l.Id, l.BeolvasottKod, l.AktivKorzetId);
        }

        async Task<OsszesitesTetel> Tetel(long eszkozId, long korzet) =>
            (await osszesites.TetelekAsync(idoszak, new OsszesitesSzuro { LeltarkorzetId = korzet, OldalMeret = OsszesitesSzuro.MaxOldalMeret }, admin, default))
                .Elemek.Single(t => t.EszkozId == eszkozId);

        var rendben = await ElsoLeolvasas(egyediRendben);
        var ismetelt = await beolvasas.BeolvasasAsync(idoszak, new BeolvasasKeres(rendben.Kod, rendben.Korzet, BeviteliMod.Kezi, 5), admin, default);

        var nemElvart = await ElsoLeolvasas(egyediNemElvart);
        var masodik = await beolvasas.BeolvasasAsync(idoszak, new BeolvasasKeres(nemElvart.Kod, nemElvart.Korzet, BeviteliMod.Kezi, 1), admin, default);
        await beolvasas.SztornoAsync(nemElvart.Id, new SztornoKeres("Teszt"), admin, default);

        var egyedi = await Tetel(egyediRendben, rendben.Korzet);
        var idegen = await Tetel(egyediNemElvart, nemElvart.Korzet);

        Assert.Equal(Minosites.Ismetelt, ismetelt.Minosites);
        Assert.Equal(Minosites.Ismetelt, masodik.Minosites);
        Assert.Equal((TetelAllapot.Rendben, 1, 1, 0), (egyedi.Allapot, egyedi.BeolvasottMennyiseg, egyedi.SajatKorzetben, egyedi.Tobblet));
        Assert.Equal((TetelAllapot.NemElvart, 1), (idegen.Allapot, idegen.BeolvasottMennyiseg));
        await SqlServerTesztek.SzkriptMegfelelAsync(adatbazis);
        await SqlServerTesztek.EgyezikAzSqlSzamitassalAsync(adatbazis);
    }
}
