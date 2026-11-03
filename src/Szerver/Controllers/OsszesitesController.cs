using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

[ApiController]
[Route("api/leltaridoszakok/{idoszakId:long}")]
public class OsszesitesController(OsszesitesSzolgaltatas osszesites) : ControllerBase
{
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
