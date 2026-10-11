using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

[ApiController]
[Authorize(Roles = Jogosultsagok.Beolvasas)]
public class LeolvasasokController(BeolvasasSzolgaltatas beolvasas) : ControllerBase
{
    [HttpPost("api/leltaridoszakok/{idoszakId:long}/leolvasasok")]
    [ProducesResponseType<BeolvasasEredmeny>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<BeolvasasEredmeny> Beolvasas(long idoszakId, BeolvasasKeres keres, CancellationToken ct) =>
        beolvasas.BeolvasasAsync(idoszakId, keres, User, ct);

    [HttpGet("api/leltaridoszakok/{idoszakId:long}/leolvasasok")]
    [ProducesResponseType<LapozottLista<LeolvasasListaElem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<LapozottLista<LeolvasasListaElem>> Listazas(
        long idoszakId, [FromQuery] LeolvasasSzuro szuro, CancellationToken ct) =>
        beolvasas.ListazasAsync(idoszakId, szuro, User, ct);

    [HttpGet("api/leltaridoszakok/{idoszakId:long}/demokod")]
    [ProducesResponseType<DemoKod>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DemoKod> Demokod(long idoszakId, long? leltarkorzetId, CancellationToken ct, bool ervenyes = true) =>
        beolvasas.DemoKodAsync(idoszakId, leltarkorzetId, ervenyes, ct);

    [HttpPost("api/leolvasasok/{id:long}/sztorno")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Sztorno(long id, SztornoKeres keres, CancellationToken ct)
    {
        await beolvasas.SztornoAsync(id, keres, User, ct);
        return NoContent();
    }
}
