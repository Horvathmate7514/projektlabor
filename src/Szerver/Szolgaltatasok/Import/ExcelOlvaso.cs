using System.Globalization;
using ClosedXML.Excel;
using LeltarKezelo.Kozos.Import;

namespace LeltarKezelo.Szerver.Szolgaltatasok.Import;

/// <summary>Egy cella értéke háromféle értelmezésben: megjelenített szöveg, szám és dátum.</summary>
public readonly record struct ExcelErtek(string Szoveg, double? Szam, DateTime? Datum, string KodSzoveg);

public sealed record ExcelSor(int Sor, IReadOnlyDictionary<ImportMezo, ExcelErtek> Ertekek)
{
    public ExcelErtek? Ertek(ImportMezo mezo) => Ertekek.TryGetValue(mezo, out var e) ? e : null;
}

public sealed record ExcelTabla(
    string Munkalap,
    int FejlecSor,
    IReadOnlyList<ImportOszlop> Oszlopok,
    IReadOnlyList<string> IsmeretlenOszlopok,
    IReadOnlyList<ExcelSor> Sorok);

/// <summary>
/// Az .xlsx munkafüzet beolvasása ClosedXML-lel (D-008): munkalap és fejlécsor felismerése, oszlop-hozzárendelés,
/// az adatsorok értékei. Üzleti szabályt nem alkalmaz – azt az <see cref="ImportSzolgaltatas"/> végzi.
/// </summary>
public static class ExcelOlvaso
{
    public const int MaxSorok = 20_000;
    private const int FejlecKeresesSorok = 20;

    private static readonly byte[] XlsAlairas = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public static ExcelTabla Beolvasas(Stream fajl, ImportBeallitasok beallitasok)
    {
        using var munkafuzet = Megnyitas(fajl);

        var (lap, fejlecSor) = MunkalapEsFejlec(munkafuzet, beallitasok.Munkalap);
        var (oszlopok, ismeretlen) = OszlopHozzarendeles(lap, fejlecSor, beallitasok.Oszlopok);

        var hianyzo = ImportMezok.Kotelezo.Where(m => oszlopok.All(o => o.Mezo != m)).ToList();
        if (hianyzo.Count > 0)
            throw UzletiHiba.Hibas(
                $"Hiányzó kötelező oszlop: {string.Join(", ", hianyzo.Select(ImportMezok.Nev))}. " +
                $"Felismert oszlopok: {(oszlopok.Count == 0 ? "nincs" : string.Join(", ", oszlopok.Select(o => o.Fejlec)))}.");

        var utolso = lap.LastRowUsed()?.RowNumber() ?? fejlecSor;
        if (utolso - fejlecSor > MaxSorok)
            throw UzletiHiba.Hibas($"A munkalap legfeljebb {MaxSorok} adatsort tartalmazhat.");

        var sorok = new List<ExcelSor>();
        for (var r = fejlecSor + 1; r <= utolso; r++)
        {
            var ertekek = new Dictionary<ImportMezo, ExcelErtek>();
            foreach (var o in oszlopok)
            {
                var cella = lap.Cell(r, o.Oszlop);
                if (!cella.IsEmpty() && CellaErtek(cella) is { } e)
                    ertekek[o.Mezo] = e;
            }
            if (ertekek.Count > 0)
                sorok.Add(new ExcelSor(r, ertekek));
        }

        return new ExcelTabla(lap.Name, fejlecSor, oszlopok, ismeretlen, sorok);
    }

    private static XLWorkbook Megnyitas(Stream fajl)
    {
        var memoria = new MemoryStream();
        fajl.CopyTo(memoria);
        memoria.Position = 0;

        var fej = memoria.ToArray().Take(8).ToArray();
        if (fej.SequenceEqual(XlsAlairas))
            throw UzletiHiba.Hibas("A fájl régi .xls formátumú. Mentsd el Excelben .xlsx formátumban, és töltsd fel újra.");
        if (fej.Length < 4 || fej[0] != 0x50 || fej[1] != 0x4B)
            throw UzletiHiba.Hibas("A fájl nem érvényes .xlsx munkafüzet.");

        try
        {
            return new XLWorkbook(memoria);
        }
        catch (Exception ex) when (ex is not UzletiHiba)
        {
            throw UzletiHiba.Hibas("A munkafüzet nem nyitható meg; ellenőrizd, hogy érvényes, nem jelszóval védett .xlsx fájl-e.");
        }
    }

