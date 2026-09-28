namespace LeltarKezelo.Szerver.Adat.Entitasok;

public class KodTipus
{
    public long Id { get; set; }
    public string Nev { get; set; } = "";
    public bool VonalkodkentHasznalhato { get; set; } = true;
    public bool Aktiv { get; set; } = true;
}
