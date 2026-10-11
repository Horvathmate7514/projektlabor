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

    public static string Nyitas(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/nyitas";
    public static string Lezaras(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/lezaras";
    public static string IdoszakLeolvasasai(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/leolvasasok";
    public static string Demokod(long idoszakId) => $"{LeltarIdoszakok}/{idoszakId}/demokod";
    public static string Sztorno(long leolvasasId) => $"{Leolvasasok}/{leolvasasId}/sztorno";
}
