namespace LeltarKezelo.Szerver.Adat.Entitasok;

public class Szerepkor
{
    public long Id { get; set; }
    public string Nev { get; set; } = "";

    public ICollection<Felhasznalo> Felhasznalok { get; set; } = [];
}
