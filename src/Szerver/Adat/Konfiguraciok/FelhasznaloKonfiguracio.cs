using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeltarKezelo.Szerver.Adat.Konfiguraciok;

public class FelhasznaloKonfiguracio : IEntityTypeConfiguration<Felhasznalo>
{
    public void Configure(EntityTypeBuilder<Felhasznalo> builder)
    {
        builder.ToTable("Felhasznalo");
        builder.Property(f => f.Felhasznalonev).HasMaxLength(50).IsRequired();
        builder.Property(f => f.Nev).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Email).HasMaxLength(200);
        builder.Property(f => f.JelszoHash).HasMaxLength(200).IsRequired();
        builder.Property(f => f.SzervezetiEgyseg).HasMaxLength(200);
        builder.HasIndex(f => f.Felhasznalonev).IsUnique();

        builder.HasMany(f => f.Szerepkorok).WithMany(s => s.Felhasznalok)
            .UsingEntity("FelhasznaloSzerepkor");
    }
}

public class SzerepkorKonfiguracio : IEntityTypeConfiguration<Szerepkor>
{
    public void Configure(EntityTypeBuilder<Szerepkor> builder)
    {
        builder.ToTable("Szerepkor");
        builder.Property(s => s.Nev).HasMaxLength(50).IsRequired();
        builder.HasIndex(s => s.Nev).IsUnique();
    }
}