    private static (IXLWorksheet Lap, int FejlecSor) MunkalapEsFejlec(XLWorkbook munkafuzet, string? nev)
    {
        if (nev is not null)
        {
            var lap = munkafuzet.Worksheets.FirstOrDefault(w => string.Equals(w.Name, nev, StringComparison.OrdinalIgnoreCase))
                ?? throw UzletiHiba.Hibas($"Nincs „{nev}” nevű munkalap. Munkalapok: {string.Join(", ", munkafuzet.Worksheets.Select(w => w.Name))}.");
            return (lap, FejlecSor(lap) ?? throw NincsFejlec());
        }

        foreach (var lap in munkafuzet.Worksheets)
            if (FejlecSor(lap) is { } sor)
                return (lap, sor);

        throw NincsFejlec();
    }

    private static UzletiHiba NincsFejlec() => UzletiHiba.Hibas(
        "A munkafüzetben nem található fejlécsor. Az első sorok egyikében szerepeljen legalább a „SAP szám” és a „Megnevezés” oszlop.");

    /// <summary>Az első 20 sor közül az, amelyikben a legtöbb ismert oszlopnév szerepel (legalább kettő).</summary>
    private static int? FejlecSor(IXLWorksheet lap)
    {
        var elso = lap.FirstRowUsed()?.RowNumber();
        var utolsoOszlop = lap.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (elso is null || utolsoOszlop == 0)
            return null;

        int? legjobb = null;
        var legtobb = 1;
        for (var r = elso.Value; r < elso.Value + FejlecKeresesSorok; r++)
        {
            var felismert = Enumerable.Range(1, utolsoOszlop)
                .Select(c => ImportErtekek.FelismertMezo(lap.Cell(r, c).GetFormattedString()))
                .Where(m => m is not null)
                .Distinct()
                .Count();
            if (felismert > legtobb)
                (legjobb, legtobb) = (r, felismert);
        }
        return legjobb;
    }

    private static (List<ImportOszlop>, List<string>) OszlopHozzarendeles(
        IXLWorksheet lap, int fejlecSor, IReadOnlyDictionary<ImportMezo, string>? megadott)
    {
        var utolsoOszlop = lap.LastColumnUsed()?.ColumnNumber() ?? 0;
        var fejlecek = Enumerable.Range(1, utolsoOszlop)
            .Select(c => (Oszlop: c, Szoveg: lap.Cell(fejlecSor, c).GetFormattedString().Trim()))
            .Where(f => f.Szoveg.Length > 0)
            .ToList();

        var oszlopok = new List<ImportOszlop>();
        foreach (var (oszlop, szoveg) in fejlecek)
            if (ImportErtekek.FelismertMezo(szoveg) is { } m && oszlopok.All(o => o.Mezo != m))
                oszlopok.Add(new ImportOszlop(m, oszlop, szoveg));

        foreach (var (mezo, fejlec) in megadott ?? new Dictionary<ImportMezo, string>())
        {
            var talalat = fejlecek.FirstOrDefault(f => ImportErtekek.NormalFejlec(f.Szoveg) == ImportErtekek.NormalFejlec(fejlec));
            if (talalat.Szoveg is null)
                throw UzletiHiba.Hibas($"A(z) „{fejlec}” oszlop nem található a fejlécben ({ImportMezok.Nev(mezo)}).");
            oszlopok.RemoveAll(o => o.Mezo == mezo || o.Oszlop == talalat.Oszlop);
            oszlopok.Add(new ImportOszlop(mezo, talalat.Oszlop, talalat.Szoveg));
        }

        var ismeretlen = fejlecek.Where(f => oszlopok.All(o => o.Oszlop != f.Oszlop)).Select(f => f.Szoveg).ToList();
        return (oszlopok.OrderBy(o => o.Oszlop).ToList(), ismeretlen);
    }

    private static ExcelErtek? CellaErtek(IXLCell cella)
    {
        var ertek = cella.Value;
        var szoveg = cella.GetFormattedString().Trim();
        double? szam = ertek.Type == XLDataType.Number ? ertek.GetNumber() : null;
        DateTime? datum = ertek.Type == XLDataType.DateTime ? ertek.GetDateTime() : null;
        if (szoveg.Length == 0 && szam is null && datum is null)
            return null;

        // Kódnál a vezető nullák számítanak: a formázott szöveget vesszük, ha az csak számjegy (pl. „000123”),
        // különben egész szám esetén a tudományos alak elkerülésére a teljes egész értéket.
        var kod = szoveg;
        if (szam is { } d && !szoveg.All(char.IsDigit) && d == Math.Floor(d) && Math.Abs(d) < 1e15)
            kod = ((long)d).ToString(CultureInfo.InvariantCulture);

        return new ExcelErtek(szoveg, szam, datum, kod);
    }
}
