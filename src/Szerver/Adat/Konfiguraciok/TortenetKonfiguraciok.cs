using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeltarKezelo.Szerver.Adat.Konfiguraciok;

public class HelyisegKonfiguracio : IEntityTypeConfiguration<Helyiseg>
{
    public void Configure(EntityTypeBuilder<Helyiseg> builder)
    {
        builder.ToTable("Helyiseg");
        builder.Property(h => h.Epulet).HasMaxLength(50).IsRequired();
        builder.Property(h => h.Emelet).HasMaxLength(20);
        builder.Property(h => h.Szobaszam).HasMaxLength(20).IsRequired();
        builder.Property(h => h.Megnevezes).HasMaxLength(200);
        builder.Property(h => h.Vonalkod).HasMaxLength(64);
        builder.Property(h => h.Aktiv).HasDefaultValue(true);

        builder.HasIndex(h => new { h.Epulet, h.Szobaszam }).IsUnique();                        // F-22
        builder.HasIndex(h => h.Vonalkod).IsUnique().HasFilter("[Vonalkod] IS NOT NULL");
    }
}

public class ElhelyezesKonfiguracio : IEntityTypeConfiguration<Elhelyezes>
{
    public void Configure(EntityTypeBuilder<Elhelyezes> builder)
    {
        builder.ToTable("Elhelyezes", t =>
        {
            t.HasCheckConstraint("CK_Elhelyezes_Ervenyesseg", "[ErvenyesIg] IS NULL OR [ErvenyesIg] > [ErvenyesTol]");
            t.HasCheckConstraint("CK_Elhelyezes_Forras", Felsorolas.Ellenorzes<ElhelyezesForras>("Forras"));
        });
        builder.Property(e => e.Forras).SzovegkentTarolva();

        // Egy eszköznek egyszerre legfeljebb egy aktuális helye lehet (D-011).
        builder.HasIndex(e => e.EszkozId).IsUnique().HasFilter("[ErvenyesIg] IS NULL")
            .HasDatabaseName("IX_Elhelyezes_EszkozId_Aktualis");
        builder.HasIndex(e => new { e.EszkozId, e.ErvenyesTol });
    }
}

public class FelelosSzemelyKonfiguracio : IEntityTypeConfiguration<FelelosSzemely>
{
    public void Configure(EntityTypeBuilder<FelelosSzemely> builder)
    {
        builder.ToTable("FelelosSzemely");
        builder.Property(f => f.Nev).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Azonosito).HasMaxLength(50);
        builder.Property(f => f.Email).HasMaxLength(200);
        builder.Property(f => f.SzervezetiEgyseg).HasMaxLength(200);
        builder.Property(f => f.Aktiv).HasDefaultValue(true);

        builder.HasIndex(f => f.Nev);
        builder.HasIndex(f => f.Azonosito).IsUnique().HasFilter("[Azonosito] IS NOT NULL");
        builder.HasIndex(f => f.FelhasznaloId).IsUnique().HasFilter("[FelhasznaloId] IS NOT NULL");
    }
}

public class FelelosHozzarendelesKonfiguracio : IEntityTypeConfiguration<FelelosHozzarendeles>
{
    public void Configure(EntityTypeBuilder<FelelosHozzarendeles> builder)
    {
        builder.ToTable("FelelosHozzarendeles", t =>
            t.HasCheckConstraint("CK_FelelosHozzarendeles_Ervenyesseg", "[ErvenyesIg] IS NULL OR [ErvenyesIg] > [ErvenyesTol]"));

        // Egy eszköznek egyszerre legfeljebb egy felelőse lehet (F-24).
        builder.HasIndex(f => f.EszkozId).IsUnique().HasFilter("[ErvenyesIg] IS NULL")
            .HasDatabaseName("IX_FelelosHozzarendeles_EszkozId_Aktualis");
        builder.HasIndex(f => new { f.EszkozId, f.ErvenyesTol });
        builder.HasIndex(f => f.FelelosId);
    }
}

public class AllapotValtozasKonfiguracio : IEntityTypeConfiguration<AllapotValtozas>
{
    public void Configure(EntityTypeBuilder<AllapotValtozas> builder)
    {
        builder.ToTable("AllapotValtozas");
        builder.Property(a => a.Indoklas).HasMaxLength(500).IsRequired();                      // F-26
        builder.Property(a => a.Ugyiratszam).HasMaxLength(50);

        builder.HasOne(a => a.RegiAllapot).WithMany().HasForeignKey(a => a.RegiAllapotId);
        builder.HasOne(a => a.UjAllapot).WithMany().HasForeignKey(a => a.UjAllapotId);
        builder.HasOne(a => a.Felhasznalo).WithMany().HasForeignKey(a => a.FelhasznaloId);

        builder.HasIndex(a => new { a.EszkozId, a.Idopont });
    }
}

public class KiegeszitoKonfiguracio : IEntityTypeConfiguration<Kiegeszito>
{
    public void Configure(EntityTypeBuilder<Kiegeszito> builder)
    {
        builder.ToTable("Kiegeszito", t =>
        {
            t.HasCheckConstraint("CK_Kiegeszito_Tartalom", "[KiegeszitoEszkozId] IS NOT NULL OR [Leiras] IS NOT NULL");
            t.HasCheckConstraint("CK_Kiegeszito_NemOnmaga", "[KiegeszitoEszkozId] IS NULL OR [KiegeszitoEszkozId] <> [FoEszkozId]");
            t.HasCheckConstraint("CK_Kiegeszito_Mennyiseg", "[Mennyiseg] > 0");
            t.HasCheckConstraint("CK_Kiegeszito_Ervenyesseg", "[ErvenyesIg] IS NULL OR [ErvenyesIg] > [ErvenyesTol]");
        });
        builder.Property(k => k.Leiras).HasMaxLength(200);
        builder.Property(k => k.Mennyiseg).HasDefaultValue(1);

        builder.HasOne(k => k.FoEszkoz).WithMany().HasForeignKey(k => k.FoEszkozId);
        builder.HasOne(k => k.KiegeszitoEszkoz).WithMany().HasForeignKey(k => k.KiegeszitoEszkozId);

        builder.HasIndex(k => k.FoEszkozId);
    }
}
