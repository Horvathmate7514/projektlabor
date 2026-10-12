using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LeltarKezelo.Kozos.Import;

namespace LeltarKezelo.Szerver.Szolgaltatasok.Import;

/// <summary>Az Excel-cellák értelmezése: fejlécek, kódok, számok, dátumok (03_alkalom/04_Excel_lekepezes.md, 1. és 4. pont).</summary>
public static partial class ImportErtekek
{
    private static readonly Dictionary<string, ImportMezo> Fejlecek = Fejlectabla();

    private static Dictionary<string, ImportMezo> Fejlectabla()
    {
        var t = new Dictionary<string, ImportMezo>();
        void Add(ImportMezo m, params string[] nevek) { foreach (var n in nevek) t[NormalFejlec(n)] = m; }

        Add(ImportMezo.SapSzam, "SAP szám", "SAP", "SAP azonosító", "Eszközszám", "SAP eszközszám");
        Add(ImportMezo.LeltariSzam1, "Leltári szám 1", "Leltári szám", "Lelt. szám 1", "Lelt. szám");
        Add(ImportMezo.LeltariSzam2, "Leltári szám 2", "Lelt. szám 2", "Régi leltári szám");
        Add(ImportMezo.GyartasiSzam, "Gyártási szám", "Gyári szám", "Sorozatszám", "Serial number", "S/N");
        Add(ImportMezo.Megnevezes, "Megnevezés", "Eszköz megnevezése", "Eszköznév", "Név");
        Add(ImportMezo.Mennyiseg, "Mennyiség", "Darab", "Darabszám", "Db");
        Add(ImportMezo.MennyisegiEgyseg, "Mennyiségi egység", "Egység", "ME");
        Add(ImportMezo.Ertek, "Érték", "Érték (Ft)", "Bekerülési érték", "Nyilvántartási érték", "Bruttó érték");
        Add(ImportMezo.BeszerzesDatuma, "Beszerzés dátuma", "Beszerzési dátum", "Beszerzés", "Aktiválás dátuma", "Aktiválás");
        Add(ImportMezo.Leltarkorzet, "Leltárkörzet", "Körzet", "Leltárkörzet kód", "Körzetkód");
        Add(ImportMezo.EszkozTipus, "Eszköztípus", "Típus", "Kategória");
        Add(ImportMezo.FelelosSzemely, "Felelős", "Felelős személy", "Felelős neve");
        Add(ImportMezo.Megjegyzes, "Megjegyzés", "Megj.");
        return t;
    }

    public static ImportMezo? FelismertMezo(string fejlec) =>
        Fejlecek.TryGetValue(NormalFejlec(fejlec), out var m) ? m : null;

    /// <summary>Ékezet nélküli, kisbetűs, csak betűket és számjegyeket tartalmazó alak: „SAP-szám ” → „sapszam”.</summary>
    public static string NormalFejlec(string szoveg)
    {
        var sb = new StringBuilder(szoveg.Length);
        foreach (var c in szoveg.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>Az adatbázis <c>ErtekNorm</c> oszlopával azonos normalizálás (D-012).</summary>
    public static string KodNorm(string kod) => kod.Trim().ToUpperInvariant();

    /// <summary>Szóközök levágása és a többszörös szóközök összevonása.</summary>
    public static string Tisztitott(string szoveg) => TobbSzokoz().Replace(szoveg.Trim(), " ");

    public static int? Egesz(ExcelErtek ertek)
    {
        if (ertek.Szam is { } d)
            return d == Math.Floor(d) && d is >= int.MinValue and <= int.MaxValue ? (int)d : null;
        var s = Regex.Replace(ertek.Szoveg, @"\s|db\.?$", "", RegexOptions.IgnoreCase);
        return int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var i) ? i : null;
    }

    /// <summary>Pénzösszeg számként vagy szövegként: „12 500”, „12.500,50”, „12,500.50”, „12 500 Ft”, „12.500,-Ft”.</summary>
    public static decimal? Penz(ExcelErtek ertek)
    {
        if (ertek.Szam is { } d)
            return Math.Round((decimal)d, 2);

        var s = ertek.Szoveg.Replace(" ", "").Replace(" ", "");
        s = Regex.Replace(s, @"(,-|\.-)?(Ft|HUF|forint)?\.?$", "", RegexOptions.IgnoreCase);
        if (s.Length == 0 || !Regex.IsMatch(s, @"^-?[\d.,]+$"))
            return null;

        var pont = s.LastIndexOf('.');
        var vesszo = s.LastIndexOf(',');
        if (pont >= 0 && vesszo >= 0)
            s = pont > vesszo ? s.Replace(",", "") : s.Replace(".", "").Replace(',', '.');
        else if (vesszo >= 0)
            // Magyar írásmód: egyetlen vessző = tizedesvessző; több vessző = angol ezreselválasztó.
            s = s.Count(c => c == ',') > 1 ? s.Replace(",", "") : s.Replace(',', '.');
        else if (pont >= 0 && EzresPont().IsMatch(s))
            s = s.Replace(".", "");

        return decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var e)
            ? Math.Round(e, 2)
            : null;
    }

    private static readonly string[] DatumFormatumok =
        ["yyyy.MM.dd.", "yyyy.MM.dd", "yyyy.M.d.", "yyyy.M.d", "yyyy-MM-dd", "yyyy/MM/dd", "dd.MM.yyyy", "d.M.yyyy"];

    public static DateOnly? Datum(ExcelErtek ertek)
    {
        if (ertek.Datum is { } dt)
            return DateOnly.FromDateTime(dt);
        if (ertek.Szam is { } d && d is > 20000 and < 80000)
            return DateOnly.FromDateTime(DateTime.FromOADate(d));

        var s = ertek.Szoveg.Replace(" ", "");
        return DateOnly.TryParseExact(s, DatumFormatumok, CultureInfo.InvariantCulture, DateTimeStyles.None, out var datum)
            ? datum
            : null;
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex TobbSzokoz();

    [GeneratedRegex(@"^-?\d{1,3}(\.\d{3})+$")]
    private static partial Regex EzresPont();
}
