using LeltarKezelo.Kozos.Auth;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using LeltarKezelo.Szerver.Adat.Konfiguraciok;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LeltarKezelo.Szerver.Tesztek;

/// <summary>
/// Az adatmodell szabályainak ellenőrzése (adatmodell v1, döntési napló D-001, D-011, D-012).
/// A modell felépítése nem nyit adatbázis-kapcsolatot, így a tesztek SQL Server nélkül is futnak.
/// </summary>
public class AdatmodellTesztek
{
    private static LeltarDbContext Kontextus() =>
        new(new DbContextOptionsBuilder<LeltarDbContext>()
            .UseSqlServer("Server=nem-letezo;Database=nem-letezo")
            .Options);

    private static IModel TervezesiModell()
    {
        using var db = Kontextus();
        return db.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void A_migraciok_naprakeszek()
    {
        using var db = Kontextus();
        var pillanatkep = db.GetService<IMigrationsAssembly>().ModelSnapshot?.Model;
        Assert.NotNull(pillanatkep);

        if (pillanatkep is IMutableModel valtoztathato)
            pillanatkep = valtoztathato.FinalizeModel();
        pillanatkep = db.GetService<IModelRuntimeInitializer>().Initialize(pillanatkep);

        var elteres = db.GetService<IMigrationsModelDiffer>().HasDifferences(
            pillanatkep.GetRelationalModel(),
            db.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        Assert.False(elteres, "A modell eltér az utolsó migrációtól: futtasd a 'dotnet ef migrations add <nev>' parancsot.");
    }

    [Fact]
    public void Nincs_kaszkadolt_torles()
    {
        var nemRestrict = TervezesiModell().GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => fk.DeleteBehavior != DeleteBehavior.Restrict)
            .Select(fk => $"{fk.DeclaringEntityType.DisplayName()} → {fk.PrincipalEntityType.DisplayName()}")
            .ToList();

        Assert.Empty(nemRestrict);
    }

    [Fact]
    public void Az_aktiv_kodok_normalizalt_erteke_egyedi()
    {
        var kod = TervezesiModell().FindEntityType(typeof(EszkozKod))!;

        var norm = kod.FindProperty(nameof(EszkozKod.ErtekNorm))!;
        Assert.Equal("UPPER(LTRIM(RTRIM([Ertek])))", norm.GetComputedColumnSql());
        Assert.True(norm.GetIsStored());

        var index = kod.GetIndexes().Single(i => i.Properties.Single().Name == nameof(EszkozKod.ErtekNorm));
        Assert.True(index.IsUnique);
        Assert.Equal("[Aktiv] = 1", index.GetFilter());
    }

    [Theory]
    [InlineData(typeof(Elhelyezes))]
    [InlineData(typeof(FelelosHozzarendeles))]
    public void Egy_eszkoznek_egyszerre_egy_aktualis_sora_lehet(Type entitas)
    {
        var tipus = TervezesiModell().FindEntityType(entitas)!;

        var index = tipus.GetIndexes().Single(i => i.GetFilter() == "[ErvenyesIg] IS NULL");
        Assert.True(index.IsUnique);
        Assert.Equal("EszkozId", index.Properties.Single().Name);
    }

    [Fact]
    public void A_leolvasas_osszehasonlito_indexe_tartalmazza_a_szamolt_oszlopokat()
    {
        var index = TervezesiModell().FindEntityType(typeof(Leolvasas))!.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(["LeltarIdoszakId", "EszkozId"]));

        Assert.Equal(["Mennyiseg", "Minosites", "Sztornozva"], index.GetIncludeProperties()!);
    }

    [Theory]
    [InlineData(typeof(Leolvasas), "CK_Leolvasas_Minosites")]
    [InlineData(typeof(Leolvasas), "CK_Leolvasas_Sztorno")]
    [InlineData(typeof(Leolvasas), "CK_Leolvasas_Eszkoz")]
    [InlineData(typeof(LeltarIdoszak), "CK_LeltarIdoszak_Lezaras")]
    [InlineData(typeof(Elhelyezes), "CK_Elhelyezes_Ervenyesseg")]
    [InlineData(typeof(Kiegeszito), "CK_Kiegeszito_NemOnmaga")]
    [InlineData(typeof(ElvartTetel), "CK_ElvartTetel_ElvartMennyiseg")]
    public void A_megszoritas_letezik(Type entitas, string nev)
    {
        var megszoritasok = TervezesiModell().FindEntityType(entitas)!.GetCheckConstraints().Select(c => c.Name);

        Assert.Contains(nev, megszoritasok);
    }

    [Fact]
    public void A_minosites_megszoritasa_minden_erteket_enged()
    {
        var sql = TervezesiModell().FindEntityType(typeof(Leolvasas))!.GetCheckConstraints()
            .Single(c => c.Name == "CK_Leolvasas_Minosites").Sql;

        Assert.All(Enum.GetValues<Minosites>(), m => Assert.Contains($"'{Felsorolas.AdatbazisErtek(m)}'", sql));
    }

    [Theory]
    [InlineData(Minosites.Ok, "OK")]
    [InlineData(Minosites.MasKorzet, "MAS_KORZET")]
    [InlineData(Minosites.IsmeretlenKod, "ISMERETLEN_KOD")]
    [InlineData(Minosites.NemAktiv, "NEM_AKTIV")]
    public void A_felsorolas_nagybetus_aláhúzásos_szovegkent_tarolodik(Minosites ertek, string vart)
    {
        Assert.Equal(vart, Felsorolas.AdatbazisErtek(ertek));
        Assert.Equal(ertek, Felsorolas.Beolvas<Minosites>(vart));
    }

    [Fact]
    public void Minden_felsorolas_oda_vissza_alakithato()
    {
        void Ellenoriz<T>() where T : struct, Enum =>
            Assert.All(Enum.GetValues<T>(), e => Assert.Equal(e, Felsorolas.Beolvas<T>(Felsorolas.AdatbazisErtek(e))));

        Ellenoriz<Minosites>();
        Ellenoriz<BeviteliMod>();
        Ellenoriz<LeltarIdoszakTipus>();
        Ellenoriz<LeltarIdoszakStatusz>();
        Ellenoriz<ElvartTetelFelvitelOka>();
        Ellenoriz<ElhelyezesForras>();
        Ellenoriz<KiegeszitoEllenorzesMod>();
        Ellenoriz<ImportStatusz>();
    }

    [Fact]
    public void A_kezdeti_szerepkorok_megegyeznek_a_kozos_projekt_szerepkoreivel()
    {
        var szerepkorok = TervezesiModell().FindEntityType(typeof(Szerepkor))!.GetSeedData()
            .Select(s => (string)s[nameof(Szerepkor.Nev)]!).Order();

        Assert.Equal(Szerepkorok.Mind.Order(), szerepkorok);
    }

    [Fact]
    public void A_kezdeti_eszkozallapotok_kozott_van_aktiv_es_megszunt()
    {
        var allapotok = TervezesiModell().FindEntityType(typeof(EszkozAllapot))!.GetSeedData().ToList();

        Assert.Contains(allapotok, a => (string)a[nameof(EszkozAllapot.Nev)]! == "aktív" && !(bool)a[nameof(EszkozAllapot.Megszunt)]!);
        Assert.Contains(allapotok, a => (string)a[nameof(EszkozAllapot.Nev)]! == "selejtezett" && (bool)a[nameof(EszkozAllapot.Megszunt)]!);
    }
}
