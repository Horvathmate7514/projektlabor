using System.Net;
using System.Net.Http.Json;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Kozos.Torzsadatok;

namespace LeltarKezelo.Szerver.Tesztek;

/// <summary>Helyiségek, eszköztípusok és felelős személyek kezelése (8. alkalom, UC-04, D-043).</summary>
public class TorzsadatKezelesTesztek(TorzsadatKezelesTesztek.Gyar gyar) : IClassFixture<TorzsadatKezelesTesztek.Gyar>
{
    public sealed class Gyar : LeltarGyar;

    private Task<HttpClient> FelelosAsync() => gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

    [Fact]
    public async Task Uj_helyiseg_felveheto_es_megjelenik_a_listaban()
    {
        var felelos = await FelelosAsync();

        var valasz = await felelos.PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes(" B ", "204", "2. emelet", "Tárgyaló"));
        var uj = await valasz.Content.ReadFromJsonAsync<HelyisegKezelesAdat>();
        var lista = await felelos.GetFromJsonAsync<List<HelyisegAdat>>(ApiUtvonalak.Helyisegek);

        Assert.Equal(HttpStatusCode.Created, valasz.StatusCode);
        Assert.Equal(("B", "204", "2. emelet", "Tárgyaló", true, 0), (uj!.Epulet, uj.Szobaszam, uj.Emelet, uj.Megnevezes, uj.Aktiv, uj.Eszkozok));
        Assert.Contains(lista!, h => h.Id == uj.Id && h.Megnevezes == "Tárgyaló");
    }

    [Theory]
    [InlineData("I", "101")]
    [InlineData("i", " 101 ")]
    public async Task Azonos_epulet_es_szobaszam_utkozik(string epulet, string szobaszam)
    {
        var felelos = await FelelosAsync();

        var valasz = await felelos.PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes(epulet, szobaszam));

        Assert.Equal(HttpStatusCode.Conflict, valasz.StatusCode);
    }

    [Theory]
    [InlineData("", "1")]
    [InlineData("A", "  ")]
    [InlineData("A", "123456789012345678901")]
    public async Task Hianyos_vagy_tul_hosszu_helyiseg_hibas(string epulet, string szobaszam)
    {
        var felelos = await FelelosAsync();

        var valasz = await felelos.PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes(epulet, szobaszam));

        Assert.Equal(HttpStatusCode.BadRequest, valasz.StatusCode);
    }

    [Fact]
    public async Task Helyiseg_vonalkodja_egyedi_es_nem_lehet_eszkoz_kodja()
    {
        var felelos = await FelelosAsync();

        var eszkozKod = await felelos.PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes("V", "1", Vonalkod: "sap-0001"));
        var elso = await felelos.PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes("V", "2", Vonalkod: "H-V-2"));
        var masodik = await felelos.PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes("V", "3", Vonalkod: "h-v-2"));

        Assert.Equal(HttpStatusCode.Conflict, eszkozKod.StatusCode);
        Assert.Equal(HttpStatusCode.Created, elso.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, masodik.StatusCode);
    }

    [Fact]
    public async Task Helyiseg_csak_akkor_inaktivalhato_ha_nincs_benne_eszkoz()
    {
        var felelos = await FelelosAsync();
        var ures = await (await felelos.PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes("R", "1")))
            .Content.ReadFromJsonAsync<HelyisegKezelesAdat>();

        var foglalt = await felelos.PutAsJsonAsync(ApiUtvonalak.Helyiseg(LeltarGyar.Terem101), new HelyisegMentes("I", "101", Aktiv: false));
        var inaktivalas = await felelos.PutAsJsonAsync(ApiUtvonalak.Helyiseg(ures!.Id), new HelyisegMentes("R", "1", Megnevezes: "Raktár", Aktiv: false));
        var nincsIlyen = await felelos.PutAsJsonAsync(ApiUtvonalak.Helyiseg(9999), new HelyisegMentes("R", "9"));
        var aktivak = await felelos.GetFromJsonAsync<List<HelyisegAdat>>(ApiUtvonalak.Helyisegek);
        var mind = await felelos.GetFromJsonAsync<List<HelyisegKezelesAdat>>(ApiUtvonalak.HelyisegKezeles);

        Assert.Equal(HttpStatusCode.Conflict, foglalt.StatusCode);
        Assert.Equal(HttpStatusCode.OK, inaktivalas.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, nincsIlyen.StatusCode);
        Assert.DoesNotContain(aktivak!, h => h.Id == ures.Id);
        Assert.Equal((false, "Raktár"), mind!.Where(h => h.Id == ures.Id).Select(h => (h.Aktiv, h.Megnevezes)).Single());
        Assert.Equal(1, mind!.Single(h => h.Id == LeltarGyar.Terem101).Eszkozok);
    }

    [Theory]
    [InlineData(LeltarGyar.Leltarozo)]
    [InlineData(LeltarGyar.Megtekinto)]
    public async Task Torzsadatot_csak_a_leltarfelelos_es_az_admin_kezelhet(string felhasznalonev)
    {
        var kliens = await gyar.BejelentkezettKliensAsync(felhasznalonev);

        var helyiseg = await kliens.PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes("X", "1"));
        var tipus = await kliens.PutAsJsonAsync(ApiUtvonalak.EszkozTipus(LeltarGyar.MonitorTipus), new EszkozTipusMentes("Kijelző"));
        var felelosok = await kliens.GetAsync(ApiUtvonalak.FelelosKezeles);
        var nevtelen = await gyar.CreateClient().PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes("X", "1"));

        Assert.Equal(HttpStatusCode.Forbidden, helyiseg.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, tipus.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, felelosok.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, nevtelen.StatusCode);
    }

    [Fact]
    public async Task Uj_eszkoztipus_altipuskent_es_a_nev_irasmodtol_fuggetlenul_egyedi()
    {
        var felelos = await FelelosAsync();

        var valasz = await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozTipusok, new EszkozTipusMentes("Laptop", LeltarGyar.SzamitastechnikaTipus));
        var uj = await valasz.Content.ReadFromJsonAsync<EszkozTipusKezelesAdat>();
        var kisbetus = await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozTipusok, new EszkozTipusMentes(" laptop "));
        var ekezetNelkul = await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozTipusok, new EszkozTipusMentes("Szamitastechnika"));
        var lista = await felelos.GetFromJsonAsync<List<EszkozTipusAdat>>(ApiUtvonalak.EszkozTipusok);

        Assert.Equal(HttpStatusCode.Created, valasz.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, kisbetus.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ekezetNelkul.StatusCode);
        Assert.Contains(lista!, t => t.Id == uj!.Id && t.SzuloId == LeltarGyar.SzamitastechnikaTipus);
    }

    [Fact]
    public async Task Tipus_nem_sorolhato_onmaga_vagy_a_sajat_altipusa_ala()
    {
        var felelos = await FelelosAsync();
        var url = ApiUtvonalak.EszkozTipus(LeltarGyar.SzamitastechnikaTipus);

        var altipusAla = await felelos.PutAsJsonAsync(url, new EszkozTipusMentes("Számítástechnika", LeltarGyar.MonitorTipus));
        var onmagaAla = await felelos.PutAsJsonAsync(url, new EszkozTipusMentes("Számítástechnika", LeltarGyar.SzamitastechnikaTipus));
        var ismeretlenAla = await felelos.PutAsJsonAsync(url, new EszkozTipusMentes("Számítástechnika", 9999));

        Assert.Equal(HttpStatusCode.BadRequest, altipusAla.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, onmagaAla.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, ismeretlenAla.StatusCode);
    }

    [Fact]
    public async Task Hasznalt_tipus_nem_inaktivalhato_a_hasznalaton_kivuli_igen()
    {
        var felelos = await FelelosAsync();
        var uj = await (await felelos.PostAsJsonAsync(ApiUtvonalak.EszkozTipusok, new EszkozTipusMentes("Hangtechnika")))
            .Content.ReadFromJsonAsync<EszkozTipusKezelesAdat>();

        var eszkozzel = await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozTipus(LeltarGyar.MonitorTipus), new EszkozTipusMentes("Monitor", LeltarGyar.SzamitastechnikaTipus, false));
        var altipussal = await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozTipus(LeltarGyar.SzamitastechnikaTipus), new EszkozTipusMentes("Számítástechnika", Aktiv: false));
        var hasznalatlan = await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozTipus(uj!.Id), new EszkozTipusMentes("Hangtechnika", Aktiv: false));
        var lista = await felelos.GetFromJsonAsync<List<EszkozTipusAdat>>(ApiUtvonalak.EszkozTipusok);
        var mind = await felelos.GetFromJsonAsync<List<EszkozTipusKezelesAdat>>(ApiUtvonalak.EszkozTipusKezeles);

        Assert.Equal(HttpStatusCode.Conflict, eszkozzel.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, altipussal.StatusCode);
        Assert.Equal(HttpStatusCode.OK, hasznalatlan.StatusCode);
        Assert.DoesNotContain(lista!, t => t.Id == uj.Id);
        Assert.Equal((1, 0), mind!.Where(t => t.Id == LeltarGyar.MonitorTipus).Select(t => (t.Eszkozok, t.Altipusok)).Single());
    }

    [Fact]
    public async Task Felelos_szemely_felvetele_es_modositasa()
    {
        var felelos = await FelelosAsync();

        var valasz = await felelos.PostAsJsonAsync(ApiUtvonalak.Felelosok,
            new FelelosMentes("  Nagy   Péter ", "D0002", "nagy.peter@demo.local", "Mérnöki Kar"));
        var uj = await valasz.Content.ReadFromJsonAsync<FelelosKezelesAdat>();
        var azonosAzonosito = await felelos.PostAsJsonAsync(ApiUtvonalak.Felelosok, new FelelosMentes("Másik Péter", "d0002"));
        var rosszEmail = await felelos.PostAsJsonAsync(ApiUtvonalak.Felelosok, new FelelosMentes("Szabó Eszter", Email: "szabo.eszter"));
        var modositas = await felelos.PutAsJsonAsync(ApiUtvonalak.Felelos(uj!.Id), new FelelosMentes("Dr. Nagy Péter", "D0002"));
        var lista = await felelos.GetFromJsonAsync<List<FelelosAdat>>(ApiUtvonalak.Felelosok);

        Assert.Equal(HttpStatusCode.Created, valasz.StatusCode);
        Assert.Equal(("Nagy Péter", "D0002", "Mérnöki Kar"), (uj.Nev, uj.Azonosito, uj.SzervezetiEgyseg));
        Assert.Equal(HttpStatusCode.Conflict, azonosAzonosito.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rosszEmail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, modositas.StatusCode);
        Assert.Contains(lista!, f => f.Id == uj.Id && f.Nev == "Dr. Nagy Péter");
    }

    [Fact]
    public async Task Felelos_nem_inaktivalhato_amig_eszkoz_van_a_neven()
    {
        var felelos = await FelelosAsync();

        var valasz = await felelos.PutAsJsonAsync(ApiUtvonalak.Felelos(LeltarGyar.KovacsAnna), new FelelosMentes("Kovács Anna", Aktiv: false));
        var mind = await felelos.GetFromJsonAsync<List<FelelosKezelesAdat>>(ApiUtvonalak.FelelosKezeles);

        Assert.Equal(HttpStatusCode.Conflict, valasz.StatusCode);
        Assert.Equal((true, 1), mind!.Where(f => f.Id == LeltarGyar.KovacsAnna).Select(f => (f.Aktiv, f.Eszkozok)).Single());
    }
}

