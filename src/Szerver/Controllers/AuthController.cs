using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthSzolgaltatas auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<BejelentkezesValasz>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BejelentkezesValasz>> Bejelentkezes(BejelentkezesKeres keres, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(keres.Felhasznalonev) || string.IsNullOrEmpty(keres.Jelszo))
            return Problem("A felhasználónév és a jelszó megadása kötelező.", statusCode: StatusCodes.Status400BadRequest);

        var valasz = await auth.BejelentkezesAsync(keres, ct);
        if (valasz is null)
            return Problem("Hibás felhasználónév vagy jelszó.", statusCode: StatusCodes.Status401Unauthorized);

        return valasz;
    }

    [HttpGet("en")]
    [ProducesResponseType<FelhasznaloAdatok>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<FelhasznaloAdatok> Sajat()
    {
        var id = long.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        return new FelhasznaloAdatok(
            id,
            User.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "",
            User.FindFirstValue(JwtRegisteredClaimNames.Name) ?? "",
            User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList());
    }
}
