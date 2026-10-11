using System.Linq.Expressions;
using System.Security.Claims;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public class LeltarIdoszakSzolgaltatas(LeltarDbContext db, TimeProvider ido)
{
    public const int MaxMegnevezesHossz = 200;

    private static readonly Expression<Func<LeltarIdoszak, LeltarIdoszakAdat>> Vetites = i => new LeltarIdoszakAdat(
        i.Id, i.Megnevezes, i.Tipus, i.Kezdete, i.Vege, i.Statusz, i.Lezarva, i.ElvartTetelek.Count);

    public async Task<IReadOnlyList<LeltarIdoszakAdat>> ListazasAsync(LeltarIdoszakStatusz? statusz, CancellationToken ct)
    {
        var lekerdezes = db.LeltarIdoszakok.AsNoTracking();
        if (statusz is { } s)
            lekerdezes = lekerdezes.Where(i => i.Statusz == s);

        return await lekerdezes
            .OrderByDescending(i => i.Kezdete).ThenByDescending(i => i.Id)
            .Select(Vetites)
            .ToListAsync(ct);
    }

    public Task<LeltarIdoszakAdat?> ReszletekAsync(long id, CancellationToken ct) =>
        db.LeltarIdoszakok.AsNoTracking().Where(i => i.Id == id).Select(Vetites).SingleOrDefaultAsync(ct);

    public async Task<LeltarIdoszakAdat> LetrehozasAsync(LeltarIdoszakLetrehozas keres, CancellationToken ct)
    {
        var megnevezes = keres.Megnevezes?.Trim() ?? "";
        if (megnevezes.Length == 0)
            throw UzletiHiba.Hibas("A leltáridőszak megnevezése kötelező.");
        if (megnevezes.Length > MaxMegnevezesHossz)
            throw UzletiHiba.Hibas($"A megnevezés legfeljebb {MaxMegnevezesHossz} karakter lehet.");
        if (!Enum.IsDefined(keres.Tipus))
            throw UzletiHiba.Hibas("Ismeretlen leltártípus.");
        if (keres.Vege < keres.Kezdete)
            throw UzletiHiba.Hibas("A leltáridőszak vége nem lehet korábbi a kezdeténél.");

        var idoszak = new LeltarIdoszak
        {
            Megnevezes = megnevezes,
            Tipus = keres.Tipus,
            Kezdete = keres.Kezdete,
            Vege = keres.Vege,
            Statusz = LeltarIdoszakStatusz.Tervezett,
        };
        db.LeltarIdoszakok.Add(idoszak);
        await db.SaveChangesAsync(ct);

        return (await ReszletekAsync(idoszak.Id, ct))!;
    }

    public async Task<LeltarIdoszakAdat> NyitasAsync(long id, CancellationToken ct)
    {
        var idoszak = await KeresesAsync(id, ct);
        if (idoszak.Statusz != LeltarIdoszakStatusz.Tervezett)
            throw UzletiHiba.Utkozes(idoszak.Statusz == LeltarIdoszakStatusz.Nyitott
                ? "A leltáridőszak már nyitva van."
                : "Lezárt leltáridőszak nem nyitható meg újra.");

        var pillanatkep = await db.Eszkozok.AsNoTracking()
            .Where(e => !e.EszkozAllapot.Megszunt)
            .Select(e => new ElvartTetel
            {
                LeltarIdoszakId = id,
                EszkozId = e.Id,
                LeltarkorzetId = e.LeltarkorzetId,
                ElvartMennyiseg = e.ElvartMennyiseg,
                FelvitelOka = ElvartTetelFelvitelOka.Pillanatkep,
            })
            .ToListAsync(ct);

        db.ElvartTetelek.AddRange(pillanatkep);
        idoszak.Statusz = LeltarIdoszakStatusz.Nyitott;
        await db.SaveChangesAsync(ct);

        return (await ReszletekAsync(id, ct))!;
    }

    public async Task<LeltarIdoszakAdat> LezarasAsync(long id, ClaimsPrincipal felhasznalo, CancellationToken ct)
    {
        var idoszak = await KeresesAsync(id, ct);
        if (idoszak.Statusz != LeltarIdoszakStatusz.Nyitott)
            throw UzletiHiba.Utkozes(idoszak.Statusz == LeltarIdoszakStatusz.Lezart
                ? "A leltáridőszak már le van zárva."
                : "Csak nyitott leltáridőszak zárható le.");

        idoszak.Statusz = LeltarIdoszakStatusz.Lezart;
        idoszak.Lezarva = ido.GetUtcNow().UtcDateTime;
        idoszak.LezartaId = felhasznalo.FelhasznaloId();
        await db.SaveChangesAsync(ct);

        return (await ReszletekAsync(id, ct))!;
    }

    private async Task<LeltarIdoszak> KeresesAsync(long id, CancellationToken ct) =>
        await db.LeltarIdoszakok.SingleOrDefaultAsync(i => i.Id == id, ct)
        ?? throw UzletiHiba.NemTalalhato($"Nincs {id} azonosítójú leltáridőszak.");
}
