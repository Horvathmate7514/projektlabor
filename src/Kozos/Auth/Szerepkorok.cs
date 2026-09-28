namespace LeltarKezelo.Kozos.Auth;

public static class Szerepkorok
{
    public const string Admin = "Admin";
    public const string Leltarfelelos = "Leltarfelelos";
    public const string Leltarozo = "Leltarozo";
    public const string Megtekinto = "Megtekinto";

    public static readonly IReadOnlyList<string> Mind = [Admin, Leltarfelelos, Leltarozo, Megtekinto];
}
