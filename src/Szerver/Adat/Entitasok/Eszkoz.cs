namespace LeltarKezelo.Szerver.Adat.Entitasok;

public class Eszkoz
{
    public long Id { get; set; }
    public string Megnevezes { get; set; } = "";
    public long? EszkozTipusId { get; set; }
    public EszkozTipus? EszkozTipus { get; set; }
    public long LeltarkorzetId { get; set; }
    public Leltarkorzet Leltarkorzet { get; set; } = null!;
    public int ElvartMennyiseg { get; set; } = 1;
    public string? MennyisegiEgyseg { get; set; }
    public decimal? Ertek { get; set; }
    public DateOnly? BeszerzesDatuma { get; set; }
    public long EszkozAllapotId { get; set; }
    public EszkozAllapot EszkozAllapot { get; set; } = null!;
    public string? Megjegyzes { get; set; }
    public DateTime Letrehozva { get; set; }
    public long? LetrehoztaId { get; set; }
    public DateTime? Modositva { get; set; }
    public long? ModositottaId { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<EszkozKod> Kodok { get; set; } = [];
}
