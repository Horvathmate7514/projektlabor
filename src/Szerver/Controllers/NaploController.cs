using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Naplo;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

[ApiController]
[Route("api/auditnaplo")]
[Authorize(Roles = Jogosultsagok.LeltarKezeles)]
public class NaploController(TortenetSzolgaltatas tortenet) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<LapozottLista<NaploElem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<LapozottLista<NaploElem>> Listazas([FromQuery] NaploSzuro szuro, CancellationToken ct) =>
        tortenet.NaploAsync(szuro, ct);
}
