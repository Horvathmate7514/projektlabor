using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

[ApiController]
[Route("api/leltaridoszakok")]
public class LeltarIdoszakokController(LeltarIdoszakSzolgaltatas idoszakok) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LeltarIdoszakAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<LeltarIdoszakAdat>> Listazas([FromQuery] LeltarIdoszakStatusz? statusz, CancellationToken ct) =>
        idoszakok.ListazasAsync(statusz, ct);

    [HttpGet("{id:long}")]
    [ProducesResponseType<LeltarIdoszakAdat>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeltarIdoszakAdat>> Reszletek(long id, CancellationToken ct)
    {
        var idoszak = await idoszakok.ReszletekAsync(id, ct);
        return idoszak is null ? Problem($"Nincs {id} azonosítójú leltáridőszak.", statusCode: StatusCodes.Status404NotFound) : idoszak;
    }

    [HttpPost]
    [Authorize(Roles = Jogosultsagok.LeltarKezeles)]
    [ProducesResponseType<LeltarIdoszakAdat>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LeltarIdoszakAdat>> Letrehozas(LeltarIdoszakLetrehozas keres, CancellationToken ct)
    {
        var idoszak = await idoszakok.LetrehozasAsync(keres, ct);
        return CreatedAtAction(nameof(Reszletek), new { id = idoszak.Id }, idoszak);
    }

    /// <summary>Megnyitja az időszakot, és pillanatképet készít az elvárt állományról.</summary>
    [HttpPost("{id:long}/nyitas")]
    [Authorize(Roles = Jogosultsagok.LeltarKezeles)]
    [ProducesResponseType<LeltarIdoszakAdat>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<LeltarIdoszakAdat> Nyitas(long id, CancellationToken ct) => idoszakok.NyitasAsync(id, ct);

    [HttpPost("{id:long}/lezaras")]
    [Authorize(Roles = Jogosultsagok.LeltarKezeles)]
    [ProducesResponseType<LeltarIdoszakAdat>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<LeltarIdoszakAdat> Lezaras(long id, CancellationToken ct) => idoszakok.LezarasAsync(id, User, ct);
}
