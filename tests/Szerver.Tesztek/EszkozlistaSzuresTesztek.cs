using System.Net;
using System.Net.Http.Json;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Kozos.Torzsadatok;

namespace LeltarKezelo.Szerver.Tesztek;

/// <summary>
/// Törzsadat-végpontok, az eszközlista bővített szűrése és a szerepkör szerinti láthatóság
/// (5. alkalom 4–6. feladat, jogosultsági mátrix).
/// </summary>
public class EszkozlistaSzuresTesztek(LeltarGyar gyar) : IClassFixture<LeltarGyar>
{
    private async Task<LapozottLista<EszkozListaElem>> ListaAsync(string lekerdezes, string felhasznalo = LeltarGyar.Felelos)
    {
        var kliens = await gyar.BejelentkezettKliensAsync(felhasznalo);
        return (await kliens.GetFromJsonAsync<LapozottLista<EszkozListaElem>>($"{ApiUtvonalak.Eszkozok}?{lekerdezes}"))!;
    }

    // --- Törzsadatok ---

    [Fact]
    public async Task Torzsadatok_listazhatok()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Megtekinto);

        var tipusok = await kliens.GetFromJsonAsync<List<EszkozTipusAdat>>(ApiUtvonalak.EszkozTipusok);
        var allapotok = await kliens.GetFromJsonAsync<List<EszkozAllapotAdat>>(ApiUtvonalak.EszkozAllapotok);
        var helyisegek = await kliens.GetFromJsonAsync<List<HelyisegAdat>>(ApiUtvonalak.Helyisegek);
        var felelosok = await kliens.GetFromJsonAsync<List<FelelosAdat>>(ApiUtvonalak.Felelosok);

        Assert.Equal(LeltarGyar.SzamitastechnikaTipus, tipusok!.Single(t => t.Nev == "Monitor").SzuloId);
        Assert.True(allapotok!.Single(a => a.Nev == "selejtezett").Megszunt);
        Assert.Equal(["101", "102"], helyisegek!.Select(h => h.Szobaszam));
        Assert.Equal("Kovács Anna", Assert.Single(felelosok!).Nev);
    }

    [Fact]
    public async Task Torzsadatok_token_nelkul_401()
    {
        var valasz = await gyar.CreateClient().GetAsync(ApiUtvonalak.Helyisegek);

        Assert.Equal(HttpStatusCode.Unauthorized, valasz.StatusCode);
    }

    // --- Szűrés ---

    [Fact]
    public async Task Tipusszuro_az_altipusokat_is_tartalmazza()
    {
        var foTipus = await ListaAsync($"eszkozTipusId={LeltarGyar.SzamitastechnikaTipus}");
        var altipus = await ListaAsync($"eszkozTipusId={LeltarGyar.MonitorTipus}");

        Assert.Equal([1L, 2L], foTipus.Elemek.Select(e => e.Id).Order());
        Assert.Equal(2, Assert.Single(altipus.Elemek).Id);
    }

    [Fact]
    public async Task Allapotra_szur()
    {
        var lista = await ListaAsync("eszkozAllapotId=5");

        Assert.Equal("selejtezett", Assert.Single(lista.Elemek).Allapot);
    }

    [Fact]
    public async Task Helyisegszuro_csak_az_aktualis_helyet_nezi()
    {
        var lista = await ListaAsync($"helyisegId={LeltarGyar.Terem101}");

        var eszkoz = Assert.Single(lista.Elemek); // a monitor csak korábban volt itt
        Assert.Equal(1, eszkoz.Id);
        Assert.Equal("I 101", eszkoz.Helyiseg);
    }

    [Fact]
    public async Task Felelosre_szur_es_a_listaban_latszik_a_felelos()
    {
        var lista = await ListaAsync($"felelosId={LeltarGyar.KovacsAnna}");

        var eszkoz = Assert.Single(lista.Elemek);
        Assert.Equal(2, eszkoz.Id);
        Assert.Equal("Kovács Anna", eszkoz.Felelos);
        Assert.Equal("I 102", eszkoz.Helyiseg);
    }

    [Fact]
    public async Task Szurok_kombinalhatok()
    {
        var talalat = await ListaAsync($"eszkozTipusId={LeltarGyar.SzamitastechnikaTipus}&felelosId={LeltarGyar.KovacsAnna}");
        var ures = await ListaAsync($"eszkozTipusId={LeltarGyar.SzamitastechnikaTipus}&eszkozAllapotId=5");

        Assert.Equal(2, Assert.Single(talalat.Elemek).Id);
        Assert.Empty(ures.Elemek);
        Assert.Equal(0, ures.Osszesen);
    }

    // --- Láthatóság ---

    [Fact]
    public async Task Leltarfelelos_minden_eszkozt_lat()
    {
        var lista = await ListaAsync("");

        Assert.Equal(5, lista.Osszesen);
    }

    [Fact]
    public async Task Leltarozo_csak_a_jogosult_korzetei_eszkozeit_latja()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);

        var lista = await kliens.GetFromJsonAsync<LapozottLista<EszkozListaElem>>(ApiUtvonalak.Eszkozok);
        var idegen = await kliens.GetAsync($"{ApiUtvonalak.Eszkozok}/3");

        Assert.All(lista!.Elemek, e => Assert.Equal("MIK-01", e.LeltarkorzetKod));
        Assert.Equal(4, lista.Osszesen);
        Assert.Equal(HttpStatusCode.Forbidden, idegen.StatusCode);
    }

    [Fact]
    public async Task Megtekinto_a_sajat_szervezeti_egysege_korzeteit_latja()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Megtekinto);

        var lista = await kliens.GetFromJsonAsync<LapozottLista<EszkozListaElem>>(ApiUtvonalak.Eszkozok);
        var sajat = await kliens.GetAsync($"{ApiUtvonalak.Eszkozok}/3");
        var idegen = await kliens.GetAsync($"{ApiUtvonalak.Eszkozok}/1");

        Assert.Equal(3, Assert.Single(lista!.Elemek).Id);
        Assert.Equal(HttpStatusCode.OK, sajat.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, idegen.StatusCode);
    }

    [Fact]
    public async Task Korzet_nelkuli_leltarozo_ures_listat_kap()
    {
        var lista = await ListaAsync("", LeltarGyar.KorzetNelkuli);

        Assert.Empty(lista.Elemek);
    }
}
