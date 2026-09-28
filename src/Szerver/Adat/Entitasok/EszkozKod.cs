namespace LeltarKezelo.Szerver.Adat.Entitasok;

public class EszkozKod
{
    public long Id { get; set; }
    public long EszkozId { get; set; }
    public Eszkoz Eszkoz { get; set; } = null!;
    public long KodTipusId { get; set; }
    public KodTipus KodTipus { get; set; } = null!;
    public string Ertek { get; set; } = "";
    public string ErtekNorm { get; set; } = "";
    public bool Aktiv { get; set; } = true;
}
