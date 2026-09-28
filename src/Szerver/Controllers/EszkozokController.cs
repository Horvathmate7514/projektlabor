using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

[ApiController]
[Route("api/eszkozok")]
public class EszkozokController(EszkozSzolgaltatas eszkozok) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<LapozottLista<EszkozListaElem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<LapozottLista<EszkozListaElem>> Listazas([FromQuery] EszkozSzuro szuro, CancellationToken ct) =>
        eszkozok.ListazasAsync(szuro, ct);

    [HttpGet("{id:long}")]
    [ProducesResponseType<EszkozReszletek>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EszkozReszletek>> Reszletek(long id, CancellationToken ct)
    {
        var eszkoz = await eszkozok.ReszletekAsync(id, ct);
        return eszkoz is null ? Problem($"Nincs {id} azonosítójú eszköz.", statusCode: StatusCodes.Status404NotFound) : eszkoz;
    }
}
