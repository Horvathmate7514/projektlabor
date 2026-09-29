using LeltarKezelo.Kozos.Leltar;

namespace LeltarKezelo.Szerver.Adat.Entitasok;

public class LeltarIdoszak
{
    public long Id { get; set; }
    public string Megnevezes { get; set; } = "";
    public LeltarIdoszakTipus Tipus { get; set; }
    public DateOnly Kezdete { get; set; }
    public DateOnly? Vege { get; set; }
    public LeltarIdoszakStatusz Statusz { get; set; }
    public DateTime? Lezarva { get; set; }
    public long? LezartaId { get; set; }
    public Felhasznalo? Lezarta { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<ElvartTetel> ElvartTetelek { get; set; } = [];
}

/// <summary>A leltáridőszak nyitásakor rögzített elvárt állomány egy sora (D-003).</summary>
public class ElvartTetel
{
    public long Id { get; set; }
    public long LeltarIdoszakId { get; set; }
    public LeltarIdoszak LeltarIdoszak { get; set; } = null!;
    public long EszkozId { get; set; }
    public Eszkoz Eszkoz { get; set; } = null!;
    public long LeltarkorzetId { get; set; }
    public Leltarkorzet Leltarkorzet { get; set; } = null!;
    public int ElvartMennyiseg { get; set; } = 1;
    public ElvartTetelFelvitelOka FelvitelOka { get; set; }
}

/// <summary>Egy beolvasás eseménye; nem törölhető, csak sztornózható (D-002).</summary>
public class Leolvasas
{
    public long Id { get; set; }
    public long LeltarIdoszakId { get; set; }
    public LeltarIdoszak LeltarIdoszak { get; set; } = null!;
    public string BeolvasottKod { get; set; } = "";
    public long? EszkozKodId { get; set; }
    public EszkozKod? EszkozKod { get; set; }
    public long? EszkozId { get; set; }
    public Eszkoz? Eszkoz { get; set; }
    public long AktivKorzetId { get; set; }
    public Leltarkorzet AktivKorzet { get; set; } = null!;
    public long? HelyisegId { get; set; }
    public Helyiseg? Helyiseg { get; set; }
    public int Mennyiseg { get; set; } = 1;
    public Minosites Minosites { get; set; }
    public BeviteliMod BeviteliMod { get; set; }
    public long FelhasznaloId { get; set; }
    public Felhasznalo Felhasznalo { get; set; } = null!;
    public DateTime Idopont { get; set; }
    public bool Sztornozva { get; set; }
    public string? SztornoIndoklas { get; set; }
    public DateTime? SztornoIdopont { get; set; }
    public long? SztornoztaId { get; set; }
    public Felhasznalo? Sztornozta { get; set; }
}

public class KiegeszitoEllenorzes
{
    public long Id { get; set; }
    public long LeolvasasId { get; set; }
    public Leolvasas Leolvasas { get; set; } = null!;
    public long KiegeszitoId { get; set; }
    public Kiegeszito Kiegeszito { get; set; } = null!;
    public KiegeszitoEllenorzesMod Mod { get; set; }
}
