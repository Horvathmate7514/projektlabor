using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

/// <summary>
/// Üzleti szabály megsértése (pl. lezárt időszakba beolvasás). Az üzenet a felhasználónak szól, magyarul;
/// az <see cref="UzletiHibaSzuro"/> Problem Details válasszá alakítja (D-018, D-029).
/// </summary>
public class UzletiHiba(int statusz, string uzenet) : Exception(uzenet)
{
    public int Statusz { get; } = statusz;

    public static UzletiHiba Hibas(string uzenet) => new(StatusCodes.Status400BadRequest, uzenet);
    public static UzletiHiba Tiltott(string uzenet) => new(StatusCodes.Status403Forbidden, uzenet);
    public static UzletiHiba NemTalalhato(string uzenet) => new(StatusCodes.Status404NotFound, uzenet);
    public static UzletiHiba Utkozes(string uzenet) => new(StatusCodes.Status409Conflict, uzenet);
}

/// <summary>
/// MVC kivételszűrő: az üzleti hibát a controller szintjén Problem Details válasszá alakítja. Szűrőként (nem
/// köztes rétegként) a várt 4xx eseteket nem naplózza szerverhibaként, és a válasz <c>traceId</c>-t is kap.
/// </summary>
public class UzletiHibaSzuro(ProblemDetailsFactory problemDetails) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (statusz, uzenet) = context.Exception switch
        {
            UzletiHiba h => (h.Statusz, h.Message),
            // Egyidejű módosítás (RowVersion): valaki más közben megváltoztatta ugyanazt az adatot.
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
                "Az adatot időközben más módosította. Frissítsd a nézetet, és próbáld újra."),
            _ => (0, ""),
        };
        if (statusz == 0)
            return;

        var problema = problemDetails.CreateProblemDetails(context.HttpContext, statusz, detail: uzenet);
        context.Result = new ObjectResult(problema) { StatusCode = statusz, ContentTypes = { "application/problem+json" } };
        context.ExceptionHandled = true;
    }
}
