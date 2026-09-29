namespace LeltarKezelo.Szerver.Adat.Entitasok;

public class Felhasznalo
{
    public long Id { get; set; }
    public string Felhasznalonev { get; set; } = "";
    public string Nev { get; set; } = "";
    public string? Email { get; set; }
    public string JelszoHash { get; set; } = "";
    public string? SzervezetiEgyseg { get; set; }
    public bool Aktiv { get; set; } = true;
    public DateTime? UtolsoBelepes { get; set; }

    public ICollection<Szerepkor> Szerepkorok { get; set; } = [];

    /// <summary>A leltárkörzetek, amelyekben a felhasználó leltározhat (jogosultsági mátrix).</summary>
    public ICollection<Leltarkorzet> JogosultKorzetek { get; set; } = [];
}
