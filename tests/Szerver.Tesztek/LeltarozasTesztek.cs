using System.Net;
using System.Net.Http.Json;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Kozos.Torzsadatok;
using LeltarKezelo.Szerver.Szolgaltatasok;

namespace LeltarKezelo.Szerver.Tesztek;

public class LeltarozasTesztek(LeltarGyar gyar) : IClassFixture<LeltarGyar>
{
    private async Task<long> NyitottIdoszakAsync()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var letrehozas = await felelos.PostAsJsonAsync(ApiUtvonalak.LeltarIdoszakok,
            new LeltarIdoszakLetrehozas("Teszt leltár", LeltarIdoszakTipus.Rendkivuli, new DateOnly(2026, 10, 1)));
        Assert.Equal(HttpStatusCode.Created, letrehozas.StatusCode);
        var idoszak = await letrehozas.Content.ReadFromJsonAsync<LeltarIdoszakAdat>();

        (await felelos.PostAsync(ApiUtvonalak.Nyitas(idoszak!.Id), null)).EnsureSuccessStatusCode();
        return idoszak.Id;
    }

    private static async Task<BeolvasasEredmeny> BeolvasAsync(
        HttpClient kliens, long idoszakId, string kod, long korzetId = LeltarGyar.MikKorzet, int mennyiseg = 1)
    {
        var valasz = await kliens.PostAsJsonAsync(ApiUtvonalak.IdoszakLeolvasasai(idoszakId),
            new BeolvasasKeres(kod, korzetId, Mennyiseg: mennyiseg));
        valasz.EnsureSuccessStatusCode();
        return (await valasz.Content.ReadFromJsonAsync<BeolvasasEredmeny>())!;
    }

    private static Task<HttpResponseMessage> BeolvasasKeresAsync(HttpClient kliens, long idoszakId, BeolvasasKeres keres) =>
        kliens.PostAsJsonAsync(ApiUtvonalak.IdoszakLeolvasasai(idoszakId), keres);

    [Theory]
    [InlineData(false, false, 1, 0, 1, Minosites.Ok)]
    [InlineData(false, false, 1, 1, 1, Minosites.Ismetelt)]
    [InlineData(true, false, 1, 1, 1, Minosites.Ismetelt)]
    [InlineData(true, true, 1, 0, 1, Minosites.NemAktiv)]
    [InlineData(false, true, 1, 0, 1, Minosites.MasKorzet)]
    [InlineData(false, true, 30, 30, 1, Minosites.MasKorzet)]
    [InlineData(false, false, 30, 29, 1, Minosites.Ok)]
    [InlineData(false, false, 30, 30, 1, Minosites.Tobblet)]
    [InlineData(false, false, 1, 0, 2, Minosites.Tobblet)]
    public void Minosites_elsobbsegi_sorrendje(bool megszunt, bool masKorzet, int elvart, int eddig, int mennyiseg, Minosites vart) =>
        Assert.Equal(vart, BeolvasasSzolgaltatas.Minosit(megszunt, masKorzet, elvart, eddig, mennyiseg));

    [Fact]
    public async Task Nyitas_pillanatkepet_keszit_a_nem_megszunt_eszkozokrol()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync();

        var idoszak = await felelos.GetFromJsonAsync<LeltarIdoszakAdat>($"{ApiUtvonalak.LeltarIdoszakok}/{id}");

        Assert.Equal(LeltarIdoszakStatusz.Nyitott, idoszak!.Statusz);
        Assert.Equal(4, idoszak.ElvartTetelekSzama);
    }

    [Fact]
    public async Task Nyitott_idoszak_nem_nyithato_ujra_409()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync();

        var valasz = await felelos.PostAsync(ApiUtvonalak.Nyitas(id), null);

        Assert.Equal(HttpStatusCode.Conflict, valasz.StatusCode);
        Assert.Equal("application/problem+json", valasz.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Idoszak_vege_nem_lehet_a_kezdete_elott_400()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

        var valasz = await felelos.PostAsJsonAsync(ApiUtvonalak.LeltarIdoszakok, new LeltarIdoszakLetrehozas(
            "Hibás", LeltarIdoszakTipus.Eves, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 1)));

        Assert.Equal(HttpStatusCode.BadRequest, valasz.StatusCode);
    }

    [Theory]
    [InlineData(LeltarGyar.Leltarozo)]
    [InlineData(LeltarGyar.Megtekinto)]
    public async Task Idoszakot_csak_leltarfelelos_vagy_admin_hozhat_letre_403(string felhasznalonev)
    {
        var kliens = await gyar.BejelentkezettKliensAsync(felhasznalonev);

        var valasz = await kliens.PostAsJsonAsync(ApiUtvonalak.LeltarIdoszakok,
            new LeltarIdoszakLetrehozas("Tiltott", LeltarIdoszakTipus.Eves, new DateOnly(2026, 10, 1)));

        Assert.Equal(HttpStatusCode.Forbidden, valasz.StatusCode);
    }

    [Fact]
    public async Task Nyitott_idoszakok_szurhetok()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Megtekinto);
        var id = await NyitottIdoszakAsync();

        var nyitottak = await kliens.GetFromJsonAsync<List<LeltarIdoszakAdat>>($"{ApiUtvonalak.LeltarIdoszakok}?statusz=Nyitott");

        Assert.Contains(nyitottak!, i => i.Id == id);
        Assert.All(nyitottak!, i => Assert.Equal(LeltarIdoszakStatusz.Nyitott, i.Statusz));
    }

    [Fact]
    public async Task Korzetvalaszto_jelzi_hol_leltarozhat_a_felhasznalo()
    {
        var leltarozo = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

        var sajat = await leltarozo.GetFromJsonAsync<List<LeltarkorzetAdat>>(ApiUtvonalak.Leltarkorzetek);
        var mind = await felelos.GetFromJsonAsync<List<LeltarkorzetAdat>>(ApiUtvonalak.Leltarkorzetek);

        Assert.True(sajat!.Single(k => k.Id == LeltarGyar.MikKorzet).Leltarozhato);
        Assert.False(sajat!.Single(k => k.Id == LeltarGyar.GtkKorzet).Leltarozhato);
        Assert.All(mind!, k => Assert.True(k.Leltarozhato));
    }

    [Fact]
    public async Task Kodtipusok_listazhatok()
    {
        var kliens = await gyar.BejelentkezettKliensAsync();

        var tipusok = await kliens.GetFromJsonAsync<List<KodTipusAdat>>(ApiUtvonalak.KodTipusok);

        Assert.Contains(tipusok!, t => t.Nev == "SAP szám");
    }

    [Fact]
    public async Task Ervenyes_kod_OK_es_megmondja_melyik_kodtipus_illeszkedett()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var eredmeny = await BeolvasAsync(kliens, id, "SAP-0001");

        Assert.Equal(Minosites.Ok, eredmeny.Minosites);
        Assert.True(eredmeny.LeolvasasId > 0);
        Assert.Equal(1, eredmeny.Eszkoz!.Id);
        Assert.Equal("SAP szám", eredmeny.Eszkoz.KodTipus);
        Assert.Equal(1, eredmeny.BeolvasottMennyiseg);
    }

    [Fact]
    public async Task Masodlagos_kod_alapjan_is_azonosit_normalizalva()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var eredmeny = await BeolvasAsync(kliens, id, "  l-1001\t");

        Assert.Equal(Minosites.Ok, eredmeny.Minosites);
        Assert.Equal(1, eredmeny.Eszkoz!.Id);
        Assert.Equal("Leltári szám 1", eredmeny.Eszkoz.KodTipus);
    }

    [Theory]
    [InlineData("NINCS-ILYEN")]
    [InlineData("REGI-0001")]
    public async Task Ismeretlen_kod_is_rogzul(string kod)
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var eredmeny = await BeolvasAsync(kliens, id, kod);
        var lista = await kliens.GetFromJsonAsync<LapozottLista<LeolvasasListaElem>>(ApiUtvonalak.IdoszakLeolvasasai(id));

        Assert.Equal(Minosites.IsmeretlenKod, eredmeny.Minosites);
        Assert.Null(eredmeny.Eszkoz);
        var sor = Assert.Single(lista!.Elemek);
        Assert.Equal(eredmeny.LeolvasasId, sor.Id);
        Assert.Equal(kod, sor.BeolvasottKod);
        Assert.Null(sor.EszkozId);
    }

    [Fact]
    public async Task Masik_korzet_eszkoze_rogzul_es_megnevezi_a_korzetet()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var eredmeny = await BeolvasAsync(kliens, id, "SAP-0003");

        Assert.Equal(Minosites.MasKorzet, eredmeny.Minosites);
        Assert.Equal("GTK-01", eredmeny.Eszkoz!.LeltarkorzetKod);
        Assert.Contains("GTK-01", eredmeny.Uzenet);
    }

    [Fact]
    public async Task Egyedi_eszkoz_ismetelt_beolvasasa_nem_noveli_a_darabszamot()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var elso = await BeolvasAsync(kliens, id, "SAP-0002");
        var masodik = await BeolvasAsync(kliens, id, "sap-0002");

        Assert.Equal(Minosites.Ok, elso.Minosites);
        Assert.Null(elso.KorabbiBeolvasas);
        Assert.Equal(Minosites.Ismetelt, masodik.Minosites);
        Assert.Equal(1, masodik.BeolvasottMennyiseg);
        Assert.NotNull(masodik.KorabbiBeolvasas);
        Assert.NotEqual(elso.LeolvasasId, masodik.LeolvasasId);
    }

    [Fact]
    public async Task Mennyiseges_eszkoz_elvart_folott_tobblet()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var elso = await BeolvasAsync(kliens, id, "SZEK-30", mennyiseg: 2);
        var masodik = await BeolvasAsync(kliens, id, "SZEK-30", mennyiseg: 2);

        Assert.Equal(Minosites.Ok, elso.Minosites);
        Assert.Equal(Minosites.Tobblet, masodik.Minosites);
        Assert.Equal(3, masodik.ElvartMennyiseg);
        Assert.Equal(4, masodik.BeolvasottMennyiseg);
    }

    [Fact]
    public async Task Selejtezett_eszkoz_nem_aktivkent_rogzul()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var eredmeny = await BeolvasAsync(kliens, id, "SAP-0005");

        Assert.Equal(Minosites.NemAktiv, eredmeny.Minosites);
        Assert.Equal("selejtezett", eredmeny.Eszkoz!.Allapot);
    }

    [Fact]
    public async Task A_minosites_szovegkent_utazik_a_JSON_ban()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var valasz = await BeolvasasKeresAsync(kliens, id, new BeolvasasKeres("SAP-0003", LeltarGyar.MikKorzet));
        var json = await valasz.Content.ReadAsStringAsync();

        Assert.Contains("\"minosites\":\"MasKorzet\"", json);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Ures_kod_400(string kod)
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var valasz = await BeolvasasKeresAsync(kliens, id, new BeolvasasKeres(kod, LeltarGyar.MikKorzet));

        Assert.Equal(HttpStatusCode.BadRequest, valasz.StatusCode);
    }

    [Fact]
    public async Task Tul_hosszu_kod_es_ervenytelen_mennyiseg_400()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var hosszu = await BeolvasasKeresAsync(kliens, id, new BeolvasasKeres(new string('X', 65), LeltarGyar.MikKorzet));
        var nulla = await BeolvasasKeresAsync(kliens, id, new BeolvasasKeres("SAP-0001", LeltarGyar.MikKorzet, Mennyiseg: 0));

        Assert.Equal(HttpStatusCode.BadRequest, hosszu.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, nulla.StatusCode);
    }

    [Fact]
    public async Task Nem_letezo_korzet_400_nem_letezo_idoszak_404()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync();

        var korzet = await BeolvasasKeresAsync(kliens, id, new BeolvasasKeres("SAP-0001", 999));
        var idoszak = await BeolvasasKeresAsync(kliens, 999_999, new BeolvasasKeres("SAP-0001", LeltarGyar.MikKorzet));

        Assert.Equal(HttpStatusCode.BadRequest, korzet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, idoszak.StatusCode);
    }

    [Fact]
    public async Task Meg_nem_nyitott_idoszakba_nem_olvashato_be_409()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var letrehozas = await felelos.PostAsJsonAsync(ApiUtvonalak.LeltarIdoszakok,
            new LeltarIdoszakLetrehozas("Tervezett", LeltarIdoszakTipus.Eves, new DateOnly(2026, 12, 1)));
        var idoszak = await letrehozas.Content.ReadFromJsonAsync<LeltarIdoszakAdat>();

        var valasz = await BeolvasasKeresAsync(felelos, idoszak!.Id, new BeolvasasKeres("SAP-0001", LeltarGyar.MikKorzet));

        Assert.Equal(HttpStatusCode.Conflict, valasz.StatusCode);
    }

    [Fact]
    public async Task Lezart_idoszak_csak_olvashato_409()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync();
        var leolvasas = await BeolvasAsync(felelos, id, "SAP-0001");

        var lezaras = await felelos.PostAsync(ApiUtvonalak.Lezaras(id), null);
        var lezart = await lezaras.Content.ReadFromJsonAsync<LeltarIdoszakAdat>();
        var beolvasas = await BeolvasasKeresAsync(felelos, id, new BeolvasasKeres("SAP-0002", LeltarGyar.MikKorzet));
        var sztorno = await felelos.PostAsJsonAsync(ApiUtvonalak.Sztorno(leolvasas.LeolvasasId), new SztornoKeres("Teszt"));

        Assert.Equal(LeltarIdoszakStatusz.Lezart, lezart!.Statusz);
        Assert.NotNull(lezart.Lezarva);
        Assert.Equal(HttpStatusCode.Conflict, beolvasas.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, sztorno.StatusCode);
    }

    [Fact]
    public async Task Leltarozo_csak_a_jogosult_korzeteben_olvashat_be()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var sajat = await BeolvasasKeresAsync(kliens, id, new BeolvasasKeres("SAP-0001", LeltarGyar.MikKorzet));
        var idegen = await BeolvasasKeresAsync(kliens, id, new BeolvasasKeres("SAP-0003", LeltarGyar.GtkKorzet));

        Assert.Equal(HttpStatusCode.OK, sajat.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, idegen.StatusCode);
    }

    [Fact]
    public async Task Korzet_nelkuli_leltarozo_sehol_sem_olvashat_be()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.KorzetNelkuli);
        var id = await NyitottIdoszakAsync();

        var valasz = await BeolvasasKeresAsync(kliens, id, new BeolvasasKeres("SAP-0001", LeltarGyar.MikKorzet));

        Assert.Equal(HttpStatusCode.Forbidden, valasz.StatusCode);
    }

    [Fact]
    public async Task Megtekinto_nem_olvashat_be_403()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Megtekinto);
        var id = await NyitottIdoszakAsync();

        var valasz = await BeolvasasKeresAsync(kliens, id, new BeolvasasKeres("SAP-0001", LeltarGyar.MikKorzet));

        Assert.Equal(HttpStatusCode.Forbidden, valasz.StatusCode);
    }

    [Fact]
    public async Task Sztornozott_leolvasas_nem_szamit_az_ujra_beolvasas_OK()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();
        var elso = await BeolvasAsync(kliens, id, "SAP-0001");

        var sztorno = await kliens.PostAsJsonAsync(ApiUtvonalak.Sztorno(elso.LeolvasasId), new SztornoKeres("Rossz körzetben olvastam be"));
        var ujra = await BeolvasAsync(kliens, id, "SAP-0001");
        var lista = await kliens.GetFromJsonAsync<LapozottLista<LeolvasasListaElem>>(
            $"{ApiUtvonalak.IdoszakLeolvasasai(id)}?sztornozottakkalEgyutt=true");

        Assert.Equal(HttpStatusCode.NoContent, sztorno.StatusCode);
        Assert.Equal(Minosites.Ok, ujra.Minosites);
        Assert.Equal(2, lista!.Osszesen);
        Assert.True(lista.Elemek.Single(l => l.Id == elso.LeolvasasId).Sztornozva);
        Assert.Equal(ujra.LeolvasasId, lista.Elemek[0].Id);
    }

    [Fact]
    public async Task Sztorno_szabalyai()
    {
        var leltarozo = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync();
        var felelosLeolvasasa = await BeolvasAsync(felelos, id, "SAP-0001");

        var indoklasNelkul = await felelos.PostAsJsonAsync(ApiUtvonalak.Sztorno(felelosLeolvasasa.LeolvasasId), new SztornoKeres(" "));
        var masoke = await leltarozo.PostAsJsonAsync(ApiUtvonalak.Sztorno(felelosLeolvasasa.LeolvasasId), new SztornoKeres("Nem az enyém"));
        var rendben = await felelos.PostAsJsonAsync(ApiUtvonalak.Sztorno(felelosLeolvasasa.LeolvasasId), new SztornoKeres("Téves"));
        var ismetelt = await felelos.PostAsJsonAsync(ApiUtvonalak.Sztorno(felelosLeolvasasa.LeolvasasId), new SztornoKeres("Téves"));
        var nemLetezo = await felelos.PostAsJsonAsync(ApiUtvonalak.Sztorno(999_999), new SztornoKeres("Téves"));

        Assert.Equal(HttpStatusCode.BadRequest, indoklasNelkul.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, masoke.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, rendben.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ismetelt.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, nemLetezo.StatusCode);
    }

    [Fact]
    public async Task Leltarozo_csak_a_jogosult_korzetei_leolvasasait_latja()
    {
        var leltarozo = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync();
        await BeolvasAsync(felelos, id, "SAP-0003", LeltarGyar.GtkKorzet);
        await BeolvasAsync(leltarozo, id, "SAP-0001");

        var latott = await leltarozo.GetFromJsonAsync<LapozottLista<LeolvasasListaElem>>(ApiUtvonalak.IdoszakLeolvasasai(id));
        var mind = await felelos.GetFromJsonAsync<LapozottLista<LeolvasasListaElem>>(ApiUtvonalak.IdoszakLeolvasasai(id));

        Assert.Equal("MIK-01", Assert.Single(latott!.Elemek).AktivKorzetKod);
        Assert.Equal(2, mind!.Osszesen);
    }

    [Fact]
    public async Task Ervenyes_demokod_a_korzet_eszkozere_mutat()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var demo = await kliens.GetFromJsonAsync<DemoKod>($"{ApiUtvonalak.Demokod(id)}?leltarkorzetId={LeltarGyar.MikKorzet}");
        var valasz = await BeolvasasKeresAsync(kliens, id, new BeolvasasKeres(demo!.Kod, LeltarGyar.MikKorzet, BeviteliMod.Demo));
        var eredmeny = await valasz.Content.ReadFromJsonAsync<BeolvasasEredmeny>();

        Assert.True(demo.Ervenyes);
        Assert.Equal(Minosites.Ok, eredmeny!.Minosites);
        Assert.Equal(demo.EszkozId, eredmeny.Eszkoz!.Id);
    }

    [Fact]
    public async Task Ervenytelen_demokod_ismeretlen_kodkent_rogzul()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await NyitottIdoszakAsync();

        var demo = await kliens.GetFromJsonAsync<DemoKod>($"{ApiUtvonalak.Demokod(id)}?ervenyes=false");
        var eredmeny = await BeolvasAsync(kliens, id, demo!.Kod);

        Assert.False(demo.Ervenyes);
        Assert.Equal(Minosites.IsmeretlenKod, eredmeny.Minosites);
    }
}
