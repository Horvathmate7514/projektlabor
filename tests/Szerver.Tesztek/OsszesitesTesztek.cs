using System.Net;
using System.Net.Http.Json;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.Extensions.DependencyInjection;

namespace LeltarKezelo.Szerver.Tesztek;

public class OsszesitesTesztek(LeltarGyar gyar) : IClassFixture<LeltarGyar>
{
    public sealed class UtolagosGyar : LeltarGyar;

    internal const long Szamitogep = 1;
    private const long Monitor = 2;
    private const long Szek = 3;
    private const long Targyaloszekek = 4;

    internal static async Task<long> NyitottIdoszakAsync(LeltarGyar gyar)
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var letrehozas = await felelos.PostAsJsonAsync(ApiUtvonalak.LeltarIdoszakok,
            new LeltarIdoszakLetrehozas("Összesítés teszt", LeltarIdoszakTipus.Rendkivuli, new DateOnly(2026, 11, 1)));
        var idoszak = await letrehozas.Content.ReadFromJsonAsync<LeltarIdoszakAdat>();
        (await felelos.PostAsync(ApiUtvonalak.Nyitas(idoszak!.Id), null)).EnsureSuccessStatusCode();
        return idoszak.Id;
    }

    internal static async Task<BeolvasasEredmeny> BeolvasAsync(
        HttpClient kliens, long idoszakId, string kod, long korzetId = LeltarGyar.MikKorzet, int mennyiseg = 1)
    {
        var valasz = await kliens.PostAsJsonAsync(ApiUtvonalak.IdoszakLeolvasasai(idoszakId),
            new BeolvasasKeres(kod, korzetId, Mennyiseg: mennyiseg));
        valasz.EnsureSuccessStatusCode();
        return (await valasz.Content.ReadFromJsonAsync<BeolvasasEredmeny>())!;
    }

    internal static async Task<List<OsszesitesTetel>> TetelekAsync(HttpClient kliens, long idoszakId, string szuro = "")
    {
        var lista = await kliens.GetFromJsonAsync<LapozottLista<OsszesitesTetel>>(
            $"{ApiUtvonalak.Tetelek(idoszakId)}?oldalMeret=200{szuro}");
        return lista!.Elemek.ToList();
    }

    private static OsszesitesSzolgaltatas.Talalat T(long korzet, int mennyiseg, Minosites m = Minosites.Ok) => new(korzet, mennyiseg, m);

    [Fact]
    public void Egyedi_eszkoz_ket_OK_leolvasasa_is_egy_darab()
    {
        var (beolvasott, sajat) = OsszesitesSzolgaltatas.Szamol(1, 1, [T(1, 1), T(1, 1)]);

        Assert.Equal(1, beolvasott);
        Assert.Equal(1, sajat);
    }

    [Fact]
    public void Sztorno_utan_megmaradt_ismetles_is_megtalalja_az_egyedi_eszkozt()
    {
        var (beolvasott, sajat) = OsszesitesSzolgaltatas.Szamol(1, 1, [T(1, 1, Minosites.Ismetelt)]);

        Assert.Equal(1, beolvasott);
        Assert.Equal(1, sajat);
    }

    [Fact]
    public void Mennyiseges_eszkoz_osszeadja_a_korzetenkenti_darabokat()
    {
        var (beolvasott, sajat) = OsszesitesSzolgaltatas.Szamol(30, 1, [T(1, 20), T(2, 5, Minosites.MasKorzet), T(1, 3)]);

        Assert.Equal(28, beolvasott);
        Assert.Equal(23, sajat);
    }

    [Theory]
    [InlineData(true, 1, 0, 0, TetelAllapot.Hianyzik)]
    [InlineData(true, 1, 1, 0, TetelAllapot.MasKorzetben)]
    [InlineData(true, 1, 1, 1, TetelAllapot.Rendben)]
    [InlineData(true, 30, 12, 12, TetelAllapot.Reszleges)]
    [InlineData(true, 30, 31, 31, TetelAllapot.Tobblet)]
    [InlineData(true, 30, 31, 1, TetelAllapot.Tobblet)]
    [InlineData(false, 0, 1, 0, TetelAllapot.NemElvart)]
    public void Tetel_allapota(bool elvartTetel, int elvart, int beolvasott, int sajat, TetelAllapot vart) =>
        Assert.Equal(vart, OsszesitesSzolgaltatas.Allapot(elvartTetel, elvart, beolvasott, sajat));

    [Fact]
    public async Task Frissen_nyitott_idoszakban_minden_elvart_tetel_hianyzik()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync(gyar);

        var tetelek = await TetelekAsync(felelos, id);

        Assert.Equal(4, tetelek.Count);
        Assert.All(tetelek, t => Assert.Equal(TetelAllapot.Hianyzik, t.Allapot));
        Assert.Equal(3, tetelek.Single(t => t.EszkozId == Targyaloszekek).Hiany);
    }

    [Fact]
    public async Task Darabszamok_hiany_tobblet_es_mas_korzet()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync(gyar);
        await BeolvasAsync(felelos, id, "SAP-0001");
        await BeolvasAsync(felelos, id, "SAP-0001");
        await BeolvasAsync(felelos, id, "SAP-0002", LeltarGyar.GtkKorzet);
        await BeolvasAsync(felelos, id, "SZEK-30", mennyiseg: 2);
        await BeolvasAsync(felelos, id, "SZEK-30", mennyiseg: 2);

        var tetelek = (await OsszesitesTesztek.TetelekAsync(felelos, id)).ToDictionary(t => t.EszkozId);

        Assert.Equal(TetelAllapot.Rendben, tetelek[Szamitogep].Allapot);
        Assert.Equal(1, tetelek[Szamitogep].BeolvasottMennyiseg);
        Assert.Equal(TetelAllapot.MasKorzetben, tetelek[Monitor].Allapot);
        Assert.Equal(["GTK-01"], tetelek[Monitor].TalaltKorzetek);
        Assert.Equal(TetelAllapot.Hianyzik, tetelek[Szek].Allapot);
        Assert.Equal(TetelAllapot.Tobblet, tetelek[Targyaloszekek].Allapot);
        Assert.Equal(4, tetelek[Targyaloszekek].BeolvasottMennyiseg);
        Assert.Equal(1, tetelek[Targyaloszekek].Tobblet);
    }

    [Fact]
    public async Task Reszleges_mennyiseg_es_sztorno_csokkenti_a_darabszamot()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync(gyar);
        await BeolvasAsync(felelos, id, "SZEK-30", mennyiseg: 2);
        var teves = await BeolvasAsync(felelos, id, "SZEK-30");
        (await felelos.PostAsJsonAsync(ApiUtvonalak.Sztorno(teves.LeolvasasId), new SztornoKeres("Kétszer számoltam"))).EnsureSuccessStatusCode();

        var szekek = (await OsszesitesTesztek.TetelekAsync(felelos, id)).Single(t => t.EszkozId == Targyaloszekek);

        Assert.Equal(TetelAllapot.Reszleges, szekek.Allapot);
        Assert.Equal(2, szekek.BeolvasottMennyiseg);
        Assert.Equal(1, szekek.Hiany);
    }

    [Fact]
    public async Task Sztornozott_elso_leolvasas_utan_az_ismetles_szamit()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync(gyar);
        var elso = await BeolvasAsync(felelos, id, "SAP-0001");
        var masodik = await BeolvasAsync(felelos, id, "SAP-0001");
        (await felelos.PostAsJsonAsync(ApiUtvonalak.Sztorno(elso.LeolvasasId), new SztornoKeres("Téves"))).EnsureSuccessStatusCode();

        var gep = (await OsszesitesTesztek.TetelekAsync(felelos, id)).Single(t => t.EszkozId == Szamitogep);

        Assert.Equal(Minosites.Ismetelt, masodik.Minosites);
        Assert.Equal(TetelAllapot.Rendben, gep.Allapot);
    }

    [Fact]
    public async Task Korzetosszesites_jelzi_a_mas_korzetbol_ide_kerult_eszkozt_es_az_ismeretlen_kodot()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync(gyar);
        await BeolvasAsync(felelos, id, "SAP-0002", LeltarGyar.GtkKorzet);
        await BeolvasAsync(felelos, id, "NINCS-ILYEN", LeltarGyar.GtkKorzet);
        await BeolvasAsync(felelos, id, "SAP-0003", LeltarGyar.GtkKorzet);

        var korzetek = await felelos.GetFromJsonAsync<List<KorzetOsszesites>>(ApiUtvonalak.Osszesites(id));
        var mik = korzetek!.Single(k => k.LeltarkorzetId == LeltarGyar.MikKorzet);
        var gtk = korzetek!.Single(k => k.LeltarkorzetId == LeltarGyar.GtkKorzet);

        Assert.Equal(3, mik.ElvartTetelek);
        Assert.Equal(1, mik.MasKorzetben);
        Assert.Equal(2, mik.Hianyzik);
        Assert.Equal(1, gtk.ElvartTetelek);
        Assert.Equal(1, gtk.Rendben);
        Assert.Equal(1, gtk.MasKorzetbolIdeKerult);
        Assert.Equal(1, gtk.IsmeretlenKodok);
        Assert.Equal(1.0, gtk.Keszultseg);
    }

    [Fact]
    public async Task Tetelek_szurhetok_allapotra_korzetre_es_elteresre()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync(gyar);
        await BeolvasAsync(felelos, id, "SAP-0001");
        await BeolvasAsync(felelos, id, "SAP-0002", LeltarGyar.GtkKorzet);

        var hianyzok = await TetelekAsync(felelos, id, "&allapot=Hianyzik");
        var gtk = await TetelekAsync(felelos, id, $"&leltarkorzetId={LeltarGyar.GtkKorzet}");
        var elteresek = await TetelekAsync(felelos, id, "&csakElteresek=true");

        Assert.Equal([Szek, Targyaloszekek], hianyzok.Select(t => t.EszkozId).Order());
        Assert.Equal([Monitor, Szek], gtk.Select(t => t.EszkozId).Order());
        Assert.DoesNotContain(elteresek, t => t.EszkozId == Szamitogep);
    }

    [Fact]
    public async Task Leltarozo_csak_a_sajat_korzetet_es_az_oda_kerult_eszkozoket_latja()
    {
        var leltarozo = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await NyitottIdoszakAsync(gyar);
        await BeolvasAsync(felelos, id, "SAP-0003");

        var korzetek = await leltarozo.GetFromJsonAsync<List<KorzetOsszesites>>(ApiUtvonalak.Osszesites(id));
        var tetelek = await TetelekAsync(leltarozo, id);

        Assert.Equal([LeltarGyar.MikKorzet], korzetek!.Select(k => k.LeltarkorzetId));
        Assert.Equal(1, korzetek![0].MasKorzetbolIdeKerult);
        Assert.Contains(tetelek, t => t.EszkozId == Szek && t.Allapot == TetelAllapot.MasKorzetben);
    }

    [Fact]
    public async Task Nem_letezo_idoszak_404()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

        var valasz = await kliens.GetAsync(ApiUtvonalak.Osszesites(999_999));

        Assert.Equal(HttpStatusCode.NotFound, valasz.StatusCode);
    }
}

