using System.Text.Encodings.Web;
using System.Text.Json;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Kozos.Naplo;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LeltarKezelo.Szerver.Adat;

/// <summary>
/// Az audit napló sorainak előállítása a mentés előtt álló változásokból (S01, D-046).
/// A törzs- és alapadatokat naplózza, valamint a kézi áthelyezést és a felelősváltást, mert ezeknél más nem
/// őrzi meg, ki rögzítette. A leolvasás, az állapotváltozás és az importnapló maga is eseménynapló,
/// a leltárból származó helyet pedig a leolvasás azonosítja, ezekről nem készül külön sor.
/// </summary>
internal sealed class AuditKeszito
{
    private static readonly Dictionary<Type, string[]> Naplozott = new()
    {
        [typeof(Eszkoz)] =
        [
            nameof(Eszkoz.Letrehozva), nameof(Eszkoz.LetrehoztaId), nameof(Eszkoz.Modositva),
            nameof(Eszkoz.ModositottaId), nameof(Eszkoz.RowVersion),
        ],
        [typeof(EszkozKod)] = [nameof(EszkozKod.ErtekNorm)],
        [typeof(EszkozTipus)] = [],
        [typeof(Helyiseg)] = [],
        [typeof(FelelosSzemely)] = [],
        [typeof(Leltarkorzet)] = [],
        [typeof(Kiegeszito)] = [],
        [typeof(Elhelyezes)] = [],
        [typeof(FelelosHozzarendeles)] = [],
        [typeof(LeltarIdoszak)] = [nameof(LeltarIdoszak.RowVersion)],
        [typeof(Felhasznalo)] = [nameof(Felhasznalo.JelszoHash), nameof(Felhasznalo.UtolsoBelepes)],
    };

    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly List<(EntityEntry Bejegyzes, string[] Kihagyott)> _ujak = [];

    public bool VanUjEntitas => _ujak.Count > 0;

    /// <summary>
    /// A módosítások naplósorai azonnal a kontextusba kerülnek, így egy mentésben íródnak az adattal.
    /// Az új entitások azonosítója csak a mentés után ismert, ezekről az <see cref="UjakNaplozasa"/> készít sort.
    /// </summary>
    public static AuditKeszito Elokeszites(LeltarDbContext db, DateTime most)
    {
        var keszito = new AuditKeszito();
        foreach (var bejegyzes in db.ChangeTracker.Entries().ToList())
        {
            if (!Naplozott.TryGetValue(bejegyzes.Metadata.ClrType, out var kihagyott))
                continue;
            if (bejegyzes.Entity is Elhelyezes { Forras: ElhelyezesForras.Leltar })
                continue;

            switch (bejegyzes.State)
            {
                case EntityState.Added when !UjEszkozKodja(db, bejegyzes):
                    keszito._ujak.Add((bejegyzes, kihagyott));
                    break;

                case EntityState.Modified:
                    var valtozott = bejegyzes.Properties
                        .Where(p => p.IsModified && !kihagyott.Contains(p.Metadata.Name) && !Equals(p.OriginalValue, p.CurrentValue))
                        .ToList();
                    if (valtozott.Count == 0)
                        break;
                    if (bejegyzes.Entity is Eszkoz eszkoz)
                        ModositasJelolese(db, bejegyzes, eszkoz, most);
                    db.AuditNaplo.Add(Sor(db, bejegyzes, NaploMuveletek.Modositas, most,
                        Sorosit(valtozott.ToDictionary(p => p.Metadata.Name, p => Ertek(p.OriginalValue))),
                        Sorosit(valtozott.ToDictionary(p => p.Metadata.Name, p => Ertek(p.CurrentValue)))));
                    break;

                case EntityState.Deleted:
                    db.AuditNaplo.Add(Sor(db, bejegyzes, NaploMuveletek.Torles, most,
                        Sorosit(bejegyzes.Properties
                            .Where(p => !p.Metadata.IsPrimaryKey() && !kihagyott.Contains(p.Metadata.Name) && p.OriginalValue is not null)
                            .ToDictionary(p => p.Metadata.Name, p => Ertek(p.OriginalValue))),
                        null));
                    break;
            }
        }
        return keszito;
    }

    /// <summary>A mentés után hívandó: ekkor már ismert az új entitás azonosítója és a külső kulcsai.</summary>
    public void UjakNaplozasa(LeltarDbContext db, DateTime most)
    {
        foreach (var (bejegyzes, kihagyott) in _ujak)
            db.AuditNaplo.Add(Sor(db, bejegyzes, NaploMuveletek.Letrehozas, most, null, Sorosit(UjErtekek(bejegyzes, kihagyott))));
        _ujak.Clear();
    }

    /// <summary>Az új eszköz kódjai az eszköz naplósorában szerepelnek, nem külön sorokban.</summary>
    private static bool UjEszkozKodja(LeltarDbContext db, EntityEntry bejegyzes) =>
        bejegyzes.Entity is EszkozKod { Eszkoz: { } eszkoz } && db.Entry(eszkoz).State == EntityState.Added;

    private static Dictionary<string, object?> UjErtekek(EntityEntry bejegyzes, string[] kihagyott)
    {
        var ertekek = bejegyzes.Properties
            .Where(p => !p.Metadata.IsPrimaryKey() && !kihagyott.Contains(p.Metadata.Name) && p.CurrentValue is not null)
            .ToDictionary(p => p.Metadata.Name, p => Ertek(p.CurrentValue));
        if (bejegyzes.Entity is Eszkoz { Kodok.Count: > 0 } eszkoz)
            ertekek["Kodok"] = eszkoz.Kodok.Select(k => new { k.KodTipusId, k.Ertek }).ToList();
        return ertekek;
    }

    private static void ModositasJelolese(LeltarDbContext db, EntityEntry bejegyzes, Eszkoz eszkoz, DateTime most)
    {
        if (!bejegyzes.Property(nameof(Eszkoz.Modositva)).IsModified)
            eszkoz.Modositva = most;
        if (!bejegyzes.Property(nameof(Eszkoz.ModositottaId)).IsModified && db.AktualisFelhasznaloId is { } felhasznaloId)
            eszkoz.ModositottaId = felhasznaloId;
    }

    private static AuditNaplo Sor(LeltarDbContext db, EntityEntry bejegyzes, string muvelet, DateTime most, string? regi, string? uj) => new()
    {
        Entitas = bejegyzes.Metadata.ClrType.Name,
        EntitasId = Convert.ToInt64(bejegyzes.Property("Id").CurrentValue),
        Muvelet = muvelet,
        RegiErtekJson = regi,
        UjErtekJson = uj,
        FelhasznaloId = db.AktualisFelhasznaloId,
        Idopont = most,
    };

    private static object? Ertek(object? ertek) => ertek is Enum ? ertek.ToString() : ertek;

    private static string Sorosit(object ertekek) => JsonSerializer.Serialize(ertekek, Json);
}
