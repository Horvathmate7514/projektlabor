using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Import;
using LeltarKezelo.Kozos.Leltar;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Tesztek;

public class ImportVegpontTesztek(LeltarGyar gyar) : IClassFixture<LeltarGyar>
{
    private static readonly object?[] Fejlec = ["SAP szám", "Leltári szám 1", "Megnevezés", "Leltárkörzet"];

    [Fact]
    public async Task Probafuttatas_elemzest_ad_es_nem_ir_az_adatbazisba()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var elotte = await NaploAsync(kliens);

        var valasz = await kliens.PostAsync(ApiUtvonalak.ImportProbafuttatas, Urlap(Xlsx(
            Fejlec,
            ["51000001", "L-5100-01", "Monitor", "MIK-01"],
            ["51000002", null, "Szék", "XYZ-9"])));

        Assert.Equal(HttpStatusCode.OK, valasz.StatusCode);
        var elemzes = await valasz.Content.ReadFromJsonAsync<ImportElemzes>();
        Assert.Equal(1, elemzes!.Osszesites.Uj);
        Assert.Equal(1, elemzes.Osszesites.Hibas);
        Assert.Equal(ImportSorAllapot.Hibas, elemzes.Sorok.Single(s => s.SapSzam == "51000002").Allapot);
        Assert.Equal(elotte.Osszesen, (await NaploAsync(kliens)).Osszesen);
    }

    [Fact]
    public async Task Vegrehajtas_201_es_a_futas_a_naploban_hibakkal_lekerdezheto()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

        var valasz = await kliens.PostAsync(ApiUtvonalak.Importok, Urlap(Xlsx(
            Fejlec,
            ["52000001", "L-5200-01", "Projektor", "MIK-01"],
            ["52000002", null, "Tábla", "XYZ-9"]), "naplo_proba.xlsx"));

        Assert.Equal(HttpStatusCode.Created, valasz.StatusCode);
        var eredmeny = await valasz.Content.ReadFromJsonAsync<ImportEredmeny>();
        Assert.Equal(ImportStatusz.Reszleges, eredmeny!.Statusz);
        Assert.Equal(1, eredmeny.Uj);
        Assert.EndsWith(ApiUtvonalak.Import(eredmeny.ImportFutasId), valasz.Headers.Location!.ToString());

        var futas = await kliens.GetFromJsonAsync<ImportFutasReszletek>(ApiUtvonalak.Import(eredmeny.ImportFutasId));
        Assert.Equal("naplo_proba.xlsx", futas!.Fajlnev);
        Assert.Equal("Leltár Felelős", futas.Felhasznalo);
        var hiba = Assert.Single(futas.Hibak);
        Assert.Equal(3, hiba.Sor);
        Assert.Equal("Leltárkörzet", hiba.Oszlop);
        Assert.Equal("XYZ-9", hiba.Ertek);

        var lista = await NaploAsync(kliens, $"?statusz={ImportStatusz.Reszleges}");
        Assert.Contains(lista.Elemek, e => e.Id == eredmeny.ImportFutasId);
        Assert.All(lista.Elemek, e => Assert.Equal(ImportStatusz.Reszleges, e.Statusz));

        var eszkozok = await kliens.GetFromJsonAsync<LapozottLista<Kozos.Eszkozok.EszkozListaElem>>($"{ApiUtvonalak.Eszkozok}?kereses=52000001");
        Assert.Single(eszkozok!.Elemek);
    }

    [Fact]
    public async Task Hibak_excelben_letolthetok()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var eredmeny = await VegrehajtasAsync(kliens, Xlsx(Fejlec, ["53000001", null, "Polc", "NINCS-ILYEN"]));

        var valasz = await kliens.GetAsync(ApiUtvonalak.ImportHibakExcel(eredmeny.ImportFutasId));

        Assert.Equal(HttpStatusCode.OK, valasz.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", valasz.Content.Headers.ContentType?.MediaType);
        Assert.Equal($"importhibak_{eredmeny.ImportFutasId}.xlsx", valasz.Content.Headers.ContentDisposition?.FileNameStar);
        using var munkafuzet = new XLWorkbook(await valasz.Content.ReadAsStreamAsync());
        var lap = munkafuzet.Worksheet(1);
        var sor = lap.RowsUsed().Single(r => r.Cell(1).GetFormattedString() == "2");
        Assert.Equal("Leltárkörzet", sor.Cell(2).GetString());
        Assert.Contains("NINCS-ILYEN", sor.Cell(4).GetString());
    }

    [Fact]
    public async Task Kikapcsolt_frissitesnel_a_meglevo_eszkoz_kimarad()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

        var eredmeny = await VegrehajtasAsync(kliens, Xlsx(Fejlec, ["SAP-0003", null, "Irodai szék, új név", "GTK-01"]),
            (ImportUrlap.Frissitesek, "false"));

        Assert.Equal(1, eredmeny.Kihagyott);
        Assert.Equal(0, eredmeny.Frissitett);
        var szek = await kliens.GetFromJsonAsync<Kozos.Eszkozok.EszkozReszletek>($"{ApiUtvonalak.Eszkozok}/3");
        Assert.Equal("Irodai szék", szek!.Megnevezes);
    }

    [Fact]
    public async Task Kezi_oszlop_hozzarendeles_es_munkalap_valasztas()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var fajl = Xlsx(["SAP szám", "Tétel", "Leltárkörzet"], ["54000001", "Állólámpa", "MIK-01"]);

        var valasz = await kliens.PostAsync(ApiUtvonalak.ImportProbafuttatas, Urlap(fajl, mezok:
        [
            (ImportUrlap.Oszlopok, """{"megnevezes": "tétel"}"""),
            (ImportUrlap.Munkalap, "Adatok"),
        ]));

        Assert.Equal(HttpStatusCode.OK, valasz.StatusCode);
        var elemzes = await valasz.Content.ReadFromJsonAsync<ImportElemzes>();
        Assert.Equal("Állólámpa", Assert.Single(elemzes!.Sorok).Megnevezes);
    }

    [Theory]
    [InlineData("nem json", "nem érvényes JSON")]
    [InlineData("""{"Szin": "Szín"}""", "Ismeretlen importmező: „Szin”")]
    [InlineData("""{"3": "Megnevezés"}""", "Ismeretlen importmező: „3”")]
    [InlineData("""{"Megnevezes": "Nincs ilyen oszlop"}""", "„Nincs ilyen oszlop” oszlop nem található")]
    public async Task Hibas_oszlop_hozzarendeles_400(string oszlopok, string varhato)
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

        var valasz = await kliens.PostAsync(ApiUtvonalak.ImportProbafuttatas,
            Urlap(Xlsx(Fejlec, ["55000001", null, "Monitor", "MIK-01"]), mezok: [(ImportUrlap.Oszlopok, oszlopok)]));

        Assert.Equal(HttpStatusCode.BadRequest, valasz.StatusCode);
        Assert.Contains(varhato, await HibauzenetAsync(valasz));
    }

    [Fact]
    public async Task Fajl_nelkul_400()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var urlap = new MultipartFormDataContent { { new StringContent("false"), ImportUrlap.Frissitesek } };

        var valasz = await kliens.PostAsync(ApiUtvonalak.Importok, urlap);

        Assert.Equal(HttpStatusCode.BadRequest, valasz.StatusCode);
        Assert.Contains("Nincs feltöltött fájl", await HibauzenetAsync(valasz));
    }

    [Fact]
    public async Task Nem_xlsx_fajl_ertheto_400()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

        var valasz = await kliens.PostAsync(ApiUtvonalak.ImportProbafuttatas, Urlap("ez nem Excel"u8.ToArray(), "adatok.csv"));

        Assert.Equal(HttpStatusCode.BadRequest, valasz.StatusCode);
        Assert.Contains("nem érvényes .xlsx", await HibauzenetAsync(valasz));
    }

    [Theory]
    [InlineData(LeltarGyar.Leltarozo)]
    [InlineData(LeltarGyar.Megtekinto)]
    public async Task Leltarozo_es_megtekinto_nem_importalhat(string felhasznalo)
    {
        var kliens = await gyar.BejelentkezettKliensAsync(felhasznalo);

        var probafuttatas = await kliens.PostAsync(ApiUtvonalak.ImportProbafuttatas, Urlap(Xlsx(Fejlec, ["56000001", null, "Monitor", "MIK-01"])));
        var naplo = await kliens.GetAsync(ApiUtvonalak.Importok);

        Assert.Equal(HttpStatusCode.Forbidden, probafuttatas.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, naplo.StatusCode);
    }

    [Fact]
    public async Task Bejelentkezes_nelkul_401()
    {
        var valasz = await gyar.CreateClient().PostAsync(ApiUtvonalak.Importok, Urlap(Xlsx(Fejlec)));

        Assert.Equal(HttpStatusCode.Unauthorized, valasz.StatusCode);
    }

    [Fact]
    public async Task Nem_letezo_futas_404()
    {
        var kliens = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

        Assert.Equal(HttpStatusCode.NotFound, (await kliens.GetAsync(ApiUtvonalak.Import(999_999))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await kliens.GetAsync(ApiUtvonalak.ImportHibakExcel(999_999))).StatusCode);
    }

    // ------------------------------------------------------------------ segédek

    private static byte[] Xlsx(params object?[][] sorok) => ImportTesztek.Fajl(sorok).ToArray();

    private static MultipartFormDataContent Urlap(byte[] fajl, string fajlnev = "forras.xlsx", params (string Nev, string Ertek)[] mezok)
    {
        var tartalom = new ByteArrayContent(fajl);
        tartalom.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var urlap = new MultipartFormDataContent { { tartalom, ImportUrlap.Fajl, fajlnev } };
        foreach (var (nev, ertek) in mezok)
            urlap.Add(new StringContent(ertek), nev);
        return urlap;
    }

    private static async Task<ImportEredmeny> VegrehajtasAsync(HttpClient kliens, byte[] fajl, params (string Nev, string Ertek)[] mezok)
    {
        var valasz = await kliens.PostAsync(ApiUtvonalak.Importok, Urlap(fajl, mezok: mezok));
        Assert.Equal(HttpStatusCode.Created, valasz.StatusCode);
        return (await valasz.Content.ReadFromJsonAsync<ImportEredmeny>())!;
    }

    private static async Task<string> HibauzenetAsync(HttpResponseMessage valasz)
    {
        Assert.Equal("application/problem+json", valasz.Content.Headers.ContentType?.MediaType);
        return (await valasz.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail ?? "";
    }

    private static async Task<LapozottLista<ImportFutasListaElem>> NaploAsync(HttpClient kliens, string lekerdezes = "") =>
        (await kliens.GetFromJsonAsync<LapozottLista<ImportFutasListaElem>>(ApiUtvonalak.Importok + lekerdezes))!;
}