/// <summary>Az eszköz helyének, felelősének és típusának kézi módosítása (UC-13).</summary>
public class EszkozHozzarendelesTesztek(EszkozHozzarendelesTesztek.Gyar gyar) : IClassFixture<EszkozHozzarendelesTesztek.Gyar>
{
    public sealed class Gyar : LeltarGyar;

    private const long Monitor = 2;
    private const long Szek = 3;
    private const long RegiNyomtato = 5;
    private const long Terem102 = 2;

    private Task<HttpClient> FelelosAsync() => gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);

    internal static async Task<EszkozHozzarendelesek> HozzarendelesekAsync(HttpClient kliens, long eszkozId) =>
        (await kliens.GetFromJsonAsync<EszkozHozzarendelesek>(ApiUtvonalak.EszkozHozzarendelesek(eszkozId)))!;

    internal static async Task<EszkozTortenet> TortenetAsync(HttpClient kliens, long eszkozId) =>
        (await kliens.GetFromJsonAsync<EszkozTortenet>(ApiUtvonalak.EszkozTortenet(eszkozId)))!;

    [Fact]
    public async Task Felelosvaltas_lezarja_a_regit_megnyitja_az_ujat_es_megorzi_a_tortenetet()
    {
        var felelos = await FelelosAsync();
        var toth = await (await felelos.PostAsJsonAsync(ApiUtvonalak.Felelosok, new FelelosMentes("Tóth Gábor")))
            .Content.ReadFromJsonAsync<FelelosKezelesAdat>();

        var valasz = await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozFelelos(Monitor), new FelelosKeres(toth!.Id));
        var utana = await valasz.Content.ReadFromJsonAsync<EszkozHozzarendelesek>();
        await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozFelelos(Monitor), new FelelosKeres(toth.Id));
        var tortenet = (await TortenetAsync(felelos, Monitor)).Felelosok;

        Assert.Equal(HttpStatusCode.OK, valasz.StatusCode);
        Assert.Equal((toth.Id, "Tóth Gábor"), (utana!.FelelosId, utana.Felelos));
        Assert.Equal(["Kovács Anna", "Tóth Gábor"], tortenet.Select(f => f.Nev));
        Assert.Equal(tortenet[1].ErvenyesTol, tortenet[0].ErvenyesIg);
        Assert.Null(tortenet[1].ErvenyesIg);
        Assert.Equal("Leltár Felelős", tortenet[1].Rogzitette);

        var megszuntetes = await (await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozFelelos(Monitor), new FelelosKeres(null)))
            .Content.ReadFromJsonAsync<EszkozHozzarendelesek>();
        var vegul = (await TortenetAsync(felelos, Monitor)).Felelosok;

        Assert.Null(megszuntetes!.FelelosId);
        Assert.Equal(2, vegul.Count);
        Assert.All(vegul, f => Assert.NotNull(f.ErvenyesIg));
    }

    [Fact]
    public async Task Kezi_athelyezes_helytortenetet_ir_es_a_lista_szurheto_ra()
    {
        var felelos = await FelelosAsync();

        var elso = await (await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozHely(Szek), new HelyKeres(LeltarGyar.Terem101)))
            .Content.ReadFromJsonAsync<EszkozHozzarendelesek>();
        var masodik = await (await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozHely(Szek), new HelyKeres(Terem102)))
            .Content.ReadFromJsonAsync<EszkozHozzarendelesek>();
        var helyek = (await TortenetAsync(felelos, Szek)).Helyek;
        var szurt = await felelos.GetFromJsonAsync<LapozottLista<EszkozListaElem>>($"{ApiUtvonalak.Eszkozok}?helyisegId={Terem102}");

        Assert.Equal(("I 101", ElhelyezesForras.Kezi), (elso!.Helyiseg, elso.HelyForras));
        Assert.Equal((Terem102, "I 102"), (masodik!.HelyisegId, masodik.Helyiseg));
        Assert.Equal(["I 101", "I 102"], helyek.Select(h => h.Helyiseg));
        Assert.Equal(helyek[1].ErvenyesTol, helyek[0].ErvenyesIg);
        Assert.All(helyek, h => Assert.Equal((ElhelyezesForras.Kezi, "Leltár Felelős"), (h.Forras, h.Rogzitette)));
        Assert.Contains(szurt!.Elemek, e => e.Id == Szek && e.Helyiseg == "I 102");

        var ismeretlenHely = await (await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozHely(Szek), new HelyKeres(null)))
            .Content.ReadFromJsonAsync<EszkozHozzarendelesek>();

        Assert.Null(ismeretlenHely!.HelyisegId);
    }

    [Fact]
    public async Task Tipus_hozzarendelese_utan_a_szulotipusra_szurve_is_megjelenik()
    {
        var felelos = await FelelosAsync();

        var utana = await (await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozTipusa(Szek), new TipusKeres(LeltarGyar.MonitorTipus)))
            .Content.ReadFromJsonAsync<EszkozHozzarendelesek>();
        var szurt = await felelos.GetFromJsonAsync<LapozottLista<EszkozListaElem>>(
            $"{ApiUtvonalak.Eszkozok}?eszkozTipusId={LeltarGyar.SzamitastechnikaTipus}");
        var torles = await (await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozTipusa(Szek), new TipusKeres(null)))
            .Content.ReadFromJsonAsync<EszkozHozzarendelesek>();

        Assert.Equal((LeltarGyar.MonitorTipus, "Monitor"), (utana!.EszkozTipusId, utana.EszkozTipus));
        Assert.Contains(szurt!.Elemek, e => e.Id == Szek);
        Assert.Null(torles!.EszkozTipusId);
    }

    [Fact]
    public async Task Nem_letezo_vagy_inaktiv_cel_hibas_keres()
    {
        var felelos = await FelelosAsync();
        var inaktivHely = await (await felelos.PostAsJsonAsync(ApiUtvonalak.Helyisegek, new HelyisegMentes("Z", "1", Aktiv: false)))
            .Content.ReadFromJsonAsync<HelyisegKezelesAdat>();
        var inaktivFelelos = await (await felelos.PostAsJsonAsync(ApiUtvonalak.Felelosok, new FelelosMentes("Régi Kolléga", Aktiv: false)))
            .Content.ReadFromJsonAsync<FelelosKezelesAdat>();

        var valaszok = new[]
        {
            await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozHely(Szek), new HelyKeres(9999)),
            await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozHely(Szek), new HelyKeres(inaktivHely!.Id)),
            await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozFelelos(Szek), new FelelosKeres(9999)),
            await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozFelelos(Szek), new FelelosKeres(inaktivFelelos!.Id)),
            await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozTipusa(Szek), new TipusKeres(9999)),
        };
        var nincsEszkoz = await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozHely(9999), new HelyKeres(LeltarGyar.Terem101));

        Assert.All(valaszok, v => Assert.Equal(HttpStatusCode.BadRequest, v.StatusCode));
        Assert.Equal(HttpStatusCode.NotFound, nincsEszkoz.StatusCode);
    }

    [Fact]
    public async Task Megszunt_eszkoznek_nem_adhato_hely_es_felelos()
    {
        var felelos = await FelelosAsync();

        var hely = await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozHely(RegiNyomtato), new HelyKeres(LeltarGyar.Terem101));
        var szemely = await felelos.PutAsJsonAsync(ApiUtvonalak.EszkozFelelos(RegiNyomtato), new FelelosKeres(LeltarGyar.KovacsAnna));

        Assert.Equal(HttpStatusCode.BadRequest, hely.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, szemely.StatusCode);
    }

    [Fact]
    public async Task Modositani_csak_a_leltarfelelos_tud_megtekinteni_a_lathato_korzetekben_lehet()
    {
        var leltarozo = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var megtekinto = await gyar.BejelentkezettKliensAsync(LeltarGyar.Megtekinto);

        var modositas = await leltarozo.PutAsJsonAsync(ApiUtvonalak.EszkozHely(OsszesitesTesztek.Szamitogep), new HelyKeres(Terem102));
        var sajatKorzet = await leltarozo.GetAsync(ApiUtvonalak.EszkozHozzarendelesek(OsszesitesTesztek.Szamitogep));
        var masEgyseg = await megtekinto.GetAsync(ApiUtvonalak.EszkozHozzarendelesek(OsszesitesTesztek.Szamitogep));
        var masEgysegTortenet = await megtekinto.GetAsync(ApiUtvonalak.EszkozTortenet(OsszesitesTesztek.Szamitogep));
        var sajatEgyseg = await megtekinto.GetAsync(ApiUtvonalak.EszkozHozzarendelesek(Szek));

        Assert.Equal(HttpStatusCode.Forbidden, modositas.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sajatKorzet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, masEgyseg.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, masEgysegTortenet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sajatEgyseg.StatusCode);
    }
}

