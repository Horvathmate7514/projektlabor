using ClosedXML.Excel;
using LeltarKezelo.Kozos.Leltar;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

/// <summary>A leltáridőszak körzetösszesítője és szűrt tétellistája egy munkafüzetben (9. alkalom).</summary>
public static class OsszesitesExcel
{
    private static readonly XLColor FejlecSzin = XLColor.FromHtml("#D9E2E3");

    public static string AllapotNev(TetelAllapot allapot) => allapot switch
    {
        TetelAllapot.Hianyzik => "Hiányzik",
        TetelAllapot.Reszleges => "Részleges",
        TetelAllapot.Rendben => "Rendben",
        TetelAllapot.Tobblet => "Többlet",
        TetelAllapot.MasKorzetben => "Más körzetben",
        TetelAllapot.NemElvart => "Nem elvárt",
        _ => allapot.ToString(),
    };

    public static byte[] Keszites(
        LeltarIdoszakAdat idoszak, IReadOnlyList<KorzetOsszesites> korzetek, IReadOnlyList<OsszesitesTetel> tetelek, string szuroLeiras)
    {
        using var munkafuzet = new XLWorkbook();

        var osszesito = munkafuzet.AddWorksheet("Összesítés");
        Cim(osszesito, $"Leltárösszesítő – {idoszak.Megnevezes}",
            $"{idoszak.Kezdete:yyyy.MM.dd.} · {idoszak.Statusz} · {idoszak.ElvartTetelekSzama} elvárt tétel");
        string[] korzetFejlec = ["Körzet", "Megnevezés", "Elvárt tétel", "Rendben", "Részleges", "Hiányzik", "Többlet",
            "Más körzetben", "Más körzetből ide", "Nem elvárt", "Ismeretlen kód", "Készültség"];
        var r = Fejlec(osszesito, korzetFejlec);
        foreach (var k in korzetek)
        {
            object[] ertekek = [k.Kod, k.Nev, k.ElvartTetelek, k.Rendben, k.Reszleges, k.Hianyzik, k.Tobblet,
                k.MasKorzetben, k.MasKorzetbolIdeKerult, k.NemElvart, k.IsmeretlenKodok, k.Keszultseg];
            Sor(osszesito, r++, ertekek);
        }
        if (korzetek.Count > 0)
        {
            osszesito.Cell(r, 1).Value = "Összesen";
            for (var c = 3; c <= 11; c++)
                osszesito.Cell(r, c).FormulaA1 = $"SUM({osszesito.Cell(5, c).Address}:{osszesito.Cell(r - 1, c).Address})";
            osszesito.Row(r).Style.Font.SetBold();
            osszesito.Range(5, 12, r - 1, 12).Style.NumberFormat.Format = "0%";
        }
        Lezaras(osszesito, korzetFejlec.Length, r);

        var lap = munkafuzet.AddWorksheet("Tételek");
        Cim(lap, $"Tételek – {idoszak.Megnevezes}", $"Szűrés: {szuroLeiras} · {tetelek.Count} tétel");
        string[] fejlec = ["Eszköz", "Megnevezés", "Állapot", "Elvárt körzet", "Elvárt", "Beolvasott", "Saját körzetben",
            "Hiány", "Többlet", "Egység", "Talált körzetek", "Megszűnt"];
        r = Fejlec(lap, fejlec);
        foreach (var t in tetelek)
        {
            object[] ertekek = [t.EszkozId, t.Megnevezes, AllapotNev(t.Allapot), t.ElvartKorzetKod ?? "", t.ElvartMennyiseg,
                t.BeolvasottMennyiseg, t.SajatKorzetben, t.Hiany, t.Tobblet, t.MennyisegiEgyseg ?? "", string.Join(", ", t.TalaltKorzetek),
                t.Megszunt ? "igen" : ""];
            Sor(lap, r++, ertekek);
        }
        if (tetelek.Count == 0)
            lap.Cell(r++, 1).Value = "A szűrésnek egy tétel sem felel meg.";
        else
            lap.Range(4, 1, r - 1, fejlec.Length).SetAutoFilter();
        Lezaras(lap, fejlec.Length, r);

        using var kimenet = new MemoryStream();
        munkafuzet.SaveAs(kimenet);
        return kimenet.ToArray();
    }

    private static void Cim(IXLWorksheet lap, string cim, string alcim)
    {
        lap.Cell(1, 1).Value = cim;
        lap.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        lap.Cell(2, 1).Value = alcim;
    }

    private static int Fejlec(IXLWorksheet lap, string[] fejlec)
    {
        for (var c = 0; c < fejlec.Length; c++)
            lap.Cell(4, c + 1).Value = fejlec[c];
        lap.Range(4, 1, 4, fejlec.Length).Style.Font.SetBold().Fill.SetBackgroundColor(FejlecSzin);
        return 5;
    }

    private static void Sor(IXLWorksheet lap, int r, object[] ertekek)
    {
        for (var c = 0; c < ertekek.Length; c++)
            lap.Cell(r, c + 1).Value = XLCellValue.FromObject(ertekek[c]);
    }

    private static void Lezaras(IXLWorksheet lap, int oszlopok, int utolsoSor)
    {
        lap.SheetView.FreezeRows(4);
        lap.Columns(1, oszlopok).AdjustToContents(4, Math.Max(4, utolsoSor));
    }
}
