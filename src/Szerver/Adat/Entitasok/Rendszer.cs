using LeltarKezelo.Kozos.Leltar;

namespace LeltarKezelo.Szerver.Adat.Entitasok;

public class ImportFutas
{
    public long Id { get; set; }
    public string Fajlnev { get; set; } = "";
    public DateTime Idopont { get; set; }
    public long FelhasznaloId { get; set; }
    public Felhasznalo Felhasznalo { get; set; } = null!;
    public int SorokSzama { get; set; }
    public int Sikeres { get; set; }
    public int Hibas { get; set; }
    public ImportStatusz Statusz { get; set; }

    public ICollection<ImportHiba> Hibak { get; set; } = [];
}

public class ImportHiba
{
    public long Id { get; set; }
    public long ImportFutasId { get; set; }
    public ImportFutas ImportFutas { get; set; } = null!;
    public int Sor { get; set; }
    public string? Oszlop { get; set; }
    public string Uzenet { get; set; } = "";
    public string? Ertek { get; set; }
}

/// <summary>Technikai napló minden adatmódosításról; a kitöltése a 10. alkalom feladata.</summary>
public class AuditNaplo
{
    public long Id { get; set; }
    public string Entitas { get; set; } = "";
    public long EntitasId { get; set; }
    public string Muvelet { get; set; } = "";
    public string? RegiErtekJson { get; set; }
    public string? UjErtekJson { get; set; }
    public long? FelhasznaloId { get; set; }
    public Felhasznalo? Felhasznalo { get; set; }
    public DateTime Idopont { get; set; }
}