/// <summary>A beolvasás helyisége az eszköz helytörténetébe kerül (F-23, D-042).</summary>
public class HelyKovetesTesztek(HelyKovetesTesztek.Gyar gyar) : IClassFixture<HelyKovetesTesztek.Gyar>
{
    public sealed class Gyar : LeltarGyar;

    private const long Monitor = 2;
    private const long Szek = 3;
    private const long Targyaloszekek = 4;
    private const long RegiNyomtato = 5;
    private const long Terem102 = 2;

    private static async Task<BeolvasasEredmeny> BeolvasAsync(
        HttpClient kliens, long idoszakId, string kod, long? helyisegId, long korzetId = LeltarGyar.MikKorzet, int mennyiseg = 1)
    {
        var valasz = await kliens.PostAsJsonAsync(ApiUtvonalak.IdoszakLeolvasasai(idoszakId),
            new BeolvasasKeres(kod, korzetId, Mennyiseg: mennyiseg, HelyisegId: helyisegId));
        valasz.EnsureSuccessStatusCode();
        return (await valasz.Content.ReadFromJsonAsync<BeolvasasEredmeny>())!;
    }

    [Fact]
    public async Task Beolvasas_helyiseggel_athelyezi_az_eszkozt_a_sztorno_visszaallitja()
    {
        var leltarozo = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var beolvasas = await BeolvasAsync(leltarozo, id, "SAP-0001", Terem102);
        var utana = await EszkozHozzarendelesTesztek.HozzarendelesekAsync(leltarozo, OsszesitesTesztek.Szamitogep);
        var helyek = (await EszkozHozzarendelesTesztek.TortenetAsync(leltarozo, OsszesitesTesztek.Szamitogep)).Helyek;

        Assert.Equal(Minosites.Ok, beolvasas.Minosites);
        Assert.Equal((Terem102, "I 102", ElhelyezesForras.Leltar), (utana.HelyisegId, utana.Helyiseg, utana.HelyForras));
        Assert.Equal(["I 101", "I 102"], helyek.Select(h => h.Helyiseg));
        Assert.Equal(helyek[1].ErvenyesTol, helyek[0].ErvenyesIg);
        Assert.Equal((beolvasas.LeolvasasId, "Leltár Ozó"), (helyek[1].LeolvasasId, helyek[1].Rogzitette));

        var sztorno = await leltarozo.PostAsJsonAsync(ApiUtvonalak.Sztorno(beolvasas.LeolvasasId), new SztornoKeres("Rossz helyiséget választottam"));
        var visszaallitva = await EszkozHozzarendelesTesztek.HozzarendelesekAsync(leltarozo, OsszesitesTesztek.Szamitogep);
        var vegul = (await EszkozHozzarendelesTesztek.TortenetAsync(leltarozo, OsszesitesTesztek.Szamitogep)).Helyek;

        Assert.Equal(HttpStatusCode.NoContent, sztorno.StatusCode);
        Assert.Equal((LeltarGyar.Terem101, "I 101"), (visszaallitva.HelyisegId, visszaallitva.Helyiseg));
        Assert.Equal(["I 101", "I 102", "I 101"], vegul.Select(h => h.Helyiseg));
        Assert.Equal(vegul[2].ErvenyesTol, vegul[1].ErvenyesIg);
    }

