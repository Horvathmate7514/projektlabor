using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Kozos.Import;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Kozos.Naplo;
using LeltarKezelo.Kozos.Torzsadatok;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.Extensions.DependencyInjection;

namespace LeltarKezelo.Szerver.Tesztek;

/// <summary>
/// Eszközállapotok, logikai törlés, az adatok története és az audit napló (10. alkalom, F-25, F-26, D-045–D-047).
/// </summary>
public class AllapotTortenetTesztek(AllapotTortenetTesztek.Gyar gyar) : IClassFixture<AllapotTortenetTesztek.Gyar>
{
    public sealed class Gyar : LeltarGyar
    {
        protected override void Feltoltes(LeltarDbContext db)
        {
            base.Feltoltes(db);
            db.EszkozAllapotok.AddRange(
                new EszkozAllapot { Id = JavitasAlatt, Nev = "javítás alatt", Sorrend = 2 },
                new EszkozAllapot { Id = Elveszett, Nev = "elveszett", Megszunt = true, Sorrend = 6 });
        }
    }

    private const long Aktiv = 1;
    private const long JavitasAlatt = 2;
    private const long Elveszett = 6;
    private const long Szamitogep = 1;
    private const long Monitor = 2;
    private const long Szek = 3;
    private const long Targyaloszekek = 4;

    private Task<HttpClient> FelelosAsync() => gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

    private static async Task<List<NaploElem>> NaploAsync(HttpClient kliens, string szuro) =>
        (await kliens.GetFromJsonAsync<LapozottLista<NaploElem>>($"{ApiUtvonalak.AuditNaplo}?oldalMeret=200&{szuro}"))!.Elemek.ToList();

    private static Task<EszkozPillanat?> PillanatAsync(HttpClient kliens, long eszkozId, string idopont) =>
        kliens.GetFromJsonAsync<EszkozPillanat>($"{ApiUtvonalak.EszkozPillanat(eszkozId)}?idopont={idopont}");

    [Fact]
    public async Task Megszunt_allapotba_lepeskor_a_hely_es_a_felelos_lezarul_a_tortenet_megmarad()
    {
        var felelos = await FelelosAsync();

        var valasz = await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozAllapot(Monitor),
            new AllapotKeres(Elveszett, " A leltárban nem került elő. ", "ÜGY-2026/15"));
        var utana = await valasz.Content.ReadFromJsonAsync<EszkozHozzarendelesek>();
        var tortenet = await EszkozHozzarendelesTesztek.TortenetAsync(felelos, Monitor);
        var szurt = await felelos.GetFromJsonAsync<LapozottLista<EszkozListaElem>>($"{ApiUtvonalak.Eszkozok}?eszkozAllapotId={Elveszett}");

        Assert.Equal(HttpStatusCode.OK, valasz.StatusCode);
        Assert.Equal(("elveszett", true, null, null), (utana!.Allapot, utana.Megszunt, utana.HelyisegId, utana.FelelosId));
        Assert.Equal(
            new AllapotTortenetElem(tortenet.Allapotok[0].Idopont, "aktív", "elveszett", true, "A leltárban nem került elő.", "ÜGY-2026/15", "Leltár Felelős"),
            Assert.Single(tortenet.Allapotok));
        Assert.Equal(2, tortenet.Helyek.Count);
        Assert.All(tortenet.Helyek, h => Assert.NotNull(h.ErvenyesIg));
        Assert.Equal("Kovács Anna", Assert.Single(tortenet.Felelosok).Nev);
        Assert.NotNull(tortenet.Felelosok[0].ErvenyesIg);
        Assert.Contains(szurt!.Elemek, e => e.Id == Monitor);

