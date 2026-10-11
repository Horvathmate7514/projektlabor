using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public class UzletiHiba(int statusz, string uzenet) : Exception(uzenet)
{
    public int Statusz { get; } = statusz;

    public static UzletiHiba Hibas(string uzenet) => new(StatusCodes.Status400BadRequest, uzenet);
    public static UzletiHiba Tiltott(string uzenet) => new(StatusCodes.Status403Forbidden, uzenet);
    public static UzletiHiba NemTalalhato(string uzenet) => new(StatusCodes.Status404NotFound, uzenet);
    public static UzletiHiba Utkozes(string uzenet) => new(StatusCodes.Status409Conflict, uzenet);
}

public class UzletiHibaSzuro(ProblemDetailsFactory problemDetails) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (statusz, uzenet) = context.Exception switch
        {
            UzletiHiba h => (h.Statusz, h.Message),
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
