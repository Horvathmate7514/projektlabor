namespace LeltarKezelo.Kozos;

public static class ApiUtvonalak
{
    public const string Allapot = "api/allapot";
    public const string Bejelentkezes = "api/auth/login";
    public const string Sajat = "api/auth/en";
    public const string Eszkozok = "api/eszkozok";
    public const string Leltarkorzetek = "api/leltarkorzetek";
    public const string KodTipusok = "api/kodtipusok";
    public const string EszkozTipusok = "api/eszkoztipusok";
    public const string EszkozAllapotok = "api/eszkozallapotok";
    public const string Helyisegek = "api/helyisegek";
    public const string Felelosok = "api/felelosok";
    public const string LeltarIdoszakok = "api/leltaridoszakok";
    public const string Leolvasasok = "api/leolvasasok";
    public const string Importok = "api/importok";
    public const string ImportProbafuttatas = "api/importok/probafuttatas";
    public const string AuditNaplo = "api/auditnaplo";
    public const string HelyisegKezeles = "api/helyisegek/kezeles";
    public const string EszkozTipusKezeles = "api/eszkoztipusok/kezeles";
    public const string FelelosKezeles = "api/felelosok/kezeles";

    public static string Nyitas(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/nyitas";
    public static string Lezaras(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/lezaras";
    public static string IdoszakLeolvasasai(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/leolvasasok";
    public static string Demokod(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/demokod";
    public static string Osszesites(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/osszesites";
    public static string Tetelek(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/tetelek";
    public static string ElvartTetelek(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/elvart-tetelek";
    public static string Sztorno(long leolvasasId) => $"{Leolvasasok}/{leolvasasId}/sztorno";
    public static string Import(long futasId) => $"{Importok}/{futasId}";
    public static string ImportHibakExcel(long futasId) => $"{Importok}/{futasId}/hibak.xlsx";
    public static string Helyiseg(long id) => $"{Helyisegek}/{id}";
    public static string EszkozTipus(long id) => $"{EszkozTipusok}/{id}";
    public static string Felelos(long id) => $"{Felelosok}/{id}";
    public static string EszkozHozzarendelesek(long eszkozId) => $"{Eszkozok}/{eszkozId}/hozzarendelesek";
    public static string EszkozHely(long eszkozId) => $"{Eszkozok}/{eszkozId}/hely";
    public static string EszkozFelelos(long eszkozId) => $"{Eszkozok}/{eszkozId}/felelos";
    public static string EszkozTipusa(long eszkozId) => $"{Eszkozok}/{eszkozId}/tipus";
    public static string EszkozAllapot(long eszkozId) => $"{Eszkozok}/{eszkozId}/allapot";
    public static string EszkozTortenet(long eszkozId) => $"{Eszkozok}/{eszkozId}/tortenet";
    public static string EszkozPillanat(long eszkozId) => $"{Eszkozok}/{eszkozId}/pillanat";
    public static string EszkozKiegeszitoi(long eszkozId) => $"{Eszkozok}/{eszkozId}/kiegeszitok";
    public static string KiegeszitoLezaras(long kiegeszitoId) => $"api/kiegeszitok/{kiegeszitoId}/lezaras";
    public static string LeolvasasKiegeszitoi(long leolvasasId) => $"{Leolvasasok}/{leolvasasId}/kiegeszitok";
    public static string LeolvasasKiegeszitoje(long leolvasasId, long kiegeszitoId) =>
        $"{Leolvasasok}/{leolvasasId}/kiegeszitok/{kiegeszitoId}";
}
