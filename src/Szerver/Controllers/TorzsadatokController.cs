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
}
