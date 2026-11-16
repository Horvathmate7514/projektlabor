using LeltarKezelo.Kozos.Torzsadatok;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using LeltarKezelo.Szerver.Szolgaltatasok.Import;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

/// <summary>
/// Helyiségek, eszköztípusok és felelős személyek felvétele és módosítása (UC-04).
/// Törlés nincs: a törzsadat inaktiválható, de csak akkor, ha már semmi nem használja (D-001, D-043).
/// </summary>
public class TorzsadatKezelesSzolgaltatas(LeltarDbContext db)
{
    // ---------------------------------------------------------------- helyiségek

    public async Task<IReadOnlyList<HelyisegKezelesAdat>> HelyisegekAsync(CancellationToken ct) =>
        await db.Helyisegek.AsNoTracking()
            .OrderBy(h => h.Epulet).ThenBy(h => h.Szobaszam)
            .Select(h => new HelyisegKezelesAdat(h.Id, h.Epulet, h.Emelet, h.Szobaszam, h.Megnevezes, h.Vonalkod, h.Aktiv,
                db.Elhelyezesek.Count(e => e.HelyisegId == h.Id && e.ErvenyesIg == null)))
            .ToListAsync(ct);

    public Task<HelyisegKezelesAdat> HelyisegLetrehozasAsync(HelyisegMentes keres, CancellationToken ct) =>
        HelyisegMentesAsync(null, keres, ct);

    public Task<HelyisegKezelesAdat> HelyisegModositasAsync(long id, HelyisegMentes keres, CancellationToken ct) =>
        HelyisegMentesAsync(id, keres, ct);

    private async Task<HelyisegKezelesAdat> HelyisegMentesAsync(long? id, HelyisegMentes keres, CancellationToken ct)
    {
        var epulet = Kotelezo(keres.Epulet, "épület", HelyisegMentes.MaxEpuletHossz);
        var szobaszam = Kotelezo(keres.Szobaszam, "szobaszám", HelyisegMentes.MaxSzobaszamHossz);
        var emelet = Opcionalis(keres.Emelet, "emelet", HelyisegMentes.MaxEmeletHossz);
        var megnevezes = Opcionalis(keres.Megnevezes, "megnevezés", HelyisegMentes.MaxMegnevezesHossz);
        var vonalkod = Opcionalis(keres.Vonalkod, "vonalkód", HelyisegMentes.MaxVonalkodHossz);

        var helyiseg = id is null
            ? new Helyiseg()
            : await db.Helyisegek.SingleOrDefaultAsync(h => h.Id == id, ct)
              ?? throw UzletiHiba.NemTalalhato($"Nincs {id} azonosítójú helyiség.");

        var tobbi = await db.Helyisegek.AsNoTracking()
            .Where(h => h.Id != helyiseg.Id)
            .Select(h => new { h.Epulet, h.Szobaszam, h.Vonalkod })
            .ToListAsync(ct);
        if (tobbi.Any(h => Azonos(h.Epulet, epulet) && Azonos(h.Szobaszam, szobaszam)))
            throw UzletiHiba.Utkozes($"Már van „{epulet} {szobaszam}” helyiség; az épület és a szobaszám együtt egyedi.");
        if (vonalkod is not null)
        {
            if (tobbi.Any(h => h.Vonalkod is not null && Azonos(h.Vonalkod, vonalkod)))
                throw UzletiHiba.Utkozes($"A(z) „{vonalkod}” vonalkód már egy másik helyiségé.");
            var norm = BeolvasasSzolgaltatas.Normalizal(vonalkod);
            if (await db.EszkozKodok.AnyAsync(k => k.Aktiv && k.ErtekNorm == norm, ct))
                throw UzletiHiba.Utkozes($"A(z) „{vonalkod}” érték egy eszköz aktív kódja, ezért nem lehet helyiség vonalkódja.");
        }
        if (helyiseg.Aktiv && !keres.Aktiv && id is not null)
        {
            var eszkozok = await db.Elhelyezesek.CountAsync(e => e.HelyisegId == id && e.ErvenyesIg == null, ct);
            if (eszkozok > 0)
                throw UzletiHiba.Utkozes($"A helyiség nem inaktiválható, mert {eszkozok} eszköz aktuális helye. Előbb helyezd át őket.");
        }

        helyiseg.Epulet = epulet;
        helyiseg.Szobaszam = szobaszam;
        helyiseg.Emelet = emelet;
        helyiseg.Megnevezes = megnevezes;
        helyiseg.Vonalkod = vonalkod;
        helyiseg.Aktiv = keres.Aktiv;
        if (id is null)
            db.Helyisegek.Add(helyiseg);
        await db.SaveChangesAsync(ct);

        return (await HelyisegekAsync(ct)).Single(h => h.Id == helyiseg.Id);
    }

