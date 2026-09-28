using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeltarKezelo.Szerver.Adat.Konfiguraciok;

public class EszkozKodKonfiguracio : IEntityTypeConfiguration<EszkozKod>
{
    public void Configure(EntityTypeBuilder<EszkozKod> builder)
    {
        builder.ToTable("EszkozKod");
        builder.Property(k => k.Ertek).HasMaxLength(64).IsRequired();
        builder.Property(k => k.ErtekNorm).HasMaxLength(64)
            .HasComputedColumnSql("UPPER(LTRIM(RTRIM([Ertek])))", stored: true);
        builder.Property(k => k.Aktiv).HasDefaultValue(true);

        builder.HasIndex(k => k.ErtekNorm).IsUnique().HasFilter("[Aktiv] = 1");
    }
}
