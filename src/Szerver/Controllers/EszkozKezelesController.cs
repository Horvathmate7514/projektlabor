using LeltarKezelo.Kozos.Eszkozok;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

/// <summary>
/// Egy eszköz helye, felelőse, típusa, állapota, kiegészítői és története (UC-12–UC-14).
/// A lekérdezéseket minden bejelentkezett felhasználó elérheti a látható körzeteiben, a módosítás a leltárfelelősé.
/// </summary>
[ApiController]
[Route("api/eszkozok/{eszkozId:long}")]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class EszkozKezelesController(
    EszkozKezelesSzolgaltatas eszkozok, KiegeszitoSzolgaltatas kiegeszitok, TortenetSzolgaltatas tortenet) : ControllerBase
{
    [HttpGet("hozzarendelesek")]
    [ProducesResponseType<EszkozHozzarendelesek>(StatusCodes.Status200OK)]
    public Task<EszkozHozzarendelesek> Hozzarendelesek(long eszkozId, CancellationToken ct) =>
        eszkozok.HozzarendelesekAsync(eszkozId, User, ct);

    [HttpPut("hely")]
    [Authorize(Roles = Jogosultsagok.LeltarKezeles)]
    [ProducesResponseType<EszkozHozzarendelesek>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EszkozHozzarendelesek> Hely(long eszkozId, HelyKeres keres, CancellationToken ct) =>
        eszkozok.HelyAsync(eszkozId, keres, User, ct);

    [HttpPut("felelos")]
    [Authorize(Roles = Jogosultsagok.LeltarKezeles)]
    [ProducesResponseType<EszkozHozzarendelesek>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EszkozHozzarendelesek> Felelos(long eszkozId, FelelosKeres keres, CancellationToken ct) =>
        eszkozok.FelelosAsync(eszkozId, keres, User, ct);

    [HttpPut("tipus")]
    [Authorize(Roles = Jogosultsagok.LeltarKezeles)]
    [ProducesResponseType<EszkozHozzarendelesek>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EszkozHozzarendelesek> Tipus(long eszkozId, TipusKeres keres, CancellationToken ct) =>
        eszkozok.TipusAsync(eszkozId, keres, User, ct);

    [HttpPost("allapot")]
    [Authorize(Roles = Jogosultsagok.LeltarKezeles)]
    [ProducesResponseType<EszkozHozzarendelesek>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<EszkozHozzarendelesek> Allapot(long eszkozId, AllapotKeres keres, CancellationToken ct) =>
        eszkozok.AllapotAsync(eszkozId, keres, User, ct);

    [HttpGet("tortenet")]
    [ProducesResponseType<EszkozTortenet>(StatusCodes.Status200OK)]
    public Task<EszkozTortenet> Tortenet(long eszkozId, CancellationToken ct) =>
        tortenet.EszkozTortenetAsync(eszkozId, User, ct);

    [HttpGet("pillanat")]
    [ProducesResponseType<EszkozPillanat>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<EszkozPillanat> Pillanat(long eszkozId, [FromQuery] DateTime idopont, CancellationToken ct) =>
        tortenet.EszkozPillanatAsync(eszkozId, idopont, User, ct);

    [HttpGet("kiegeszitok")]
    [ProducesResponseType<IReadOnlyList<KiegeszitoAdat>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<KiegeszitoAdat>> Kiegeszitok(
        long eszkozId, long? leltarIdoszakId, CancellationToken ct, bool lezartakkal = false) =>
        kiegeszitok.EszkozKiegeszitoiAsync(eszkozId, leltarIdoszakId, lezartakkal, User, ct);

    [HttpPost("kiegeszitok")]
    [Authorize(Roles = Jogosultsagok.LeltarKezeles)]
    [ProducesResponseType<KiegeszitoAdat>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KiegeszitoAdat>> KiegeszitoFelvetel(long eszkozId, KiegeszitoFelvetel keres, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await kiegeszitok.FelvetelAsync(eszkozId, keres, User, ct));
}
