using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

[ApiController]
[Route("api/leltaridoszakok/{idoszakId:long}")]
public class OsszesitesController(OsszesitesSzolgaltatas osszesites, LeltarIdoszakSzolgaltatas idoszakok) : ControllerBase
{
    private const string XlsxTipus = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("tetelek.xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, XlsxTipus)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TetelekExcel(long idoszakId, [FromQuery] OsszesitesSzuro szuro, CancellationToken ct)
    {
        var tetelek = await osszesites.SzurtTetelekAsync(idoszakId, szuro, User, ct);
        var korzetek = await osszesites.KorzetekAsync(idoszakId, User, ct);
        var idoszak = (await idoszakok.ReszletekAsync(idoszakId, ct))!;

        var szuroLeiras = string.Join(", ", new[]
        {
            szuro.LeltarkorzetId is { } k ? $"körzet {k}" : null,
            szuro.Allapot is { } a ? OsszesitesExcel.AllapotNev(a) : null,
            szuro.CsakElteresek ? "csak eltérések" : null,
        }.Where(s => s is not null));
        var tartalom = OsszesitesExcel.Keszites(idoszak, korzetek, tetelek, szuroLeiras.Length == 0 ? "nincs" : szuroLeiras);
        return File(tartalom, XlsxTipus, $"leltar_{idoszakId}_tetelek.xlsx");
    }

    [HttpGet("osszevetes")]
    [ProducesResponseType<IdoszakOsszevetes>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IdoszakOsszevetes> Osszevetes(long idoszakId, [FromQuery] OsszevetesSzuro szuro, CancellationToken ct) =>
        osszesites.OsszevetesAsync(idoszakId, szuro, User, ct);

    [HttpGet("osszesites")]
    [ProducesResponseType<IReadOnlyList<KorzetOsszesites>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<KorzetOsszesites>> Korzetek(long idoszakId, CancellationToken ct) =>
        osszesites.KorzetekAsync(idoszakId, User, ct);

    [HttpGet("tetelek")]
    [ProducesResponseType<LapozottLista<OsszesitesTetel>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<LapozottLista<OsszesitesTetel>> Tetelek(long idoszakId, [FromQuery] OsszesitesSzuro szuro, CancellationToken ct) =>
        osszesites.TetelekAsync(idoszakId, szuro, User, ct);

    [HttpPost("elvart-tetelek")]
    [Authorize(Roles = Jogosultsagok.LeltarKezeles)]
    [ProducesResponseType<ElvartTetelAdat>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ElvartTetelAdat>> UtolagosFelvetel(long idoszakId, UtolagosTetelKeres keres, CancellationToken ct)
    {
        var tetel = await osszesites.UtolagosFelvetelAsync(idoszakId, keres, ct);
        return StatusCode(StatusCodes.Status201Created, tetel);
    }
}
