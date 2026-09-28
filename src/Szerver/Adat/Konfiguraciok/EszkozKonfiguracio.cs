using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeltarKezelo.Szerver.Adat.Konfiguraciok;

public class EszkozKonfiguracio : IEntityTypeConfiguration<Eszkoz>
{
    public void Configure(EntityTypeBuilder<Eszkoz> builder)
    {
        builder.ToTable("Eszkoz", t => t.HasCheckConstraint("CK_Eszkoz_ElvartMennyiseg", "[ElvartMennyiseg] > 0"));
        builder.Property(e => e.Megnevezes).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ElvartMennyiseg).HasDefaultValue(1);
        builder.Property(e => e.MennyisegiEgyseg).HasMaxLength(20);
        builder.Property(e => e.Megjegyzes).HasMaxLength(1000);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasMany(e => e.Kodok).WithOne(k => k.Eszkoz).HasForeignKey(k => k.EszkozId);

        builder.HasIndex(e => e.Megnevezes);
    }
}
