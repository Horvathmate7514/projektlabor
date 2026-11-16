using System.Net;
using System.Net.Http.Json;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;

namespace LeltarKezelo.Szerver.Tesztek;

/// <summary>
/// Kiegészítők: közvetlen és feltételezett ellenőrzés a leltárban, a kapcsolat felvétele és lezárása
/// (8. alkalom, F-19–F-21, D-044). Minden teszt saját leltáridőszakot nyit, így nem zavarják egymást.
/// </summary>
public class KiegeszitoTesztek(KiegeszitoTesztek.Gyar gyar) : IClassFixture<KiegeszitoTesztek.Gyar>
{
    public sealed class Gyar : LeltarGyar
    {
        protected override void Feltoltes(LeltarDbContext db)
        {
            base.Feltoltes(db);
            var kezdet = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            db.Kiegeszitok.AddRange(
                new Kiegeszito { Id = MonitorKapcsolat, FoEszkozId = Szamitogep, KiegeszitoEszkozId = Monitor, ErvenyesTol = kezdet },
                new Kiegeszito { Id = TartozekKapcsolat, FoEszkozId = Szamitogep, Leiras = "Billentyűzet és egér", ErvenyesTol = kezdet });
        }
    }

    private const long Szamitogep = 1;
    private const long Monitor = 2;
    private const long Szek = 3;
    private const long RegiNyomtato = 5;
    private const long MonitorKapcsolat = 1;
    private const long TartozekKapcsolat = 2;

