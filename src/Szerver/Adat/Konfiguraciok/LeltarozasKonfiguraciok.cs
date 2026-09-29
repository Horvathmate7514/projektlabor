using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeltarKezelo.Szerver.Adat.Konfiguraciok;

public class LeltarIdoszakKonfiguracio : IEntityTypeConfiguration<LeltarIdoszak>
{
    public void Configure(EntityTypeBuilder<LeltarIdoszak> builder)
    {
        builder.ToTable("LeltarIdoszak", t =>
        {
            t.HasCheckConstraint("CK_LeltarIdoszak_Tipus", Felsorolas.Ellenorzes<LeltarIdoszakTipus>("Tipus"));
            t.HasCheckConstraint("CK_LeltarIdoszak_Statusz", Felsorolas.Ellenorzes<LeltarIdoszakStatusz>("Statusz"));
            // Lezárt időszakhoz kötelező a lezárás időpontja (F-18).
            t.HasCheckConstraint("CK_LeltarIdoszak_Lezaras", "[Statusz] <> 'LEZART' OR [Lezarva] IS NOT NULL");
            t.HasCheckConstraint("CK_LeltarIdoszak_Idoszak", "[Vege] IS NULL OR [Vege] >= [Kezdete]");
        });
        builder.Property(i => i.Megnevezes).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Tipus).SzovegkentTarolva();
        builder.Property(i => i.Statusz).SzovegkentTarolva();
        builder.Property(i => i.RowVersion).IsRowVersion();

        builder.HasOne(i => i.Lezarta).WithMany().HasForeignKey(i => i.LezartaId);
        builder.HasMany(i => i.ElvartTetelek).WithOne(t => t.LeltarIdoszak).HasForeignKey(t => t.LeltarIdoszakId);
    }
}

public class ElvartTetelKonfiguracio : IEntityTypeConfiguration<ElvartTetel>
{
    public void Configure(EntityTypeBuilder<ElvartTetel> builder)
    {
        builder.ToTable("ElvartTetel", t =>
        {
            t.HasCheckConstraint("CK_ElvartTetel_ElvartMennyiseg", "[ElvartMennyiseg] > 0");
            t.HasCheckConstraint("CK_ElvartTetel_FelvitelOka", Felsorolas.Ellenorzes<ElvartTetelFelvitelOka>("FelvitelOka"));
        });
        builder.Property(t => t.FelvitelOka).SzovegkentTarolva();

        builder.HasIndex(t => new { t.LeltarIdoszakId, t.EszkozId }).IsUnique();
    }
}

public class LeolvasasKonfiguracio : IEntityTypeConfiguration<Leolvasas>
{
    public void Configure(EntityTypeBuilder<Leolvasas> builder)
    {
        builder.ToTable("Leolvasas", t =>
        {
            t.HasCheckConstraint("CK_Leolvasas_Mennyiseg", "[Mennyiseg] > 0");
            t.HasCheckConstraint("CK_Leolvasas_Minosites", Felsorolas.Ellenorzes<Minosites>("Minosites"));
            t.HasCheckConstraint("CK_Leolvasas_BeviteliMod", Felsorolas.Ellenorzes<BeviteliMod>("BeviteliMod"));
            // Sztornózott leolvasáshoz kötelező az indoklás (UC-08).
            t.HasCheckConstraint("CK_Leolvasas_Sztorno",
                "[Sztornozva] = 0 OR ([SztornoIndoklas] IS NOT NULL AND [SztornoIdopont] IS NOT NULL)");
            // Ismeretlen kódnál nincs eszköz, minden más minősítésnél van.
            t.HasCheckConstraint("CK_Leolvasas_Eszkoz",
                "([Minosites] = 'ISMERETLEN_KOD' AND [EszkozId] IS NULL) OR ([Minosites] <> 'ISMERETLEN_KOD' AND [EszkozId] IS NOT NULL)");
        });
        builder.Property(l => l.BeolvasottKod).HasMaxLength(64).IsRequired();
        builder.Property(l => l.Mennyiseg).HasDefaultValue(1);
        builder.Property(l => l.Minosites).SzovegkentTarolva();
        builder.Property(l => l.BeviteliMod).SzovegkentTarolva();
        builder.Property(l => l.SztornoIndoklas).HasMaxLength(500);

        builder.HasOne(l => l.AktivKorzet).WithMany().HasForeignKey(l => l.AktivKorzetId);
        builder.HasOne(l => l.Felhasznalo).WithMany().HasForeignKey(l => l.FelhasznaloId);
        builder.HasOne(l => l.Sztornozta).WithMany().HasForeignKey(l => l.SztornoztaId);

        // Az összehasonlítás és a darabszámok ezt az indexet használják.
        builder.HasIndex(l => new { l.LeltarIdoszakId, l.EszkozId })
            .IncludeProperties(l => new { l.Mennyiseg, l.Minosites, l.Sztornozva });
        builder.HasIndex(l => new { l.LeltarIdoszakId, l.Idopont });
        builder.HasIndex(l => l.BeolvasottKod);
    }
}

public class KiegeszitoEllenorzesKonfiguracio : IEntityTypeConfiguration<KiegeszitoEllenorzes>
{
    public void Configure(EntityTypeBuilder<KiegeszitoEllenorzes> builder)
    {
        builder.ToTable("KiegeszitoEllenorzes", t =>
            t.HasCheckConstraint("CK_KiegeszitoEllenorzes_Mod", Felsorolas.Ellenorzes<KiegeszitoEllenorzesMod>("Mod")));
        builder.Property(k => k.Mod).SzovegkentTarolva();

        builder.HasIndex(k => new { k.LeolvasasId, k.KiegeszitoId }).IsUnique();
    }
}
