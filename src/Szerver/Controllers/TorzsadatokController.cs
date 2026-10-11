using LeltarKezelo.Kozos.Torzsadatok;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

/// <summary>A kliens választólistáihoz szükséges törzsadatok (csak olvasás).</summary>
[ApiController]
public class TorzsadatokController(TorzsadatSzolgaltatas torzsadatok) : ControllerBase
{
    [HttpGet("api/leltarkorzetek")]
    [ProducesResponseType<IReadOnlyList<LeltarkorzetAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<LeltarkorzetAdat>> Leltarkorzetek(CancellationToken ct) =>
        torzsadatok.LeltarkorzetekAsync(User, ct);

    [HttpGet("api/kodtipusok")]
    [ProducesResponseType<IReadOnlyList<KodTipusAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<KodTipusAdat>> KodTipusok(CancellationToken ct) => torzsadatok.KodTipusokAsync(ct);

    [HttpGet("api/eszkoztipusok")]
    [ProducesResponseType<IReadOnlyList<EszkozTipusAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<EszkozTipusAdat>> EszkozTipusok(CancellationToken ct) => torzsadatok.EszkozTipusokAsync(ct);

    [HttpGet("api/eszkozallapotok")]
    [ProducesResponseType<IReadOnlyList<EszkozAllapotAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<EszkozAllapotAdat>> EszkozAllapotok(CancellationToken ct) => torzsadatok.EszkozAllapotokAsync(ct);

    [HttpGet("api/helyisegek")]
    [ProducesResponseType<IReadOnlyList<HelyisegAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<HelyisegAdat>> Helyisegek(CancellationToken ct) => torzsadatok.HelyisegekAsync(ct);

    [HttpGet("api/felelosok")]
    [ProducesResponseType<IReadOnlyList<FelelosAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<FelelosAdat>> Felelosok(CancellationToken ct) => torzsadatok.FelelosokAsync(ct);
}