    private Task<HttpClient> LeltarozoAsync() => gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);

    private static async Task<Dictionary<long, KiegeszitoEllenorzesMod?>> IdoszakbanAsync(HttpClient kliens, long idoszakId)
    {
        var lista = await kliens.GetFromJsonAsync<List<KiegeszitoAdat>>($"{ApiUtvonalak.EszkozKiegeszitoi(Szamitogep)}?leltarIdoszakId={idoszakId}");
        return lista!.ToDictionary(k => k.Id, k => k.Ellenorzes);
    }

    private static async Task SztornoAsync(HttpClient kliens, long leolvasasId) =>
        (await kliens.PostAsJsonAsync(ApiUtvonalak.Sztorno(leolvasasId), new SztornoKeres("Teszt: téves beolvasás"))).EnsureSuccessStatusCode();

    [Fact]
    public async Task Fo_eszkoz_beolvasasakor_a_kiegeszitok_feltetelezetten_megvannak()
    {
        var leltarozo = await LeltarozoAsync();
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        var elotte = await IdoszakbanAsync(leltarozo, id);

        var beolvasas = await OsszesitesTesztek.BeolvasAsync(leltarozo, id, "SAP-0001");
        var leolvasasnal = await leltarozo.GetFromJsonAsync<List<KiegeszitoAdat>>(ApiUtvonalak.LeolvasasKiegeszitoi(beolvasas.LeolvasasId));
        var utana = await IdoszakbanAsync(leltarozo, id);
        var idoszakNelkul = await leltarozo.GetFromJsonAsync<List<KiegeszitoAdat>>(ApiUtvonalak.EszkozKiegeszitoi(Szamitogep));

        Assert.All(elotte.Values, Assert.Null);
        Assert.Equal(["Monitor 24\"", "Billentyűzet és egér"], leolvasasnal!.Select(k => k.Megnevezes));
        Assert.All(leolvasasnal!, k => Assert.Equal(KiegeszitoEllenorzesMod.Feltetelezett, k.Ellenorzes));
        Assert.Equal(KiegeszitoEllenorzesMod.Feltetelezett, utana[MonitorKapcsolat]);
        Assert.Equal(KiegeszitoEllenorzesMod.Feltetelezett, utana[TartozekKapcsolat]);
        Assert.All(idoszakNelkul!, k => Assert.Null(k.Ellenorzes));
    }

    [Fact]
    public async Task A_kiegeszito_sajat_kodjanak_beolvasasa_kozvetlen_ellenorzes_a_sztornoja_visszaallitja()
    {
        var leltarozo = await LeltarozoAsync();
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        await OsszesitesTesztek.BeolvasAsync(leltarozo, id, "SAP-0001");
        var monitor = await OsszesitesTesztek.BeolvasAsync(leltarozo, id, "SAP-0002");
        var kozvetlen = await IdoszakbanAsync(leltarozo, id);
        await SztornoAsync(leltarozo, monitor.LeolvasasId);
        var sztornoUtan = await IdoszakbanAsync(leltarozo, id);

        Assert.Equal(KiegeszitoEllenorzesMod.Kozvetlen, kozvetlen[MonitorKapcsolat]);
        Assert.Equal(KiegeszitoEllenorzesMod.Feltetelezett, kozvetlen[TartozekKapcsolat]);
        Assert.Equal(KiegeszitoEllenorzesMod.Feltetelezett, sztornoUtan[MonitorKapcsolat]);
    }

    [Fact]
    public async Task Ha_a_kiegeszitot_olvassak_be_elobb_a_fo_eszkoznel_mar_kozvetlen()
    {
        var leltarozo = await LeltarozoAsync();
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        await OsszesitesTesztek.BeolvasAsync(leltarozo, id, "SAP-0002");
        var csakKiegeszito = await IdoszakbanAsync(leltarozo, id);
        await OsszesitesTesztek.BeolvasAsync(leltarozo, id, "SAP-0001");
        var mindketto = await IdoszakbanAsync(leltarozo, id);

        Assert.Null(csakKiegeszito[MonitorKapcsolat]);
        Assert.Equal(KiegeszitoEllenorzesMod.Kozvetlen, mindketto[MonitorKapcsolat]);
        Assert.Equal(KiegeszitoEllenorzesMod.Feltetelezett, mindketto[TartozekKapcsolat]);
    }

    [Fact]
    public async Task A_fo_eszkoz_leolvasasanak_sztornoja_utan_a_kiegeszitok_nincsenek_ellenorizve()
    {
        var leltarozo = await LeltarozoAsync();
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var beolvasas = await OsszesitesTesztek.BeolvasAsync(leltarozo, id, "SAP-0001");
        await SztornoAsync(leltarozo, beolvasas.LeolvasasId);
        var utana = await IdoszakbanAsync(leltarozo, id);

        Assert.All(utana.Values, Assert.Null);
    }

    [Fact]
    public async Task Kod_nelkuli_tartozek_kezzel_megerositheto_a_sajat_koddal_rendelkezo_nem()
    {
        var leltarozo = await LeltarozoAsync();
        var masik = await gyar.BejelentkezettKliensAsync();
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        var beolvasas = await OsszesitesTesztek.BeolvasAsync(leltarozo, id, "SAP-0001");
        var tartozek = ApiUtvonalak.LeolvasasKiegeszitoje(beolvasas.LeolvasasId, TartozekKapcsolat);

        var megerosites = await leltarozo.PutAsJsonAsync(tartozek, new KiegeszitoEllenorzesKeres(true));
        var megerositve = await IdoszakbanAsync(leltarozo, id);
        var sajatKodu = await leltarozo.PutAsJsonAsync(
            ApiUtvonalak.LeolvasasKiegeszitoje(beolvasas.LeolvasasId, MonitorKapcsolat), new KiegeszitoEllenorzesKeres(true));
        var masLeolvasasa = await masik.PutAsJsonAsync(tartozek, new KiegeszitoEllenorzesKeres(false));
        var nincsIlyen = await leltarozo.PutAsJsonAsync(
            ApiUtvonalak.LeolvasasKiegeszitoje(beolvasas.LeolvasasId, 9999), new KiegeszitoEllenorzesKeres(true));
        var visszavonas = await leltarozo.PutAsJsonAsync(tartozek, new KiegeszitoEllenorzesKeres(false));
        var visszavonva = await IdoszakbanAsync(leltarozo, id);

        Assert.Equal(HttpStatusCode.OK, megerosites.StatusCode);
        Assert.Equal(KiegeszitoEllenorzesMod.Kozvetlen, megerositve[TartozekKapcsolat]);
        Assert.Equal(HttpStatusCode.BadRequest, sajatKodu.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, masLeolvasasa.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, nincsIlyen.StatusCode);
        Assert.Equal(HttpStatusCode.OK, visszavonas.StatusCode);
        Assert.Equal(KiegeszitoEllenorzesMod.Feltetelezett, visszavonva[TartozekKapcsolat]);
    }

    [Fact]
    public async Task Lezart_leltarban_a_kiegeszitok_ellenorzese_nem_modosithato()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        var beolvasas = await OsszesitesTesztek.BeolvasAsync(felelos, id, "SAP-0001");
        (await felelos.PostAsync(ApiUtvonalak.Lezaras(id), null)).EnsureSuccessStatusCode();

        var valasz = await felelos.PutAsJsonAsync(
            ApiUtvonalak.LeolvasasKiegeszitoje(beolvasas.LeolvasasId, TartozekKapcsolat), new KiegeszitoEllenorzesKeres(true));
        var megmaradt = await IdoszakbanAsync(felelos, id);

        Assert.Equal(HttpStatusCode.Conflict, valasz.StatusCode);
        Assert.Equal(KiegeszitoEllenorzesMod.Feltetelezett, megmaradt[TartozekKapcsolat]);
    }

    [Fact]
    public async Task Kapcsolat_felvetele_ellenorzessel_es_lezarasa_torles_nelkul()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var leltarozo = await LeltarozoAsync();
        var url = ApiUtvonalak.EszkozKiegeszitoi(Szek);

        var felvetel = await felelos.PostAsJsonAsync(url, new KiegeszitoFelvetel(null, "  Karfa  ", 2));
        var uj = await felvetel.Content.ReadFromJsonAsync<KiegeszitoAdat>();
        var hibasak = new[]
        {
            await felelos.PostAsJsonAsync(url, new KiegeszitoFelvetel(null)),
            await felelos.PostAsJsonAsync(url, new KiegeszitoFelvetel(Szek)),
            await felelos.PostAsJsonAsync(url, new KiegeszitoFelvetel(RegiNyomtato)),
            await felelos.PostAsJsonAsync(url, new KiegeszitoFelvetel(null, "Párna", 0)),
            await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozKiegeszitoi(RegiNyomtato), new KiegeszitoFelvetel(null, "Tálca")),
            await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozKiegeszitoi(Monitor), new KiegeszitoFelvetel(Szamitogep)),
        };
        var marKiegeszito = await felelos.PostAsJsonAsync(url, new KiegeszitoFelvetel(Monitor));
        var jogosulatlan = await leltarozo.PostAsJsonAsync(url, new KiegeszitoFelvetel(null, "Párna"));

        Assert.Equal(HttpStatusCode.Created, felvetel.StatusCode);
        Assert.Equal(("Karfa", 2, null), (uj!.Megnevezes, uj.Mennyiseg, uj.ErvenyesIg));
        Assert.All(hibasak, v => Assert.Equal(HttpStatusCode.BadRequest, v.StatusCode));
        Assert.Equal(HttpStatusCode.Conflict, marKiegeszito.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, jogosulatlan.StatusCode);

        var lezaras = await felelos.PostAsync(ApiUtvonalak.KiegeszitoLezaras(uj.Id), null);
        var lezart = await lezaras.Content.ReadFromJsonAsync<KiegeszitoAdat>();
        var ujra = await felelos.PostAsync(ApiUtvonalak.KiegeszitoLezaras(uj.Id), null);
        var nincsIlyen = await felelos.PostAsync(ApiUtvonalak.KiegeszitoLezaras(9999), null);
        var aktivak = await felelos.GetFromJsonAsync<List<KiegeszitoAdat>>(url);
        var mind = await felelos.GetFromJsonAsync<List<KiegeszitoAdat>>($"{url}?lezartakkal=true");

        Assert.Equal(HttpStatusCode.OK, lezaras.StatusCode);
        Assert.NotNull(lezart!.ErvenyesIg);
        Assert.Equal(HttpStatusCode.Conflict, ujra.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, nincsIlyen.StatusCode);
        Assert.DoesNotContain(aktivak!, k => k.Id == uj.Id);
        Assert.Contains(mind!, k => k.Id == uj.Id);
    }
}
