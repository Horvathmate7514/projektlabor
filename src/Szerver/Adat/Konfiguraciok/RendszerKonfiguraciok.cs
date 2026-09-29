using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeltarKezelo.Szerver.Adat.Konfiguraciok;

public class ImportFutasKonfiguracio : IEntityTypeConfiguration<ImportFutas>
{
    public void Configure(EntityTypeBuilder<ImportFutas> builder)
    {
        builder.ToTable("ImportFutas", t =>
            t.HasCheckConstraint("CK_ImportFutas_Statusz", Felsorolas.Ellenorzes<ImportStatusz>("Statusz")));
        builder.Property(i => i.Fajlnev).HasMaxLength(260).IsRequired();
        builder.Property(i => i.Statusz).SzovegkentTarolva();

        builder.HasMany(i => i.Hibak).WithOne(h => h.ImportFutas).HasForeignKey(h => h.ImportFutasId);
        builder.HasIndex(i => i.Idopont);
    }
}

public class ImportHibaKonfiguracio : IEntityTypeConfiguration<ImportHiba>
{
    public void Configure(EntityTypeBuilder<ImportHiba> builder)
    {
        builder.ToTable("ImportHiba");
        builder.Property(h => h.Oszlop).HasMaxLength(100);
        builder.Property(h => h.Uzenet).HasMaxLength(500).IsRequired();
        builder.Property(h => h.Ertek).HasMaxLength(500);
    }
}

public class AuditNaploKonfiguracio : IEntityTypeConfiguration<AuditNaplo>
{
    public void Configure(EntityTypeBuilder<AuditNaplo> builder)
    {
        builder.ToTable("AuditNaplo");
        builder.Property(a => a.Entitas).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Muvelet).HasMaxLength(20).IsUnicode(false).IsRequired();

        builder.HasIndex(a => new { a.Entitas, a.EntitasId });
        builder.HasIndex(a => a.Idopont);
    }
}