    // ---------------------------------------------------------------- eszköztípusok

    public async Task<IReadOnlyList<EszkozTipusKezelesAdat>> EszkozTipusokAsync(CancellationToken ct) =>
        await db.EszkozTipusok.AsNoTracking()
            .OrderBy(t => t.Nev)
            .Select(t => new EszkozTipusKezelesAdat(t.Id, t.Nev, t.SzuloId, t.Aktiv,
                db.Eszkozok.Count(e => e.EszkozTipusId == t.Id),
                db.EszkozTipusok.Count(a => a.SzuloId == t.Id && a.Aktiv)))
            .ToListAsync(ct);

    public Task<EszkozTipusKezelesAdat> EszkozTipusLetrehozasAsync(EszkozTipusMentes keres, CancellationToken ct) =>
        EszkozTipusMentesAsync(null, keres, ct);

    public Task<EszkozTipusKezelesAdat> EszkozTipusModositasAsync(long id, EszkozTipusMentes keres, CancellationToken ct) =>
        EszkozTipusMentesAsync(id, keres, ct);

    private async Task<EszkozTipusKezelesAdat> EszkozTipusMentesAsync(long? id, EszkozTipusMentes keres, CancellationToken ct)
    {
        var nev = Kotelezo(keres.Nev, "típusnév", EszkozTipusMentes.MaxNevHossz);

        var tipusok = await db.EszkozTipusok.ToListAsync(ct);
        var tipus = id is null
            ? new EszkozTipus()
            : tipusok.SingleOrDefault(t => t.Id == id) ?? throw UzletiHiba.NemTalalhato($"Nincs {id} azonosítójú eszköztípus.");

        // Az import név alapján keres típust, ezért a név ékezet- és írásmódfüggetlenül egyedi (D-034).
        if (tipusok.Any(t => t != tipus && ImportErtekek.NormalFejlec(t.Nev) == ImportErtekek.NormalFejlec(nev)))
            throw UzletiHiba.Utkozes($"Már van „{nev}” nevű eszköztípus.");

        if (keres.SzuloId is { } szuloId)
        {
            var szulo = tipusok.SingleOrDefault(t => t.Id == szuloId);
            if (szulo is not { Aktiv: true })
                throw UzletiHiba.Hibas("A kiválasztott szülőtípus nem létezik vagy nem aktív.");
            for (var os = szulo; os is not null; os = tipusok.SingleOrDefault(t => t.Id == os.SzuloId))
                if (os == tipus)
                    throw UzletiHiba.Hibas("A típus nem lehet önmaga vagy egy saját altípusa alá sorolva.");
        }

        if (tipus.Aktiv && !keres.Aktiv && id is not null)
        {
            var eszkozok = await db.Eszkozok.CountAsync(e => e.EszkozTipusId == id, ct);
            if (eszkozok > 0)
                throw UzletiHiba.Utkozes($"A típus nem inaktiválható, mert {eszkozok} eszköz ebbe a típusba tartozik. Előbb sorold át őket.");
            if (tipusok.Any(t => t.SzuloId == id && t.Aktiv))
                throw UzletiHiba.Utkozes("A típus nem inaktiválható, mert aktív altípusa van.");
        }

        tipus.Nev = nev;
        tipus.SzuloId = keres.SzuloId;
        tipus.Aktiv = keres.Aktiv;
        if (id is null)
            db.EszkozTipusok.Add(tipus);
        await db.SaveChangesAsync(ct);

        return (await EszkozTipusokAsync(ct)).Single(t => t.Id == tipus.Id);
    }

    // ---------------------------------------------------------------- felelős személyek

