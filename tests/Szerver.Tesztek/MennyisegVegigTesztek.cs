using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Import;
using LeltarKezelo.Kozos.Leltar;

namespace LeltarKezelo.Szerver.Tesztek;

/// <summary>
/// A mennyiségek végigkövetése a teljes láncon: Excel-import → leltár nyitása (pillanatkép) → beolvasás → összesítés
/// (7. alkalom). A tesztek a valódi HTTP-végpontokat hívják, így az import és a leltárlogika együttműködését ellenőrzik.
/// </summary>
public class MennyisegVegigTesztek(LeltarGyar gyar) : IClassFixture<LeltarGyar>
{
    private static readonly object?[] Fejlec = ["SAP szám", "Megnevezés", "Mennyiség", "Leltárkörzet"];
    private const string Mik = "MIK-01";
    private const string Gtk = "GTK-01";

    [Fact]
    public async Task Excelbol_importalt_keszlet_darabszamai_vegigkovethetok()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-1001", "Előadói székkészlet", 30, Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-1001", mennyiseg: 12);
        var masodik = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-1001", mennyiseg: 15);
        var reszleges = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal((Minosites.Ok, 27, 30), (masodik.Minosites, masodik.BeolvasottMennyiseg, masodik.ElvartMennyiseg));
        Assert.Equal((TetelAllapot.Reszleges, 27, 3, 0), (reszleges.Allapot, reszleges.BeolvasottMennyiseg, reszleges.Hiany, reszleges.Tobblet));

        var harmadik = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-1001", mennyiseg: 5);
        var tobblet = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal(Minosites.Tobblet, harmadik.Minosites);
        Assert.Equal((TetelAllapot.Tobblet, 32, 0, 2), (tobblet.Allapot, tobblet.BeolvasottMennyiseg, tobblet.Hiany, tobblet.Tobblet));

        await SztornoAsync(felelos, harmadik.LeolvasasId);
        var sztorno = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal((TetelAllapot.Reszleges, 27, 3, 0), (sztorno.Allapot, sztorno.BeolvasottMennyiseg, sztorno.Hiany, sztorno.Tobblet));
    }

    [Fact]
    public async Task Ismetelt_import_nyitott_leltarban_nem_irja_at_a_pillanatkepet()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-1002", "Laborasztal-készlet", 30, Mik);
        var nyitott = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        await ImportAsync(felelos, "V-1002", "Laborasztal-készlet", 40, Gtk);
        var beolvasas = await OsszesitesTesztek.BeolvasAsync(felelos, nyitott, "V-1002", mennyiseg: 30);
        var tetel = await TetelAsync(felelos, nyitott, eszkozId);

        Assert.Equal((Minosites.Ok, 30), (beolvasas.Minosites, beolvasas.ElvartMennyiseg));
        Assert.Equal((TetelAllapot.Rendben, 30, Mik), (tetel.Allapot, tetel.ElvartMennyiseg, tetel.ElvartKorzetKod));

        var kovetkezo = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        var ujTetel = await TetelAsync(felelos, kovetkezo, eszkozId);

        Assert.Equal((TetelAllapot.Hianyzik, 40, Gtk), (ujTetel.Allapot, ujTetel.ElvartMennyiseg, ujTetel.ElvartKorzetKod));
    }

    [Fact]
    public async Task Nyitas_utan_importalt_keszlet_nem_elvart_majd_utolag_felveheto()
    {
        var felelos = await FelelosAsync();
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        var eszkozId = await ImportAsync(felelos, "V-1003", "Mérőcsúcs-készlet", 6, Mik);

        var beolvasas = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-1003", mennyiseg: 2);
        var elotte = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal((Minosites.Ok, 6), (beolvasas.Minosites, beolvasas.ElvartMennyiseg));
        Assert.Equal((TetelAllapot.NemElvart, 0, 2), (elotte.Allapot, elotte.ElvartMennyiseg, elotte.BeolvasottMennyiseg));

        var felvetel = await felelos.PostAsJsonAsync(ApiUtvonalak.ElvartTetelek(id), new UtolagosTetelKeres(eszkozId));
        var utana = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal(HttpStatusCode.Created, felvetel.StatusCode);
        Assert.Equal((TetelAllapot.Reszleges, 6, 4), (utana.Allapot, utana.ElvartMennyiseg, utana.Hiany));
    }

    [Fact]
    public async Task Cellaba_irt_egyseg_megjelenik_a_beolvasasban_es_az_osszesitesben()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-1004", "Vetítővászon-szett", "4 szett", Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var beolvasas = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-1004", mennyiseg: 5);
        var tetel = await TetelAsync(felelos, id, eszkozId);

        Assert.Contains("elvárt 4 szett", beolvasas.Uzenet);
        Assert.Equal(("szett", 1), (tetel.MennyisegiEgyseg, tetel.Tobblet));
    }

    // ------------------------------------------------------------------ eltérő körzet és mennyiség együtt

    [Fact]
    public async Task Keszlet_reszben_mas_korzetben_a_darabszam_alapjan_minosul()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-2001", "Asztalkészlet A", 15, Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-2001", mennyiseg: 9);
        var mas = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-2001", LeltarGyar.GtkKorzet, 4);
        var tetel = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal(Minosites.MasKorzet, mas.Minosites);
        Assert.Equal((TetelAllapot.Reszleges, 13, 9, 2), (tetel.Allapot, tetel.BeolvasottMennyiseg, tetel.SajatKorzetben, tetel.Hiany));
        Assert.Equal([Gtk, Mik], tetel.TalaltKorzetek);
    }

    [Fact]
    public async Task Keszlet_teljes_egeszeben_mas_korzetben_nem_hiany_hanem_mas_korzet()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-2002", "Asztalkészlet B", 15, Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-2002", LeltarGyar.GtkKorzet, 15);
        var tetel = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal((TetelAllapot.MasKorzetben, 15, 0, 0), (tetel.Allapot, tetel.BeolvasottMennyiseg, tetel.SajatKorzetben, tetel.Hiany));
    }

    [Fact]
    public async Task Keszlet_a_sajat_es_mas_korzetben_egyutt_tobbet_ad_tobblet()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-2003", "Asztalkészlet C", 10, Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-2003", mennyiseg: 8);
        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-2003", LeltarGyar.GtkKorzet, 5);
        var tetel = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal((TetelAllapot.Tobblet, 13, 8, 3), (tetel.Allapot, tetel.BeolvasottMennyiseg, tetel.SajatKorzetben, tetel.Tobblet));
    }

    [Fact]
    public async Task Egyedi_eszkoz_ket_darabbal_tobblet_es_a_sztorno_visszaallitja()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-3001", "Projektor A", 1, Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var elgepelt = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-3001", mennyiseg: 2);
        var tobblet = await TetelAsync(felelos, id, eszkozId);
        await SztornoAsync(felelos, elgepelt.LeolvasasId);
        var helyes = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-3001");
        var rendben = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal(Minosites.Tobblet, elgepelt.Minosites);
        Assert.Equal((TetelAllapot.Tobblet, 1), (tobblet.Allapot, tobblet.Tobblet));
        Assert.Equal(Minosites.Ok, helyes.Minosites);
        Assert.Equal((TetelAllapot.Rendben, 1, 0), (rendben.Allapot, rendben.BeolvasottMennyiseg, rendben.Tobblet));
    }

    [Fact]
    public async Task Egyedi_eszkoz_mas_korzetben_majd_a_sajatjaban_rendben_es_mindket_korzet_latszik()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-3002", "Projektor B", 1, Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var mas = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-3002", LeltarGyar.GtkKorzet);
        var sajat = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-3002");
        var tetel = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal((Minosites.MasKorzet, Minosites.Ismetelt), (mas.Minosites, sajat.Minosites));
        Assert.Equal((TetelAllapot.Rendben, 1, 1), (tetel.Allapot, tetel.BeolvasottMennyiseg, tetel.SajatKorzetben));
        Assert.Equal([Gtk, Mik], tetel.TalaltKorzetek);
    }

    [Fact]
    public async Task Minden_leolvasas_sztornozasa_utan_ujra_hianyzik()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-3003", "Székkészlet D", 6, Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var elso = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-3003", mennyiseg: 4);
        var masodik = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-3003", LeltarGyar.GtkKorzet, 2);
        await SztornoAsync(felelos, elso.LeolvasasId);
        await SztornoAsync(felelos, masodik.LeolvasasId);
        var tetel = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal((TetelAllapot.Hianyzik, 0, 6), (tetel.Allapot, tetel.BeolvasottMennyiseg, tetel.Hiany));
        Assert.Empty(tetel.TalaltKorzetek);
    }

    // ------------------------------------------------------------------ összesítés egészben

    [Fact]
    public async Task Korzetosszesitesben_a_tetelallapotok_osszege_az_elvart_tetelek_szama()
    {
        var felelos = await FelelosAsync();
        await ImportAsync(felelos, "V-4001", "Székkészlet E", 10, Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-4001", mennyiseg: 3);
        await OsszesitesTesztek.BeolvasAsync(felelos, id, "SAP-0001");
        await OsszesitesTesztek.BeolvasAsync(felelos, id, "SAP-0002", LeltarGyar.GtkKorzet);
        await OsszesitesTesztek.BeolvasAsync(felelos, id, "SAP-0003", LeltarGyar.GtkKorzet, 3);

        var korzetek = (await felelos.GetFromJsonAsync<List<KorzetOsszesites>>(ApiUtvonalak.Osszesites(id)))!;
        var tetelek = await OsszesitesTesztek.TetelekAsync(felelos, id);

        Assert.All(korzetek, k => Assert.Equal(k.ElvartTetelek, k.Rendben + k.Reszleges + k.Hianyzik + k.Tobblet + k.MasKorzetben));
        Assert.Equal(tetelek.Where(t => t.ElvartKorzetId is not null).Sum(t => t.ElvartMennyiseg), korzetek.Sum(k => k.ElvartMennyiseg));
        Assert.Equal(tetelek.Where(t => t.ElvartKorzetId is not null).Sum(t => t.BeolvasottMennyiseg), korzetek.Sum(k => k.BeolvasottMennyiseg));
        Assert.All(tetelek, t => Assert.Equal(t.ElvartMennyiseg - t.BeolvasottMennyiseg, t.Hiany - t.Tobblet));
    }

    [Fact]
    public async Task Lezart_leltar_osszesitese_lekerdezheto_es_nem_valtozik()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-4002", "Székkészlet F", 10, Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-4002", mennyiseg: 7);
        (await felelos.PostAsync(ApiUtvonalak.Lezaras(id), null)).EnsureSuccessStatusCode();

        await ImportAsync(felelos, "V-4002", "Székkészlet F", 12, Mik);
        var kesei = await felelos.PostAsJsonAsync(ApiUtvonalak.IdoszakLeolvasasai(id), new BeolvasasKeres("V-4002", LeltarGyar.MikKorzet));
        var tetel = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal(HttpStatusCode.Conflict, kesei.StatusCode);
        Assert.Equal((TetelAllapot.Reszleges, 10, 7, 3), (tetel.Allapot, tetel.ElvartMennyiseg, tetel.BeolvasottMennyiseg, tetel.Hiany));
    }

    [Fact]
    public async Task Megtekinto_csak_a_sajat_egysege_korzeteit_latja_az_osszesitesben()
    {
        var felelos = await FelelosAsync();
        var megtekinto = await gyar.BejelentkezettKliensAsync(LeltarGyar.Megtekinto);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var korzetek = await megtekinto.GetFromJsonAsync<List<KorzetOsszesites>>(ApiUtvonalak.Osszesites(id));
        var tetelek = await OsszesitesTesztek.TetelekAsync(megtekinto, id);
        var beolvasas = await megtekinto.PostAsJsonAsync(ApiUtvonalak.IdoszakLeolvasasai(id), new BeolvasasKeres("SAP-0003", LeltarGyar.GtkKorzet));

        Assert.Equal([LeltarGyar.GtkKorzet], korzetek!.Select(k => k.LeltarkorzetId));
        Assert.All(tetelek, t => Assert.Equal(LeltarGyar.GtkKorzet, t.ElvartKorzetId));
        Assert.Equal(HttpStatusCode.Forbidden, beolvasas.StatusCode);
    }

    // ------------------------------------------------------------------ a kódátnézéskor talált, javított hibák (D-041)

    [Fact]
    public async Task Egyedi_eszkoz_ismetelt_leolvasasanak_mennyisege_nem_novelheti_a_darabszamot()
    {
        var felelos = await FelelosAsync();
        var eszkozId = await ImportAsync(felelos, "V-5001", "Projektor C", 1, Mik);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-5001");
        var ismetelt = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-5001", mennyiseg: 5);
        var tetel = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal((Minosites.Ismetelt, 1), (ismetelt.Minosites, ismetelt.BeolvasottMennyiseg));
        Assert.Equal((TetelAllapot.Rendben, 1, 0), (tetel.Allapot, tetel.BeolvasottMennyiseg, tetel.Tobblet));
    }

    [Fact]
    public async Task Nem_elvart_egyedi_eszkoz_sztorno_utan_megmaradt_ismetlese_is_megtalalja()
    {
        var felelos = await FelelosAsync();
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        var eszkozId = await ImportAsync(felelos, "V-5002", "Projektor D", 1, Mik);

        var elso = await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-5002");
        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-5002");
        await SztornoAsync(felelos, elso.LeolvasasId);
        var tetel = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal((TetelAllapot.NemElvart, 1), (tetel.Allapot, tetel.BeolvasottMennyiseg));
    }

    [Fact]
    public async Task Nem_elvart_mennyiseges_eszkoz_leolvasasai_osszeadodnak()
    {
        var felelos = await FelelosAsync();
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        var eszkozId = await ImportAsync(felelos, "V-5003", "Hosszabbító-készlet", 6, Mik);

        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-5003", mennyiseg: 2);
        await OsszesitesTesztek.BeolvasAsync(felelos, id, "V-5003", mennyiseg: 3);
        var tetel = await TetelAsync(felelos, id, eszkozId);

        Assert.Equal((TetelAllapot.NemElvart, 0, 5), (tetel.Allapot, tetel.ElvartMennyiseg, tetel.BeolvasottMennyiseg));
    }

    // ------------------------------------------------------------------ segédek

    private Task<HttpClient> FelelosAsync() => gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

    /// <summary>Egysoros import a valódi végponton; az eszköz azonosítójával tér vissza.</summary>
    private static async Task<long> ImportAsync(HttpClient kliens, string sap, string megnevezes, object mennyiseg, string korzet)
    {
        var fajl = ImportTesztek.Fajl(Fejlec, [sap, megnevezes, mennyiseg, korzet]).ToArray();
        var tartalom = new ByteArrayContent(fajl);
        tartalom.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        using var urlap = new MultipartFormDataContent { { tartalom, ImportUrlap.Fajl, "mennyisegek.xlsx" } };

        var valasz = await kliens.PostAsync(ApiUtvonalak.Importok, urlap);

        Assert.Equal(HttpStatusCode.Created, valasz.StatusCode);
        var eredmeny = (await valasz.Content.ReadFromJsonAsync<ImportEredmeny>())!;
        if (eredmeny.Elemzes.Sorok[0].EszkozId is { } meglevo)
            return meglevo;

        var talalat = await kliens.GetFromJsonAsync<LapozottLista<LeltarKezelo.Kozos.Eszkozok.EszkozListaElem>>(
            $"{ApiUtvonalak.Eszkozok}?kereses={sap}");
        return talalat!.Elemek.Single().Id;
    }

    private static async Task<OsszesitesTetel> TetelAsync(HttpClient kliens, long idoszakId, long eszkozId) =>
        (await OsszesitesTesztek.TetelekAsync(kliens, idoszakId)).Single(t => t.EszkozId == eszkozId);

    private static async Task SztornoAsync(HttpClient kliens, long leolvasasId) =>
        (await kliens.PostAsJsonAsync(ApiUtvonalak.Sztorno(leolvasasId), new SztornoKeres("Teszt: téves beolvasás"))).EnsureSuccessStatusCode();
}
