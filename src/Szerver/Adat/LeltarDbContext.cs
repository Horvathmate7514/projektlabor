using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.EntityFrameworkCore;

namespace LeltarKezelo.Szerver.Adat;

public class LeltarDbContext(DbContextOptions<LeltarDbContext> options) : DbContext(options)
{
    public DbSet<Eszkoz> Eszkozok => Set<Eszkoz>();
    public DbSet<EszkozKod> EszkozKodok => Set<EszkozKod>();
    public DbSet<KodTipus> KodTipusok => Set<KodTipus>();
    public DbSet<EszkozTipus> EszkozTipusok => Set<EszkozTipus>();
    public DbSet<Leltarkorzet> Leltarkorzetek => Set<Leltarkorzet>();
    public DbSet<EszkozAllapot> EszkozAllapotok => Set<EszkozAllapot>();
    public DbSet<Felhasznalo> Felhasznalok => Set<Felhasznalo>();
    public DbSet<Szerepkor> Szerepkorok => Set<Szerepkor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeltarDbContext).Assembly);

        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime2(3)");
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
