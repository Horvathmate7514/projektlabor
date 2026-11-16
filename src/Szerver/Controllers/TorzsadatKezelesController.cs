using LeltarKezelo.Kozos.Torzsadatok;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

/// <summary>Helyiségek, eszköztípusok és felelős személyek kezelése (UC-04); a listázás a <see cref="TorzsadatokController"/> feladata.</summary>
[ApiController]
[Authorize(Roles = Jogosultsagok.LeltarKezeles)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class TorzsadatKezelesController(TorzsadatKezelesSzolgaltatas torzsadatok) : ControllerBase
{
    [HttpGet("api/helyisegek/kezeles")]
    [ProducesResponseType<IReadOnlyList<HelyisegKezelesAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<HelyisegKezelesAdat>> Helyisegek(CancellationToken ct) => torzsadatok.HelyisegekAsync(ct);

    [HttpPost("api/helyisegek")]
    [ProducesResponseType<HelyisegKezelesAdat>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HelyisegKezelesAdat>> HelyisegLetrehozas(HelyisegMentes keres, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await torzsadatok.HelyisegLetrehozasAsync(keres, ct));

    [HttpPut("api/helyisegek/{id:long}")]
    [ProducesResponseType<HelyisegKezelesAdat>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<HelyisegKezelesAdat> HelyisegModositas(long id, HelyisegMentes keres, CancellationToken ct) =>
        torzsadatok.HelyisegModositasAsync(id, keres, ct);

    [HttpGet("api/eszkoztipusok/kezeles")]
    [ProducesResponseType<IReadOnlyList<EszkozTipusKezelesAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<EszkozTipusKezelesAdat>> EszkozTipusok(CancellationToken ct) => torzsadatok.EszkozTipusokAsync(ct);

    [HttpPost("api/eszkoztipusok")]
    [ProducesResponseType<EszkozTipusKezelesAdat>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EszkozTipusKezelesAdat>> EszkozTipusLetrehozas(EszkozTipusMentes keres, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await torzsadatok.EszkozTipusLetrehozasAsync(keres, ct));

    [HttpPut("api/eszkoztipusok/{id:long}")]
    [ProducesResponseType<EszkozTipusKezelesAdat>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<EszkozTipusKezelesAdat> EszkozTipusModositas(long id, EszkozTipusMentes keres, CancellationToken ct) =>
        torzsadatok.EszkozTipusModositasAsync(id, keres, ct);

    [HttpGet("api/felelosok/kezeles")]
    [ProducesResponseType<IReadOnlyList<FelelosKezelesAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<FelelosKezelesAdat>> Felelosok(CancellationToken ct) => torzsadatok.FelelosokAsync(ct);

    [HttpPost("api/felelosok")]
    [ProducesResponseType<FelelosKezelesAdat>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FelelosKezelesAdat>> FelelosLetrehozas(FelelosMentes keres, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await torzsadatok.FelelosLetrehozasAsync(keres, ct));

    [HttpPut("api/felelosok/{id:long}")]
    [ProducesResponseType<FelelosKezelesAdat>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<FelelosKezelesAdat> FelelosModositas(long id, FelelosMentes keres, CancellationToken ct) =>
        torzsadatok.FelelosModositasAsync(id, keres, ct);
}
