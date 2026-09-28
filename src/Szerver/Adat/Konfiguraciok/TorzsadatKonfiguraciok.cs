using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeltarKezelo.Szerver.Adat.Konfiguraciok;

public class KodTipusKonfiguracio : IEntityTypeConfiguration<KodTipus>
{
    public void Configure(EntityTypeBuilder<KodTipus> builder)
    {
        builder.ToTable("KodTipus");
        builder.Property(t => t.Nev).HasMaxLength(50).IsRequired();
        builder.HasIndex(t => t.Nev).IsUnique();
    }
}

public class EszkozTipusKonfiguracio : IEntityTypeConfiguration<EszkozTipus>
{
    public void Configure(EntityTypeBuilder<EszkozTipus> builder)
    {
        builder.ToTable("EszkozTipus");
        builder.Property(t => t.Nev).HasMaxLength(100).IsRequired();
        builder.HasOne(t => t.Szulo).WithMany().HasForeignKey(t => t.SzuloId);
    }
}

public class LeltarkorzetKonfiguracio : IEntityTypeConfiguration<Leltarkorzet>
{
    public void Configure(EntityTypeBuilder<Leltarkorzet> builder)
    {
        builder.ToTable("Leltarkorzet");
        builder.Property(k => k.Kod).HasMaxLength(20).IsRequired();
        builder.Property(k => k.Nev).HasMaxLength(200).IsRequired();
        builder.Property(k => k.SzervezetiEgyseg).HasMaxLength(200);
        builder.HasIndex(k => k.Kod).IsUnique();
    }
}

public class EszkozAllapotKonfiguracio : IEntityTypeConfiguration<EszkozAllapot>
{
    public void Configure(EntityTypeBuilder<EszkozAllapot> builder)
    {
        builder.ToTable("EszkozAllapot");
        builder.Property(a => a.Nev).HasMaxLength(50).IsRequired();
        builder.HasIndex(a => a.Nev).IsUnique();
    }
}
