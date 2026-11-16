using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

[ApiController]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class KiegeszitokController(KiegeszitoSzolgaltatas kiegeszitok) : ControllerBase
{
    [HttpPost("api/kiegeszitok/{id:long}/lezaras")]
    [Authorize(Roles = Jogosultsagok.LeltarKezeles)]
    [ProducesResponseType<KiegeszitoAdat>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<KiegeszitoAdat> Lezaras(long id, CancellationToken ct) => kiegeszitok.LezarasAsync(id, User, ct);

    [HttpGet("api/leolvasasok/{leolvasasId:long}/kiegeszitok")]
    [Authorize(Roles = Jogosultsagok.Beolvasas)]
    [ProducesResponseType<IReadOnlyList<KiegeszitoAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<KiegeszitoAdat>> LeolvasasKiegeszitoi(long leolvasasId, CancellationToken ct) =>
        kiegeszitok.LeolvasasKiegeszitoiAsync(leolvasasId, User, ct);

    [HttpPut("api/leolvasasok/{leolvasasId:long}/kiegeszitok/{kiegeszitoId:long}")]
    [Authorize(Roles = Jogosultsagok.Beolvasas)]
    [ProducesResponseType<IReadOnlyList<KiegeszitoAdat>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IReadOnlyList<KiegeszitoAdat>> Ellenorzes(
        long leolvasasId, long kiegeszitoId, KiegeszitoEllenorzesKeres keres, CancellationToken ct) =>
        kiegeszitok.EllenorzesAsync(leolvasasId, kiegeszitoId, keres, User, ct);
}
