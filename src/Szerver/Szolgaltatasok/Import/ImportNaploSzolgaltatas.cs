using ClosedXML.Excel;
using LeltarKezelo.Kozos;
using LeltarKezelo.Kozos.Import;
using LeltarKezelo.Szerver.Adat;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Szolgaltatasok.Import;

/// <summary>Az importnapló (<c>ImportFutas</c>, <c>ImportHiba</c>) lekérdezése és a hibák Excelbe exportálása.</summary>
public class ImportNaploSzolgaltatas(LeltarDbContext db)
{
    public async Task<LapozottLista<ImportFutasListaElem>> ListazasAsync(ImportNaploSzuro szuro, CancellationToken ct)
    {
        var oldal = Math.Max(1, szuro.Oldal);
        var meret = Math.Clamp(szuro.OldalMeret, 1, ImportNaploSzuro.MaxOldalMeret);

        var lekerdezes = db.ImportFutasok.AsNoTracking();
        if (szuro.Statusz is { } statusz)
            lekerdezes = lekerdezes.Where(f => f.Statusz == statusz);

        var osszesen = await lekerdezes.CountAsync(ct);
        var elemek = await lekerdezes
            .OrderByDescending(f => f.Idopont)
            .ThenByDescending(f => f.Id)
            .Skip((oldal - 1) * meret)
            .Take(meret)
            .Select(f => new ImportFutasListaElem(
                f.Id, f.Fajlnev, f.Idopont, f.Felhasznalo.Nev, f.SorokSzama, f.Sikeres, f.Hibas, f.Statusz))
            .ToListAsync(ct);

        return new LapozottLista<ImportFutasListaElem>(elemek, oldal, meret, osszesen);
    }

    public Task<ImportFutasReszletek?> ReszletekAsync(long id, CancellationToken ct) =>
        db.ImportFutasok.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new ImportFutasReszletek(
                f.Id, f.Fajlnev, f.Idopont, f.Felhasznalo.Nev, f.SorokSzama, f.Sikeres, f.Hibas, f.Statusz,
                f.Hibak.OrderBy(h => h.Sor).ThenBy(h => h.Id)
                    .Select(h => new ImportHibaAdat(h.Sor, h.Oszlop, h.Uzenet, h.Ertek))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

    /// <summary>A futás hibái egy munkalapon (sor, oszlop, hiba, érték), hogy a forrásfájlt javítani lehessen.</summary>
    public async Task<byte[]?> HibakExcelAsync(long id, CancellationToken ct)
    {
        var futas = await ReszletekAsync(id, ct);
        if (futas is null)
            return null;

        using var munkafuzet = new XLWorkbook();
        var lap = munkafuzet.AddWorksheet("Importhibák");

        lap.Cell(1, 1).Value = $"Importhibák – {futas.Fajlnev}";
        lap.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        lap.Cell(2, 1).Value =
            $"Futás: {futas.Id} · {MagyarIdo(futas.Idopont):yyyy.MM.dd. HH:mm} · {futas.Felhasznalo} · {futas.Statusz} · " +
            $"{futas.SorokSzama} sor, ebből {futas.Sikeres} sikeres, {futas.Hibas} hibás";

        const int fejlecSor = 4;
        string[] fejlec = ["Excel-sor", "Oszlop", "Hiba", "Hibás érték"];
        for (var c = 0; c < fejlec.Length; c++)
            lap.Cell(fejlecSor, c + 1).Value = fejlec[c];
        lap.Range(fejlecSor, 1, fejlecSor, fejlec.Length).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D9E2E3"));

        var r = fejlecSor + 1;
        foreach (var hiba in futas.Hibak)
        {
            lap.Cell(r, 1).Value = hiba.Sor;
            if (hiba.Oszlop is not null)
                lap.Cell(r, 2).Value = hiba.Oszlop;
            lap.Cell(r, 3).Value = hiba.Uzenet;
            if (hiba.Ertek is not null)
                lap.Cell(r, 4).Value = hiba.Ertek;
            r++;
        }
        if (futas.Hibak.Count == 0)
            lap.Cell(r, 1).Value = "Ebben a futásban nem volt hiba.";
        else
            lap.Range(fejlecSor, 1, r - 1, fejlec.Length).SetAutoFilter();

        lap.SheetView.FreezeRows(fejlecSor);
        lap.Columns(1, fejlec.Length).AdjustToContents(fejlecSor, Math.Max(fejlecSor, r - 1));
        lap.Column(3).Width = Math.Min(lap.Column(3).Width, 80);

        using var kimenet = new MemoryStream();
        munkafuzet.SaveAs(kimenet);
        return kimenet.ToArray();
    }

    private static DateTime MagyarIdo(DateTime utc)
    {
        try
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.SpecifyKind(utc, DateTimeKind.Utc), "Central Europe Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return utc;
        }
    }
}
