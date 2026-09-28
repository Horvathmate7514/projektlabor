namespace LeltarKezelo.Szerver.Adat.Entitasok;

public class EszkozTipus
{
    public long Id { get; set; }
    public string Nev { get; set; } = "";
    public long? SzuloId { get; set; }
    public EszkozTipus? Szulo { get; set; }
    public bool Aktiv { get; set; } = true;
}