        var vissza = await (await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozAllapot(Monitor), new AllapotKeres(Aktiv, "Előkerült a raktárból.")))
            .Content.ReadFromJsonAsync<EszkozHozzarendelesek>();
        var vegul = await EszkozHozzarendelesTesztek.TortenetAsync(felelos, Monitor);

        Assert.Equal(("aktív", false, null), (vissza!.Allapot, vissza.Megszunt, vissza.HelyisegId));
        Assert.Equal(["elveszett", "aktív"], vegul.Allapotok.Select(a => a.UjAllapot));
        Assert.Equal("elveszett", vegul.Allapotok[1].RegiAllapot);
    }

    [Fact]
    public async Task Nem_megszunt_allapotvaltasnal_a_hely_megmarad()
    {
        var felelos = await FelelosAsync();

        var utana = await (await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozAllapot(Szamitogep), new AllapotKeres(JavitasAlatt, "Szervizbe szállítva.")))
            .Content.ReadFromJsonAsync<EszkozHozzarendelesek>();

        Assert.Equal(("javítás alatt", false, LeltarGyar.Terem101), (utana!.Allapot, utana.Megszunt, utana.HelyisegId));
    }

    [Theory]
    [InlineData(Szek, Elveszett, "", HttpStatusCode.BadRequest)]
    [InlineData(Szek, Elveszett, "   ", HttpStatusCode.BadRequest)]
    [InlineData(Szek, 99, "Indoklás", HttpStatusCode.BadRequest)]
    [InlineData(Szek, Aktiv, "Indoklás", HttpStatusCode.Conflict)]
    [InlineData(9999, Elveszett, "Indoklás", HttpStatusCode.NotFound)]
    public async Task Hibas_allapotvaltas_elutasitva(long eszkozId, long allapotId, string indoklas, HttpStatusCode vart)
    {
        var felelos = await FelelosAsync();

        var valasz = await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozAllapot(eszkozId), new AllapotKeres(allapotId, indoklas));
        var tulHosszu = await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozAllapot(Szek), new AllapotKeres(Elveszett, new string('x', 501)));

        Assert.Equal(vart, valasz.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tulHosszu.StatusCode);
    }

    [Theory]
    [InlineData(LeltarGyar.Leltarozo)]
    [InlineData(LeltarGyar.Megtekinto)]
    public async Task Allapotot_es_naplot_csak_a_leltarfelelos_es_az_admin_kezel(string felhasznalonev)
    {
        var kliens = await gyar.BejelentkezettKliensAsync(felhasznalonev);

        var allapot = await kliens.PostAsJsonAsync(ApiUtvonalak.EszkozAllapot(Szek), new AllapotKeres(Elveszett, "Indoklás"));
        var naplo = await kliens.GetAsync(ApiUtvonalak.AuditNaplo);

        Assert.Equal(HttpStatusCode.Forbidden, allapot.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, naplo.StatusCode);
    }

    [Fact]
    public async Task Megszunt_eszkoz_nem_kerul_az_uj_leltar_pillanatkepebe_de_a_beolvasasa_rogzul()
    {
        var felelos = await FelelosAsync();
        (await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozAllapot(Targyaloszekek), new AllapotKeres(Elveszett, "Nem található."))).EnsureSuccessStatusCode();
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var elotte = await OsszesitesTesztek.TetelekAsync(felelos, id);
        var beolvasas = await OsszesitesTesztek.BeolvasAsync(felelos, id, "SZEK-30");
        var leltar = (await EszkozHozzarendelesTesztek.TortenetAsync(felelos, Targyaloszekek)).Leltarak.Single(l => l.LeltarIdoszakId == id);

        Assert.DoesNotContain(elotte, t => t.EszkozId == Targyaloszekek);
        Assert.Equal(Minosites.NemAktiv, beolvasas.Minosites);
        Assert.Equal((TetelAllapot.NemElvart, 0), (leltar.Allapot, leltar.ElvartMennyiseg));
    }

    [Fact]
    public async Task Korabbi_idopont_allapota_helye_es_felelose_visszakeresheto()
    {
        var felelos = await FelelosAsync();
        using (var scope = gyar.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LeltarDbContext>();
            db.Eszkozok.Add(new Eszkoz
            {
                Id = 900, Megnevezes = "Új projektor", LeltarkorzetId = LeltarGyar.MikKorzet, EszkozAllapotId = Aktiv,
                Letrehozva = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            });
            await db.SaveChangesAsync();
        }

        var korabban = await PillanatAsync(felelos, Monitor, "2025-06-01T00:00:00Z");
        var marcius = await PillanatAsync(felelos, Monitor, "2026-03-01T00:00:00Z");
        var augusztus = await PillanatAsync(felelos, Monitor, "2026-08-01T00:00:00Z");
        var megNemLetezett = await PillanatAsync(felelos, 900, "2026-09-01T00:00:00Z");

        Assert.Equal((true, "aktív", null, null), (korabban!.Letezett, korabban.Allapot, korabban.Helyiseg, korabban.Felelos));
        Assert.Equal(("aktív", "I 101", "Kovács Anna"), (marcius!.Allapot, marcius.Helyiseg, marcius.Felelos));
        Assert.Equal(("I 102", "Kovács Anna"), (augusztus!.Helyiseg, augusztus.Felelos));
        Assert.Equal((false, null), (megNemLetezett!.Letezett, megNemLetezett.Allapot));
    }

    [Fact]
    public async Task Allapotvaltas_utan_a_korabbi_allapot_is_lekerdezheto()
    {
        var felelos = await FelelosAsync();
        using (var scope = gyar.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LeltarDbContext>();
            db.Eszkozok.Add(new Eszkoz { Id = 910, Megnevezes = "Vetítővászon", LeltarkorzetId = LeltarGyar.MikKorzet, EszkozAllapotId = Aktiv });
            await db.SaveChangesAsync();
        }
        (await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozAllapot(910), new AllapotKeres(Elveszett, "Nem található."))).EnsureSuccessStatusCode();

        var tegnap = await PillanatAsync(felelos, 910, DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"));
        var holnap = await PillanatAsync(felelos, 910, DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ"));

        Assert.Equal(("aktív", false), (tegnap!.Allapot, tegnap.Megszunt));
        Assert.Equal(("elveszett", true), (holnap!.Allapot, holnap.Megszunt));
    }

    [Fact]
    public async Task Eszkoz_modositasa_az_audit_naploba_es_a_tortenetbe_kerul()
    {
        var felelos = await FelelosAsync();

        (await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozTipusa(Szek), new TipusKeres(LeltarGyar.MonitorTipus))).EnsureSuccessStatusCode();
        var sor = (await NaploAsync(felelos, $"entitas=Eszkoz&entitasId={Szek}")).Single(n => n.Muvelet == NaploMuveletek.Modositas);
        var tortenet = await EszkozHozzarendelesTesztek.TortenetAsync(felelos, Szek);

        Assert.Equal(("{\"EszkozTipusId\":null}", "{\"EszkozTipusId\":11}", "Leltár Felelős"), (sor.RegiErtekJson, sor.UjErtekJson, sor.Felhasznalo));
        Assert.Contains(tortenet.Modositasok, n => n.Id == sor.Id);

        using var scope = gyar.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeltarDbContext>();
        var eszkoz = db.Eszkozok.Single(e => e.Id == Szek);
        Assert.NotNull(eszkoz.Modositva);
        Assert.Equal(db.Felhasznalok.Single(f => f.Felhasznalonev == LeltarGyar.Felelos).Id, eszkoz.ModositottaId);
    }

    [Fact]
    public async Task Uj_torzsadat_letrehozasa_naplozodik_a_bejelentkezes_nem()
    {
        var felelos = await FelelosAsync();
        var uj = await (await felelos.PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes("N", "12", Megnevezes: "Labor")))
            .Content.ReadFromJsonAsync<HelyisegKezelesAdat>();

        var sor = Assert.Single(await NaploAsync(felelos, $"entitas=Helyiseg&entitasId={uj!.Id}"));
        var elotte = await NaploAsync(felelos, "entitas=Felhasznalo");
        await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var utana = await NaploAsync(felelos, "entitas=Felhasznalo");
        var lapozva = await felelos.GetFromJsonAsync<LapozottLista<NaploElem>>($"{ApiUtvonalak.AuditNaplo}?oldalMeret=1");
        var jovobeli = await NaploAsync(felelos, $"tol={DateTime.UtcNow.AddDays(1):yyyy-MM-dd}");

        Assert.Equal((NaploMuveletek.Letrehozas, null, "Leltár Felelős"), (sor.Muvelet, sor.RegiErtekJson, sor.Felhasznalo));
        Assert.Contains("\"Epulet\":\"N\"", sor.UjErtekJson);
        Assert.Contains("\"Megnevezes\":\"Labor\"", sor.UjErtekJson);
        Assert.Equal(elotte.Count, utana.Count);
        Assert.Single(lapozva!.Elemek);
        Assert.True(lapozva.Osszesen > 1);
        Assert.Empty(jovobeli);
    }

    [Fact]
    public async Task Importalt_eszkoz_letrehozasa_es_modositasa_is_naplozodik()
    {
        var felelos = await FelelosAsync();

        var eszkozId = await ImportAsync(felelos, "A-7001", "Dokumentumkamera");
        await ImportAsync(felelos, "A-7001", "Dokumentumkamera HD");
        var sorok = await NaploAsync(felelos, $"entitas=Eszkoz&entitasId={eszkozId}");
        var kodSorok = await NaploAsync(felelos, "entitas=EszkozKod");

        var letrehozas = sorok.Single(n => n.Muvelet == NaploMuveletek.Letrehozas);
        var modositas = sorok.Single(n => n.Muvelet == NaploMuveletek.Modositas);
        Assert.Contains("\"Megnevezes\":\"Dokumentumkamera\"", letrehozas.UjErtekJson);
        Assert.Contains("\"Ertek\":\"A-7001\"", letrehozas.UjErtekJson);
        Assert.Equal("{\"Megnevezes\":\"Dokumentumkamera\"}", modositas.RegiErtekJson);
        Assert.Equal("{\"Megnevezes\":\"Dokumentumkamera HD\"}", modositas.UjErtekJson);
        Assert.DoesNotContain(kodSorok, n => n.UjErtekJson!.Contains("A-7001"));
    }

    private static async Task<long> ImportAsync(HttpClient kliens, string sap, string megnevezes)
    {
        var fajl = ImportTesztek.Fajl(["SAP szám", "Megnevezés", "Leltárkörzet"], [sap, megnevezes, "MIK-01"]).ToArray();
        var tartalom = new ByteArrayContent(fajl);
        tartalom.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        using var urlap = new MultipartFormDataContent { { tartalom, ImportUrlap.Fajl, "naplo.xlsx" } };
        (await kliens.PostAsync(ApiUtvonalak.Importok, urlap)).EnsureSuccessStatusCode();

        var talalat = await kliens.GetFromJsonAsync<LapozottLista<EszkozListaElem>>($"{ApiUtvonalak.Eszkozok}?kereses={sap}");
        return talalat!.Elemek.Single().Id;
    }
}
