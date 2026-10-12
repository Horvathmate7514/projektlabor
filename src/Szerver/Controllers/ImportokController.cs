using System.Text.Json;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Import;
using LeltarKezelo.Szerver.Szolgaltatasok;
using LeltarKezelo.Szerver.Szolgaltatasok.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

[ApiController]
[Route(ApiUtvonalak.Importok)]
[Authorize(Roles = Jogosultsagok.LeltarKezeles)]
public class ImportokController(ImportSzolgaltatas import, ImportNaploSzolgaltatas naplo) : ControllerBase
{
    private const string XlsxTipus = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpPost("probafuttatas")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImportUrlap.MaxFajlMeret)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImportUrlap.MaxFajlMeret)]
    [ProducesResponseType<ImportElemzes>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ImportElemzes> Probafuttatas([FromForm] ImportFeltoltes feltoltes, CancellationToken ct)
    {
        var (fajl, beallitasok) = Ellenorzes(feltoltes);
        await using var adat = fajl.OpenReadStream();
        return await import.ElemzesAsync(adat, fajl.FileName, beallitasok, ct);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImportUrlap.MaxFajlMeret)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImportUrlap.MaxFajlMeret)]
    [ProducesResponseType<ImportEredmeny>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ImportEredmeny>> Vegrehajtas([FromForm] ImportFeltoltes feltoltes, CancellationToken ct)
    {
        var (fajl, beallitasok) = Ellenorzes(feltoltes);
        await using var adat = fajl.OpenReadStream();
        var eredmeny = await import.VegrehajtasAsync(adat, fajl.FileName, User.FelhasznaloId(), beallitasok, ct);
        return CreatedAtAction(nameof(Reszletek), new { id = eredmeny.ImportFutasId }, eredmeny);
    }

    [HttpGet]
    [ProducesResponseType<LapozottLista<ImportFutasListaElem>>(StatusCodes.Status200OK)]
    public Task<LapozottLista<ImportFutasListaElem>> Listazas([FromQuery] ImportNaploSzuro szuro, CancellationToken ct) =>
        naplo.ListazasAsync(szuro, ct);

    [HttpGet("{id:long}")]
    [ProducesResponseType<ImportFutasReszletek>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportFutasReszletek>> Reszletek(long id, CancellationToken ct)
    {
        var futas = await naplo.ReszletekAsync(id, ct);
        return futas is null ? NincsIlyenFutas(id) : futas;
    }

    [HttpGet("{id:long}/hibak.xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, XlsxTipus)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HibakExcel(long id, CancellationToken ct)
    {
        var tartalom = await naplo.HibakExcelAsync(id, ct);
        return tartalom is null ? NincsIlyenFutas(id) : File(tartalom, XlsxTipus, $"importhibak_{id}.xlsx");
    }

    private ObjectResult NincsIlyenFutas(long id) =>
        Problem($"Nincs {id} azonosítójú importfutás.", statusCode: StatusCodes.Status404NotFound);

    private static (IFormFile Fajl, ImportBeallitasok Beallitasok) Ellenorzes(ImportFeltoltes feltoltes)
    {
        if (feltoltes.Fajl is not { Length: > 0 } fajl)
            throw UzletiHiba.Hibas($"Nincs feltöltött fájl, vagy üres. A fájlt a „{ImportUrlap.Fajl}” mezőben kell küldeni.");

        return (fajl, new ImportBeallitasok
        {
            Munkalap = string.IsNullOrWhiteSpace(feltoltes.Munkalap) ? null : feltoltes.Munkalap.Trim(),
            Oszlopok = Oszlopok(feltoltes.Oszlopok),
            FrissitesekAlkalmazasa = feltoltes.Frissitesek,
        });
    }

    private static Dictionary<ImportMezo, string>? Oszlopok(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        Dictionary<string, string?>? nyers;
        try
        {
            nyers = JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
        }
        catch (JsonException)
        {
            throw UzletiHiba.Hibas(
                $"Az „{ImportUrlap.Oszlopok}” mező nem érvényes JSON objektum. Példa: {{\"Megnevezes\": \"Tétel neve\"}}.");
        }

        var oszlopok = new Dictionary<ImportMezo, string>();
        foreach (var (kulcs, fejlec) in nyers ?? [])
        {
            if (int.TryParse(kulcs, out _) || !Enum.TryParse<ImportMezo>(kulcs, ignoreCase: true, out var mezo))
                throw UzletiHiba.Hibas(
                    $"Ismeretlen importmező: „{kulcs}”. Lehetséges értékek: {string.Join(", ", Enum.GetNames<ImportMezo>())}.");
            if (!string.IsNullOrWhiteSpace(fejlec))
                oszlopok[mezo] = fejlec;
        }
        return oszlopok.Count == 0 ? null : oszlopok;
    }
}

/// <summary>A feltöltés űrlapmezői; a nevek az <see cref="ImportUrlap"/> állandói.</summary>
public sealed class ImportFeltoltes
{
    [FromForm(Name = ImportUrlap.Fajl)]
    public IFormFile? Fajl { get; set; }

    [FromForm(Name = ImportUrlap.Munkalap)]
    public string? Munkalap { get; set; }

    [FromForm(Name = ImportUrlap.Oszlopok)]
    public string? Oszlopok { get; set; }

    [FromForm(Name = ImportUrlap.Frissitesek)]
    public bool Frissitesek { get; set; } = true;
}
