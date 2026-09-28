namespace LeltarKezelo.Szerver.Adat.Entitasok;

public class Leltarkorzet
{
    public long Id { get; set; }
    public string Kod { get; set; } = "";
    public string Nev { get; set; } = "";
    public string? SzervezetiEgyseg { get; set; }
    public bool Aktiv { get; set; } = true;
}