    [Fact]
    public async Task Ugyanabban_a_helyisegben_beolvasva_nem_keletkezik_uj_helytortenet()
    {
        var leltarozo = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);
        var elotte = (await EszkozHozzarendelesTesztek.TortenetAsync(leltarozo, Monitor)).Helyek;

        await BeolvasAsync(leltarozo, id, "SAP-0002", Terem102);
        var utana = (await EszkozHozzarendelesTesztek.TortenetAsync(leltarozo, Monitor)).Helyek;

        Assert.Equal(elotte, utana);
    }

    [Fact]
    public async Task Mennyiseges_eszkoznel_latszik_a_helyisegenkenti_megoszlas()
    {
        var leltarozo = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        await BeolvasAsync(leltarozo, id, "SZEK-30", LeltarGyar.Terem101, mennyiseg: 2);
        await BeolvasAsync(leltarozo, id, "SZEK-30", Terem102, mennyiseg: 1);
        var hely = await EszkozHozzarendelesTesztek.HozzarendelesekAsync(leltarozo, Targyaloszekek);
        var leltar = (await EszkozHozzarendelesTesztek.TortenetAsync(leltarozo, Targyaloszekek)).Leltarak.Single(l => l.LeltarIdoszakId == id);

        Assert.Equal(Terem102, hely.HelyisegId);
        Assert.Equal((TetelAllapot.Rendben, 3, 3), (leltar.Allapot, leltar.ElvartMennyiseg, leltar.BeolvasottMennyiseg));
        Assert.Equal(
            [new HelyisegMennyiseg(LeltarGyar.Terem101, "I 101", 2), new HelyisegMennyiseg(Terem102, "I 102", 1)],
            leltar.Helyisegek);
    }

    [Fact]
    public async Task Megszunt_eszkoz_es_ismeretlen_kod_beolvasasa_nem_ir_helytortenetet()
    {
        var leltarozo = await gyar.BejelentkezettKliensAsync(LeltarGyar.Leltarozo);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var megszunt = await BeolvasAsync(leltarozo, id, "SAP-0005", LeltarGyar.Terem101);
        var ismeretlen = await BeolvasAsync(leltarozo, id, "NINCS-ILYEN-KOD", LeltarGyar.Terem101);
        var hely = await EszkozHozzarendelesTesztek.HozzarendelesekAsync(leltarozo, RegiNyomtato);

        Assert.Equal((Minosites.NemAktiv, Minosites.IsmeretlenKod), (megszunt.Minosites, ismeretlen.Minosites));
        Assert.Null(hely.HelyisegId);
    }

    [Fact]
    public async Task Mas_korzetben_helyiseggel_beolvasott_eszkoz_helye_frissul_helyiseg_nelkul_nem_valtozik()
    {
        var felelos = await gyar.BejelentkezettKliensAsync(LeltarGyar.Felelos);
        var id = await OsszesitesTesztek.NyitottIdoszakAsync(gyar);

        var masKorzet = await BeolvasAsync(felelos, id, "SAP-0003", LeltarGyar.Terem101);
        var helyiseggel = await EszkozHozzarendelesTesztek.HozzarendelesekAsync(felelos, Szek);
        await BeolvasAsync(felelos, id, "SAP-0003", null, LeltarGyar.GtkKorzet);
        var helyisegNelkul = await EszkozHozzarendelesTesztek.HozzarendelesekAsync(felelos, Szek);

        Assert.Equal(Minosites.MasKorzet, masKorzet.Minosites);
        Assert.Equal((LeltarGyar.Terem101, ElhelyezesForras.Leltar), (helyiseggel.HelyisegId, helyiseggel.HelyForras));
        Assert.Equal(helyiseggel, helyisegNelkul);
    }
}