    public async Task<IReadOnlyList<FelelosKezelesAdat>> FelelosokAsync(CancellationToken ct) =>
        await db.FelelosSzemelyek.AsNoTracking()
            .OrderBy(f => f.Nev).ThenBy(f => f.Id)
            .Select(f => new FelelosKezelesAdat(f.Id, f.Nev, f.Azonosito, f.Email, f.SzervezetiEgyseg, f.Aktiv,
                db.FelelosHozzarendelesek.Count(h => h.FelelosId == f.Id && h.ErvenyesIg == null)))
            .ToListAsync(ct);

    public Task<FelelosKezelesAdat> FelelosLetrehozasAsync(FelelosMentes keres, CancellationToken ct) =>
        FelelosMentesAsync(null, keres, ct);

    public Task<FelelosKezelesAdat> FelelosModositasAsync(long id, FelelosMentes keres, CancellationToken ct) =>
        FelelosMentesAsync(id, keres, ct);

    private async Task<FelelosKezelesAdat> FelelosMentesAsync(long? id, FelelosMentes keres, CancellationToken ct)
    {
        var nev = ImportErtekek.Tisztitott(Kotelezo(keres.Nev, "név", FelelosMentes.MaxNevHossz));
        var azonosito = Opcionalis(keres.Azonosito, "azonosító", FelelosMentes.MaxAzonositoHossz);
        var email = Opcionalis(keres.Email, "e-mail-cím", FelelosMentes.MaxEmailHossz);
        var egyseg = Opcionalis(keres.SzervezetiEgyseg, "szervezeti egység", FelelosMentes.MaxEgysegHossz);
        if (email is not null && (email.Contains(' ') || email.IndexOf('@') is < 1 || email.EndsWith('@')))
            throw UzletiHiba.Hibas("Az e-mail-cím formátuma nem megfelelő.");

        var felelos = id is null
            ? new FelelosSzemely()
            : await db.FelelosSzemelyek.SingleOrDefaultAsync(f => f.Id == id, ct)
              ?? throw UzletiHiba.NemTalalhato($"Nincs {id} azonosítójú felelős személy.");

        if (azonosito is not null)
        {
            var azonositok = await db.FelelosSzemelyek.AsNoTracking()
                .Where(f => f.Id != felelos.Id && f.Azonosito != null)
                .Select(f => f.Azonosito!)
                .ToListAsync(ct);
            if (azonositok.Any(a => Azonos(a, azonosito)))
                throw UzletiHiba.Utkozes($"A(z) „{azonosito}” azonosító már egy másik felelős személyé.");
        }
        if (felelos.Aktiv && !keres.Aktiv && id is not null)
        {
            var eszkozok = await db.FelelosHozzarendelesek.CountAsync(h => h.FelelosId == id && h.ErvenyesIg == null, ct);
            if (eszkozok > 0)
                throw UzletiHiba.Utkozes($"A felelős nem inaktiválható, mert {eszkozok} eszköz van a nevén. Előbb add át őket.");
        }

        felelos.Nev = nev;
        felelos.Azonosito = azonosito;
        felelos.Email = email;
        felelos.SzervezetiEgyseg = egyseg;
        felelos.Aktiv = keres.Aktiv;
        if (id is null)
            db.FelelosSzemelyek.Add(felelos);
        await db.SaveChangesAsync(ct);

        return (await FelelosokAsync(ct)).Single(f => f.Id == felelos.Id);
    }

    // ---------------------------------------------------------------- segédek

    private static string Kotelezo(string? ertek, string mezo, int maxHossz)
    {
        var szoveg = ertek?.Trim() ?? "";
        if (szoveg.Length == 0)
            throw UzletiHiba.Hibas($"Hiányzó kötelező mező: {mezo}.");
        if (szoveg.Length > maxHossz)
            throw UzletiHiba.Hibas($"A(z) {mezo} legfeljebb {maxHossz} karakter lehet.");
        return szoveg;
    }

    private static string? Opcionalis(string? ertek, string mezo, int maxHossz)
    {
        var szoveg = ertek?.Trim();
        if (string.IsNullOrEmpty(szoveg))
            return null;
        if (szoveg.Length > maxHossz)
            throw UzletiHiba.Hibas($"A(z) {mezo} legfeljebb {maxHossz} karakter lehet.");
        return szoveg;
    }

    private static bool Azonos(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
