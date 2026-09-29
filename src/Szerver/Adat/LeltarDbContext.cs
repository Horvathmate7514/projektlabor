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
    public DbSet<Kiegeszito> Kiegeszitok => Set<Kiegeszito>();
    public DbSet<Helyiseg> Helyisegek => Set<Helyiseg>();
    public DbSet<Elhelyezes> Elhelyezesek => Set<Elhelyezes>();
    public DbSet<FelelosSzemely> FelelosSzemelyek => Set<FelelosSzemely>();
    public DbSet<FelelosHozzarendeles> FelelosHozzarendelesek => Set<FelelosHozzarendeles>();
    public DbSet<AllapotValtozas> AllapotValtozasok => Set<AllapotValtozas>();
    public DbSet<LeltarIdoszak> LeltarIdoszakok => Set<LeltarIdoszak>();
    public DbSet<ElvartTetel> ElvartTetelek => Set<ElvartTetel>();
    public DbSet<Leolvasas> Leolvasasok => Set<Leolvasas>();
    public DbSet<KiegeszitoEllenorzes> KiegeszitoEllenorzesek => Set<KiegeszitoEllenorzes>();
    public DbSet<ImportFutas> ImportFutasok => Set<ImportFutas>();
    public DbSet<ImportHiba> ImportHibak => Set<ImportHiba>();
    public DbSet<AuditNaplo> AuditNaplo => Set<AuditNaplo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeltarDbContext).Assembly);
        KezdetiAdatok.Feltoltes(modelBuilder);

        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime2(3)");
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
