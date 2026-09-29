using LeltarKezelo.Kozos.Leltar;

namespace LeltarKezelo.Szerver.Adat.Entitasok;

public class Helyiseg
{
    public long Id { get; set; }
    public string Epulet { get; set; } = "";
    public string? Emelet { get; set; }
    public string Szobaszam { get; set; } = "";
    public string? Megnevezes { get; set; }
    public string? Vonalkod { get; set; }
    public bool Aktiv { get; set; } = true;
}

/// <summary>Az eszköz helye egy időszakban; <see cref="ErvenyesIg"/> = null az aktuális hely (D-011).</summary>
public class Elhelyezes
{
    public long Id { get; set; }
    public long EszkozId { get; set; }
    public Eszkoz Eszkoz { get; set; } = null!;
    public long HelyisegId { get; set; }
    public Helyiseg Helyiseg { get; set; } = null!;
    public DateTime ErvenyesTol { get; set; }
    public DateTime? ErvenyesIg { get; set; }
    public ElhelyezesForras Forras { get; set; }
    public long? LeolvasasId { get; set; }
    public Leolvasas? Leolvasas { get; set; }
}

/// <summary>Akinek a nevén az eszköz szerepel; nem feltétlenül felhasználója a rendszernek.</summary>
public class FelelosSzemely
{
    public long Id { get; set; }
    public string Nev { get; set; } = "";
    public string? Azonosito { get; set; }
    public string? Email { get; set; }
    public string? SzervezetiEgyseg { get; set; }
    public long? FelhasznaloId { get; set; }
    public Felhasznalo? Felhasznalo { get; set; }
    public bool Aktiv { get; set; } = true;
}

public class FelelosHozzarendeles
{
    public long Id { get; set; }
    public long EszkozId { get; set; }
    public Eszkoz Eszkoz { get; set; } = null!;
    public long FelelosId { get; set; }
    public FelelosSzemely Felelos { get; set; } = null!;
    public DateTime ErvenyesTol { get; set; }
    public DateTime? ErvenyesIg { get; set; }
}

public class AllapotValtozas
{
    public long Id { get; set; }
    public long EszkozId { get; set; }
    public Eszkoz Eszkoz { get; set; } = null!;
    public long? RegiAllapotId { get; set; }
    public EszkozAllapot? RegiAllapot { get; set; }
    public long UjAllapotId { get; set; }
    public EszkozAllapot UjAllapot { get; set; } = null!;
    public string Indoklas { get; set; } = "";
    public string? Ugyiratszam { get; set; }
    public long FelhasznaloId { get; set; }
    public Felhasznalo Felhasznalo { get; set; } = null!;
    public DateTime Idopont { get; set; }
}

/// <summary>Fő eszköz és tartozéka: önálló leltári tétel vagy csak leírt tartozék (F-19).</summary>
public class Kiegeszito
{
    public long Id { get; set; }
    public long FoEszkozId { get; set; }
    public Eszkoz FoEszkoz { get; set; } = null!;
    public long? KiegeszitoEszkozId { get; set; }
    public Eszkoz? KiegeszitoEszkoz { get; set; }
    public string? Leiras { get; set; }
    public int Mennyiseg { get; set; } = 1;
    public DateTime ErvenyesTol { get; set; }
    public DateTime? ErvenyesIg { get; set; }
}
