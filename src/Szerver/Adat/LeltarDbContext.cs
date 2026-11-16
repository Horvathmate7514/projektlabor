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

    /// <summary>A kérést végző felhasználó; az audit napló és az eszköz módosítási mezői ezt rögzítik.</summary>
    public long? AktualisFelhasznaloId { get; set; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var most = DateTime.UtcNow;
        var audit = AuditKeszito.Elokeszites(this, most);
        if (!audit.VanUjEntitas)
            return base.SaveChanges(acceptAllChangesOnSuccess);

        // Az új entitás azonosítója csak a mentés után ismert, ezért a naplósora második mentésben készül;
        // a két mentést tranzakció fogja össze, ha az adatbázis támogatja, és még nincs nyitva egy.
        using var tranzakcio = SajatTranzakcioKell ? Database.BeginTransaction() : null;
        var sorok = base.SaveChanges(acceptAllChangesOnSuccess);
        audit.UjakNaplozasa(this, most);
        base.SaveChanges(acceptAllChangesOnSuccess);
        tranzakcio?.Commit();
        return sorok;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var most = DateTime.UtcNow;
        var audit = AuditKeszito.Elokeszites(this, most);
        if (!audit.VanUjEntitas)
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        await using var tranzakcio = SajatTranzakcioKell ? await Database.BeginTransactionAsync(cancellationToken) : null;
        var sorok = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        audit.UjakNaplozasa(this, most);
        await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (tranzakcio is not null)
            await tranzakcio.CommitAsync(cancellationToken);
        return sorok;
    }

    private bool SajatTranzakcioKell => Database.IsRelational() && Database.CurrentTransaction is null;

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