public class UtolagosFelvetelTesztek(OsszesitesTesztek.UtolagosGyar gyar) : IClassFixture<OsszesitesTesztek.UtolagosGyar>
{
    private const long Szamitogep = OsszesitesTesztek.Szamitogep;

    [Fact]
    public async Task Utolag_felvett_eszkoz_bekerul_az_elvart_tetelek_koze()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        var ujId = UjEszkoz("UJ-0001");
        await OsszesitesTesztek.BeolvasAsync(felelos, id, "UJ-0001");

        var elotte = (await OsszesitesTesztek.TetelekAsync(felelos, id)).Single(t => t.EszkozId == ujId);
        var felvetel = await felelos.PostAsJsonAsync(ApiUtvonalak.ElvartTetelek(id), new UtolagosTetelKeres(ujId));
        var ujra = await felelos.PostAsJsonAsync(ApiUtvonalak.ElvartTetelek(id), new UtolagosTetelKeres(ujId));
        var utana = (await OsszesitesTesztek.TetelekAsync(felelos, id)).Single(t => t.EszkozId == ujId);

        Assert.Equal(TetelAllapot.NemElvart, elotte.Allapot);
        Assert.Equal(HttpStatusCode.Created, felvetel.StatusCode);
        Assert.Equal(ElvartTetelFelvitelOka.Utolagos, (await felvetel.Content.ReadFromJsonAsync<ElvartTetelAdat>())!.FelvitelOka);
        Assert.Equal(HttpStatusCode.Conflict, ujra.StatusCode);
        Assert.Equal(TetelAllapot.Rendben, utana.Allapot);
    }

    [Fact]
    public async Task Utolagos_felvetel_hibas_esetei()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var leltarozo = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var selejtezett = await felelos.PostAsJsonAsync(ApiUtvonalak.ElvartTetelek(id), new UtolagosTetelKeres(5));
        var nemLetezo = await felelos.PostAsJsonAsync(ApiUtvonalak.ElvartTetelek(id), new UtolagosTetelKeres(999_999));
        var tiltott = await leltarozo.PostAsJsonAsync(ApiUtvonalak.ElvartTetelek(id), new UtolagosTetelKeres(Szamitogep));
        (await felelos.PostAsync(ApiUtvonalak.Lezaras(id), null)).EnsureSuccessStatusCode();
        var lezart = await felelos.PostAsJsonAsync(ApiUtvonalak.ElvartTetelek(id), new UtolagosTetelKeres(Szamitogep));

        Assert.Equal(HttpStatusCode.BadRequest, selejtezett.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, nemLetezo.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, tiltott.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, lezart.StatusCode);
    }

    private long UjEszkoz(string kod)
    {
        using var scope = gyar.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeltarDbContext>();
        var eszkoz = new Eszkoz
        {
            Megnevezes = "Utólag felvett projektor",
            LeltarkorzetId = LeltarGyar.MikKorzet,
            EszkozAllapotId = 1,
            Kodok = [new EszkozKod { KodTipusId = 1, Ertek = kod, ErtekNorm = kod }],
        };
        db.Eszkozok.Add(eszkoz);
        db.SaveChanges();
        return eszkoz.Id;
    }
}
