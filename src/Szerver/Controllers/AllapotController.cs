using System.Reflection;
using LeltarKezelo.Kozos;
using LeltarKezelo.Szerver.Adat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeltarKezelo.Szerver.Controllers;

[ApiController]
[Route("api/allapot")]
[AllowAnonymous]
public class AllapotController(LeltarDbContext db, TimeProvider ido) : ControllerBase
{
    [HttpGet]
    public async Task<SzerverAllapot> Lekerdezes(CancellationToken ct)
    {
        var verzio = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
        bool elerheto;
        try
        {
            elerheto = await db.Database.CanConnectAsync(ct);
        }
        catch
        {
            elerheto = false;
        }
        return new SzerverAllapot(verzio, elerheto, ido.GetUtcNow().UtcDateTime);
    }
}
