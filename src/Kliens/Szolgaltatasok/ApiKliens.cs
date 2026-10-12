using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Kozos.Import;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Kozos.Torzsadatok;

namespace Leltarkezelo.Client.Szolgaltatasok;

/// <summary>Hiba a szerverhívás során; az üzenet a felhasználónak megjeleníthető.</summary>
public sealed class ApiHiba(HttpStatusCode? statusz, string uzenet) : Exception(uzenet)
{
    public HttpStatusCode? Statusz { get; } = statusz;
}

/// <summary>
/// A szerver REST API-jának típusos elérése. Az útvonalak és a típusok a közös projektből jönnek
/// (<see cref="ApiUtvonalak"/>), a hibák Problem Details formában érkeznek (D-018), ezekből <see cref="ApiHiba"/> lesz.
/// </summary>
public sealed class ApiKliens(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Bejelentkezett állapotban 401-es válasz érkezett: a token lejárt vagy érvénytelen.</summary>
    public event EventHandler? MunkamenetLejart;

    public Uri? SzerverCim => http.BaseAddress;

    public void TokenBeallitasa(string? token) =>
        http.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);

    // ---------------------------------------------------------------- hitelesítés, állapot

    public Task<SzerverAllapot> AllapotAsync(CancellationToken ct = default) =>
        GetAsync<SzerverAllapot>(ApiUtvonalak.Allapot, ct);

    public async Task<BejelentkezesValasz> BejelentkezesAsync(string felhasznalonev, string jelszo, CancellationToken ct = default)
    {
        using var keres = new HttpRequestMessage(HttpMethod.Post, ApiUtvonalak.Bejelentkezes)
        {
            Content = JsonContent.Create(new BejelentkezesKeres(felhasznalonev, jelszo), options: Json),
        };
        return await KuldesAsync<BejelentkezesValasz>(keres, bejelentkezes: true, ct);
    }

    // ---------------------------------------------------------------- eszközök, törzsadatok

    public Task<LapozottLista<EszkozListaElem>> EszkozokAsync(EszkozSzuro szuro, CancellationToken ct = default) =>
        GetAsync<LapozottLista<EszkozListaElem>>(ApiUtvonalak.Eszkozok + Lekerdezes(
            ("kereses", szuro.Kereses),
            ("leltarkorzetId", szuro.LeltarkorzetId),
            ("eszkozTipusId", szuro.EszkozTipusId),
            ("eszkozAllapotId", szuro.EszkozAllapotId),
            ("helyisegId", szuro.HelyisegId),
            ("felelosId", szuro.FelelosId),
            ("oldal", szuro.Oldal),
            ("oldalMeret", szuro.OldalMeret)), ct);

    public Task<EszkozReszletek> EszkozAsync(long id, CancellationToken ct = default) =>
        GetAsync<EszkozReszletek>($"{ApiUtvonalak.Eszkozok}/{id}", ct);

    public Task<List<LeltarkorzetAdat>> LeltarkorzetekAsync(CancellationToken ct = default) =>
        GetAsync<List<LeltarkorzetAdat>>(ApiUtvonalak.Leltarkorzetek, ct);

    public Task<List<EszkozTipusAdat>> EszkozTipusokAsync(CancellationToken ct = default) =>
        GetAsync<List<EszkozTipusAdat>>(ApiUtvonalak.EszkozTipusok, ct);

    public Task<List<EszkozAllapotAdat>> EszkozAllapotokAsync(CancellationToken ct = default) =>
        GetAsync<List<EszkozAllapotAdat>>(ApiUtvonalak.EszkozAllapotok, ct);

    public Task<List<HelyisegAdat>> HelyisegekAsync(CancellationToken ct = default) =>
        GetAsync<List<HelyisegAdat>>(ApiUtvonalak.Helyisegek, ct);

    public Task<List<FelelosAdat>> FelelosokAsync(CancellationToken ct = default) =>
        GetAsync<List<FelelosAdat>>(ApiUtvonalak.Felelosok, ct);

    // ---------------------------------------------------------------- leltározás

    public Task<List<LeltarIdoszakAdat>> LeltarIdoszakokAsync(LeltarIdoszakStatusz? statusz, CancellationToken ct = default) =>
        GetAsync<List<LeltarIdoszakAdat>>(ApiUtvonalak.LeltarIdoszakok + Lekerdezes(("statusz", statusz)), ct);

    public Task<BeolvasasEredmeny> BeolvasasAsync(long idoszakId, BeolvasasKeres keres, CancellationToken ct = default) =>
        PostAsync<BeolvasasEredmeny>(ApiUtvonalak.IdoszakLeolvasasai(idoszakId), keres, ct);

    public Task<LapozottLista<LeolvasasListaElem>> LeolvasasokAsync(long idoszakId, LeolvasasSzuro szuro, CancellationToken ct = default) =>
        GetAsync<LapozottLista<LeolvasasListaElem>>(ApiUtvonalak.IdoszakLeolvasasai(idoszakId) + Lekerdezes(
            ("leltarkorzetId", szuro.LeltarkorzetId),
            ("csakSajat", szuro.CsakSajat),
            ("sztornozottakkalEgyutt", szuro.SztornozottakkalEgyutt),
            ("oldal", szuro.Oldal),
            ("oldalMeret", szuro.OldalMeret)), ct);

    public async Task SztornoAsync(long leolvasasId, string indoklas, CancellationToken ct = default)
    {
        using var keres = new HttpRequestMessage(HttpMethod.Post, ApiUtvonalak.Sztorno(leolvasasId))
        {
            Content = JsonContent.Create(new SztornoKeres(indoklas), options: Json),
        };
        using var valasz = await KuldesAsync(keres, bejelentkezes: false, ct);
    }

    public Task<DemoKod> DemoKodAsync(long idoszakId, long korzetId, bool ervenyes, CancellationToken ct = default) =>
        GetAsync<DemoKod>(ApiUtvonalak.Demokod(idoszakId) + Lekerdezes(("leltarkorzetId", korzetId), ("ervenyes", ervenyes)), ct);

    // ---------------------------------------------------------------- import

    public Task<ImportElemzes> ImportProbafuttatasAsync(string fajlUt, ImportBeallitasok beallitasok, CancellationToken ct = default) =>
        FeltoltesAsync<ImportElemzes>(ApiUtvonalak.ImportProbafuttatas, fajlUt, beallitasok, ct);

    public Task<ImportEredmeny> ImportVegrehajtasAsync(string fajlUt, ImportBeallitasok beallitasok, CancellationToken ct = default) =>
        FeltoltesAsync<ImportEredmeny>(ApiUtvonalak.Importok, fajlUt, beallitasok, ct);

    public Task<LapozottLista<ImportFutasListaElem>> ImportNaploAsync(int oldal, int oldalMeret, CancellationToken ct = default) =>
        GetAsync<LapozottLista<ImportFutasListaElem>>(ApiUtvonalak.Importok + Lekerdezes(("oldal", oldal), ("oldalMeret", oldalMeret)), ct);

    public Task<ImportFutasReszletek> ImportFutasAsync(long id, CancellationToken ct = default) =>
        GetAsync<ImportFutasReszletek>(ApiUtvonalak.Import(id), ct);

    public async Task<byte[]> ImportHibakExcelAsync(long id, CancellationToken ct = default)
    {
        using var keres = new HttpRequestMessage(HttpMethod.Get, ApiUtvonalak.ImportHibakExcel(id));
        using var valasz = await KuldesAsync(keres, bejelentkezes: false, ct);
        return await valasz.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task<T> FeltoltesAsync<T>(string utvonal, string fajlUt, ImportBeallitasok beallitasok, CancellationToken ct)
    {
        FileStream fajl;
        try
        {
            fajl = File.OpenRead(fajlUt);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ApiHiba(null, $"A fájl nem nyitható meg: {ex.Message} Ha nyitva van Excelben, zárd be.");
        }

        await using var _ = fajl;
        var tartalom = new StreamContent(fajl);
        tartalom.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        using var urlap = new MultipartFormDataContent { { tartalom, ImportUrlap.Fajl, Path.GetFileName(fajlUt) } };
        urlap.Add(new StringContent(beallitasok.FrissitesekAlkalmazasa ? "true" : "false"), ImportUrlap.Frissitesek);
        if (!string.IsNullOrWhiteSpace(beallitasok.Munkalap))
            urlap.Add(new StringContent(beallitasok.Munkalap), ImportUrlap.Munkalap);
        if (beallitasok.Oszlopok is { Count: > 0 } oszlopok)
            urlap.Add(new StringContent(JsonSerializer.Serialize(oszlopok.ToDictionary(o => o.Key.ToString(), o => o.Value))), ImportUrlap.Oszlopok);

        using var keres = new HttpRequestMessage(HttpMethod.Post, utvonal) { Content = urlap };
        return await KuldesAsync<T>(keres, bejelentkezes: false, ct);
    }

    // ---------------------------------------------------------------- közös

    private async Task<T> GetAsync<T>(string utvonal, CancellationToken ct)
    {
        using var keres = new HttpRequestMessage(HttpMethod.Get, utvonal);
        return await KuldesAsync<T>(keres, bejelentkezes: false, ct);
    }

    private async Task<T> PostAsync<T>(string utvonal, object torzs, CancellationToken ct)
    {
        using var keres = new HttpRequestMessage(HttpMethod.Post, utvonal) { Content = JsonContent.Create(torzs, options: Json) };
        return await KuldesAsync<T>(keres, bejelentkezes: false, ct);
    }

    private async Task<T> KuldesAsync<T>(HttpRequestMessage keres, bool bejelentkezes, CancellationToken ct)
    {
        using var valasz = await KuldesAsync(keres, bejelentkezes, ct);
        return await valasz.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new ApiHiba(valasz.StatusCode, "A szerver üres választ adott.");
    }

    private async Task<HttpResponseMessage> KuldesAsync(HttpRequestMessage keres, bool bejelentkezes, CancellationToken ct)
    {
        HttpResponseMessage valasz;
        try
        {
            valasz = await http.SendAsync(keres, ct);
        }
        catch (HttpRequestException)
        {
            throw new ApiHiba(null, $"A szerver nem érhető el ({http.BaseAddress}). Ellenőrizd, hogy fut-e a szerver.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ApiHiba(null, "A szerver nem válaszolt időben. Próbáld újra.");
        }

        if (valasz.IsSuccessStatusCode)
            return valasz;

        using (valasz)
        {
            var uzenet = await HibauzenetAsync(valasz, ct);
            if (valasz.StatusCode == HttpStatusCode.Unauthorized && !bejelentkezes)
            {
                MunkamenetLejart?.Invoke(this, EventArgs.Empty);
                uzenet = "A bejelentkezés lejárt. Jelentkezz be újra.";
            }
            throw new ApiHiba(valasz.StatusCode, uzenet);
        }
    }

    private static async Task<string> HibauzenetAsync(HttpResponseMessage valasz, CancellationToken ct)
    {
        try
        {
            var problema = await valasz.Content.ReadFromJsonAsync<Problema>(Json, ct);
            if (!string.IsNullOrWhiteSpace(problema?.Detail))
                return problema.Detail;
            if (problema?.Errors is { Count: > 0 } hibak)
                return string.Join(" ", hibak.SelectMany(h => h.Value));
        }
        catch (JsonException)
        {
        }
        catch (NotSupportedException)
        {
        }

        return valasz.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Hibás felhasználónév vagy jelszó.",
            HttpStatusCode.Forbidden => "Ehhez a művelethez nincs jogosultságod.",
            HttpStatusCode.NotFound => "A keresett adat nem található.",
            HttpStatusCode.RequestEntityTooLarge => "A fájl túl nagy.",
            _ => $"A szerver hibát jelzett ({(int)valasz.StatusCode}).",
        };
    }

    private static string Lekerdezes(params (string Nev, object? Ertek)[] parameterek)
    {
        var reszek = parameterek
            .Where(p => p.Ertek is not null && !(p.Ertek is string s && string.IsNullOrWhiteSpace(s)))
            .Select(p => $"{p.Nev}={Uri.EscapeDataString(Szoveg(p.Ertek!))}")
            .ToList();
        return reszek.Count == 0 ? "" : "?" + string.Join("&", reszek);

        static string Szoveg(object ertek) => ertek switch
        {
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => ertek.ToString()!.Trim(),
        };
    }

    private sealed record Problema(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
