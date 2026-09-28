using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Kozos.Eszkozok;

namespace LeltarKezelo.Szerver.Tesztek;

public class ApiTesztek(SzerverGyar gyar) : IClassFixture<SzerverGyar>
{
    private async Task<HttpClient> BejelentkezettKliensAsync()
    {
        var kliens = gyar.CreateClient();
        var valasz = await kliens.PostAsJsonAsync(ApiUtvonalak.Bejelentkezes,
            new BejelentkezesKeres(SzerverGyar.Felhasznalonev, SzerverGyar.Jelszo));
        valasz.EnsureSuccessStatusCode();
        var adat = await valasz.Content.ReadFromJsonAsync<BejelentkezesValasz>();
        kliens.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adat!.Token);
        return kliens;
    }

    [Fact]
    public async Task Allapot_bejelentkezes_nelkul_elerheto()
    {
        var valasz = await gyar.CreateClient().GetFromJsonAsync<SzerverAllapot>(ApiUtvonalak.Allapot);

        Assert.NotNull(valasz);
        Assert.True(valasz.AdatbazisElerheto);
    }

    [Fact]
    public async Task Eszkozlista_token_nelkul_401()
    {
        var valasz = await gyar.CreateClient().GetAsync(ApiUtvonalak.Eszkozok);

        Assert.Equal(HttpStatusCode.Unauthorized, valasz.StatusCode);
    }

    [Theory]
    [InlineData(SzerverGyar.Felhasznalonev, "rossz-jelszo")]
    [InlineData("nincs-ilyen", SzerverGyar.Jelszo)]
    [InlineData("inaktiv", SzerverGyar.Jelszo)]
    public async Task Hibas_bejelentkezes_401(string felhasznalonev, string jelszo)
    {
        var valasz = await gyar.CreateClient().PostAsJsonAsync(ApiUtvonalak.Bejelentkezes,
            new BejelentkezesKeres(felhasznalonev, jelszo));

        Assert.Equal(HttpStatusCode.Unauthorized, valasz.StatusCode);
        Assert.Equal("application/problem+json", valasz.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Ures_bejelentkezesi_adat_400()
    {
        var valasz = await gyar.CreateClient().PostAsJsonAsync(ApiUtvonalak.Bejelentkezes,
            new BejelentkezesKeres("", ""));

        Assert.Equal(HttpStatusCode.BadRequest, valasz.StatusCode);
    }

    [Fact]
    public async Task Tokenbol_kiolvashato_a_felhasznalo_es_szerepkore()
    {
        var kliens = await BejelentkezettKliensAsync();

        var en = await kliens.GetFromJsonAsync<FelhasznaloAdatok>(ApiUtvonalak.Sajat);

        Assert.Equal(SzerverGyar.Felhasznalonev, en!.Felhasznalonev);
        Assert.Equal([Szerepkorok.Leltarozo], en.Szerepkorok);
    }

    [Fact]
    public async Task Eszkozlista_lapoz()
    {
        var kliens = await BejelentkezettKliensAsync();

        var elso = await kliens.GetFromJsonAsync<LapozottLista<EszkozListaElem>>($"{ApiUtvonalak.Eszkozok}?oldal=1&oldalMeret=2");
        var masodik = await kliens.GetFromJsonAsync<LapozottLista<EszkozListaElem>>($"{ApiUtvonalak.Eszkozok}?oldal=2&oldalMeret=2");

        Assert.Equal(3, elso!.Osszesen);
        Assert.Equal(2, elso.OldalakSzama);
        Assert.Equal(2, elso.Elemek.Count);
        Assert.Single(masodik!.Elemek);
        Assert.Empty(elso.Elemek.Select(e => e.Id).Intersect(masodik.Elemek.Select(e => e.Id)));
    }

    [Fact]
    public async Task Eszkozlista_barmely_kod_alapjan_keres_normalizalva()
    {
        var kliens = await BejelentkezettKliensAsync();

        var lista = await kliens.GetFromJsonAsync<LapozottLista<EszkozListaElem>>($"{ApiUtvonalak.Eszkozok}?kereses=%20l-1001%20");

        var talalat = Assert.Single(lista!.Elemek);
        Assert.Equal(1, talalat.Id);
        Assert.Equal(["SAP-0001", "L-1001"], talalat.Kodok);
    }

    [Fact]
    public async Task Eszkozlista_korzetre_szur()
    {
        var kliens = await BejelentkezettKliensAsync();

        var lista = await kliens.GetFromJsonAsync<LapozottLista<EszkozListaElem>>($"{ApiUtvonalak.Eszkozok}?leltarkorzetId=2");

        Assert.Equal("GTK-01", Assert.Single(lista!.Elemek).LeltarkorzetKod);
    }

    [Fact]
    public async Task Tul_nagy_oldalmeret_korlatozva()
    {
        var kliens = await BejelentkezettKliensAsync();

        var lista = await kliens.GetFromJsonAsync<LapozottLista<EszkozListaElem>>($"{ApiUtvonalak.Eszkozok}?oldalMeret=100000");

        Assert.Equal(EszkozSzuro.MaxOldalMeret, lista!.OldalMeret);
    }

    [Fact]
    public async Task Eszkoz_reszletei_kodtipussal()
    {
        var kliens = await BejelentkezettKliensAsync();

        var eszkoz = await kliens.GetFromJsonAsync<EszkozReszletek>($"{ApiUtvonalak.Eszkozok}/1");

        Assert.Equal("MIK-01", eszkoz!.LeltarkorzetKod);
        Assert.Contains(eszkoz.Kodok, k => k.KodTipus == "Leltári szám 1" && k.Ertek == "L-1001");
    }

    [Fact]
    public async Task Nem_letezo_eszkoz_404()
    {
        var kliens = await BejelentkezettKliensAsync();

        var valasz = await kliens.GetAsync($"{ApiUtvonalak.Eszkozok}/999");

        Assert.Equal(HttpStatusCode.NotFound, valasz.StatusCode);
    }
}
