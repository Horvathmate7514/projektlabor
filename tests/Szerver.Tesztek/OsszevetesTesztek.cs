using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Szolgaltatasok;

namespace LeltarKezelo.Szerver.Tesztek;

public class OsszevetesTesztek(LeltarGyar gyar) : IClassFixture<LeltarGyar>
{
    private const long Szamitogep = 1;
    private const long Monitor = 2;
    private const long Szek = 3;

    [Theory]
    [InlineData(null, TetelAllapot.Rendben, TetelValtozas.UjTetel)]
    [InlineData(TetelAllapot.Rendben, null, TetelValtozas.KikerultTetel)]
    [InlineData(TetelAllapot.Hianyzik, TetelAllapot.Hianyzik, TetelValtozas.TartosHiany)]
    [InlineData(TetelAllapot.Hianyzik, TetelAllapot.MasKorzetben, TetelValtozas.Megkerult)]
    [InlineData(TetelAllapot.Rendben, TetelAllapot.Hianyzik, TetelValtozas.UjHiany)]
    [InlineData(TetelAllapot.Rendben, TetelAllapot.Rendben, TetelValtozas.Valtozatlan)]
    [InlineData(TetelAllapot.Rendben, TetelAllapot.MasKorzetben, TetelValtozas.AllapotValtozott)]
    public void Valtozas_minositese(TetelAllapot? korabbi, TetelAllapot? aktualis, TetelValtozas vart) =>
        Assert.Equal(vart, OsszesitesSzolgaltatas.Valtozas(korabbi, aktualis));

    private async Task<(HttpClient Felelos, long Korabbi, long Aktualis)> KetIdoszakAsync()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

        var korabbi = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        await OsszesitesTesztek.BeolvasAsync(felelos, korabbi, "SAP-0001");
        await OsszesitesTesztek.BeolvasAsync(felelos, korabbi, "SAP-0003", LeltarGyar.GtkKorzet);
        (await felelos.PostAsync(ApiUtvonalak.Lezaras(korabbi), null)).EnsureSuccessStatusCode();

        var aktualis = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        await OsszesitesTesztek.BeolvasAsync(felelos, aktualis, "SAP-0002");
        await OsszesitesTesztek.BeolvasAsync(felelos, aktualis, "SAP-0003", LeltarGyar.GtkKorzet);
        return (felelos, korabbi, aktualis);
    }

    [Fact]
    public async Task Ket_idoszak_eszkozonkenti_osszevetese()
    {
        var (felelos, korabbi, aktualis) = await KetIdoszakAsync();

        var eredmeny = await felelos.GetFromJsonAsync<IdoszakOsszevetes>(
            $"{ApiUtvonalak.Osszevetes(aktualis)}?korabbiIdoszakId={korabbi}&oldalMeret=200");

        var tetelek = eredmeny!.Tetelek.Elemek.ToDictionary(t => t.EszkozId);
        Assert.Equal(TetelValtozas.UjHiany, tetelek[Szamitogep].Valtozas);
        Assert.Equal(TetelValtozas.Megkerult, tetelek[Monitor].Valtozas);
        Assert.Equal(TetelValtozas.Valtozatlan, tetelek[Szek].Valtozas);
        Assert.Equal(TetelAllapot.Rendben, tetelek[Szamitogep].KorabbiAllapot);
        Assert.Equal(TetelAllapot.Hianyzik, tetelek[Szamitogep].AktualisAllapot);
        Assert.Equal(1, eredmeny.Darabok[TetelValtozas.UjHiany]);
    }

    [Fact]
    public async Task Osszevetes_szurheto_valtozasra()
    {
        var (felelos, korabbi, aktualis) = await KetIdoszakAsync();

        var csakValtozasok = await felelos.GetFromJsonAsync<IdoszakOsszevetes>(
            $"{ApiUtvonalak.Osszevetes(aktualis)}?korabbiIdoszakId={korabbi}&csakValtozasok=true&oldalMeret=200");
        var megkerultek = await felelos.GetFromJsonAsync<IdoszakOsszevetes>(
            $"{ApiUtvonalak.Osszevetes(aktualis)}?korabbiIdoszakId={korabbi}&valtozas=Megkerult");

        Assert.DoesNotContain(csakValtozasok!.Tetelek.Elemek, t => t.Valtozas == TetelValtozas.Valtozatlan);
        Assert.All(megkerultek!.Tetelek.Elemek, t => Assert.Equal(TetelValtozas.Megkerult, t.Valtozas));
        Assert.Contains(megkerultek.Tetelek.Elemek, t => t.EszkozId == Monitor);
    }

    [Fact]
    public async Task Onmagaval_vagy_nem_letezo_idoszakkal_nem_vetheto_ossze()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var idoszak = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var onmaga = await felelos.GetAsync($"{ApiUtvonalak.Osszevetes(idoszak)}?korabbiIdoszakId={idoszak}");
        var nincs = await felelos.GetAsync($"{ApiUtvonalak.Osszevetes(idoszak)}?korabbiIdoszakId=9999");

        Assert.Equal(HttpStatusCode.BadRequest, onmaga.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, nincs.StatusCode);
    }

    [Fact]
    public async Task Tetelek_Excelbe_exportalhatok_szuressel()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var idoszak = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        await OsszesitesTesztek.BeolvasAsync(felelos, idoszak, "SAP-0001");

        var valasz = await felelos.GetAsync($"{ApiUtvonalak.TetelekExcel(idoszak)}?allapot=Hianyzik");

        valasz.EnsureSuccessStatusCode();
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", valasz.Content.Headers.ContentType!.MediaType);
        using var munkafuzet = new XLWorkbook(await valasz.Content.ReadAsStreamAsync());
        Assert.True(munkafuzet.Worksheets.Contains("Összesítés"));
        var lap = munkafuzet.Worksheet("Tételek");
        var allapotok = lap.Column(3).CellsUsed().Skip(1).Select(c => c.GetString()).ToList();
        Assert.NotEmpty(allapotok);
        Assert.All(allapotok, a => Assert.Equal("Hiányzik", a));
        Assert.DoesNotContain(lap.Column(1).CellsUsed(), c => c.GetString() == Szamitogep.ToString());
    }

    [Fact]
    public async Task Nyitott_leltarban_megszunt_eszkoz_jelolve_marad_elvart()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var idoszak = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        (await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozAllapot(Szek),
            new AllapotKeres(5, "Selejtezve a leltár alatt"))).EnsureSuccessStatusCode();

        var szek = (await OsszesitesTesztek.TetelekAsync(felelos, idoszak)).Single(t => t.EszkozId == Szek);
        Assert.True(szek.Megszunt);
        Assert.Equal(TetelAllapot.Hianyzik, szek.Allapot);
    }
}
