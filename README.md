# ProjektLab – Leltározó és leltárkezelő alkalmazás

Ez a repository a Projekt labor tárgy féléves munkájának anyagait és az alkalmazás forráskódját tartalmazza.
Az alkalmankénti anyagok a `0X_alkalom/` mappákban, a forráskód az `src/` és a `tests/` mappában található
(fordítás és futtatás: [Forráskód](#forráskód)).

## Mit vár az 1. alkalom?

A kiírás szerint az 1. alkalmon **még nincs program és nincs dokumentáció** – a cél, hogy a csoport:

1. megértse a feladatot és a féléves elvárásokat,
2. megalakuljon (3 fő),
3. kiossza az elsődleges szerepeket és a munkamegosztást,
4. tisztában legyen a Git-, dokumentációs, prezentációs és AI-használati szabályokkal,
5. az alkalom végén meg tudja mondani, **mit csinál a 2. alkalomig** (ott már 5 perces prezentáció és ~5 oldal dokumentáció kell!).

## A mappa tartalma

| Fájl | Mire való |
|---|---|
| [01_alkalom/01_Feladat_ertelmezese.md](01_alkalom/01_Feladat_ertelmezese.md) | A féléves kiírás értelmezése: mi a cél, mi számít az értékelésnél, hol lehet elbukni |
| [01_alkalom/02_Kovetelmeny_checklist.md](01_alkalom/02_Kovetelmeny_checklist.md) | A 19 pontos szakmai követelmény kipipálható listája, alapfogalmak, eldöntendő kérdések |
| [01_alkalom/03_Csoport_es_szerepek.md](01_alkalom/03_Csoport_es_szerepek.md) | Csoporttagok, szerepek, felelősségi mátrix, csapatmegállapodás |
| [01_alkalom/04_Felevi_utemterv.md](01_alkalom/04_Felevi_utemterv.md) | 14 alkalmas mérföldkőterv felelősökkel, oldalszám-célokkal |
| [01_alkalom/05_Teendok_a_2_alkalomig.md](01_alkalom/05_Teendok_a_2_alkalomig.md) | Konkrét, felelőshöz rendelt feladatlista a következő alkalomig |
| [01_alkalom/06_Kerdesek_az_oktatohoz.md](01_alkalom/06_Kerdesek_az_oktatohoz.md) | Nyitott kérdések, amiket érdemes már az 1. alkalmon feltenni |
| [repo_sablon/](repo_sablon/) | A GitHub repository kezdő szerkezete (README, .gitignore, döntési napló, AI-napló, közreműködési szabályok) |

### 2. alkalom – A probléma megismerése és ötletelés

| Fájl | Mire való |
|---|---|
| [02_alkalom/01_Hasonlo_rendszerek.md](02_alkalom/01_Hasonlo_rendszerek.md) | 5 hasonló rendszer vizsgálata, összehasonlítás, tanulságok |
| [02_alkalom/02_Funkciolista.md](02_alkalom/02_Funkciolista.md) | Priorizált funkciólista (MoSCoW), kiegészítő funkció jelöltek |
| [02_alkalom/03_Felhasznalok_es_hasznalati_esetek.md](02_alkalom/03_Felhasznalok_es_hasznalati_esetek.md) | Szerepkörök, use case-ek, részletes forgatókönyvek, beolvasási logika |
| [02_alkalom/04_Feltetelezesek.md](02_alkalom/04_Feltetelezesek.md) | 32 rögzített mérnöki feltételezés indoklással |
| [02_alkalom/05_Technologiai_osszevetes.md](02_alkalom/05_Technologiai_osszevetes.md) | WPF + ASP.NET Core választás indoklása, alternatívák, nyitott részdöntések |
| [02_alkalom/06_Architektura_vazlat.md](02_alkalom/06_Architektura_vazlat.md) | Kliens–szerver architektúra első vázlata, API-vázlat |
| [02_alkalom/07_Adatmodell_vazlat.md](02_alkalom/07_Adatmodell_vazlat.md) | ER-vázlat és kulcs tervezési szabályok |
| [02_alkalom/08_Munkamegosztas.md](02_alkalom/08_Munkamegosztas.md) | Pontosított munkamegosztás, modulgazdák |
| [02_alkalom/09_Teendok_a_3_alkalomig.md](02_alkalom/09_Teendok_a_3_alkalomig.md) | Feladatlista a 3. alkalomig |
| [02_alkalom/Dokumentacio/](02_alkalom/Dokumentacio/) | A dokumentáció első ~5 oldala (Bevezetés fejezet) |
| [02_alkalom/Prezentacio/](02_alkalom/Prezentacio/) | 5 perces prezentáció előadói jegyzetekkel |

### 3. alkalom – Tervezési alapok

| Fájl | Mire való |
|---|---|
| [03_alkalom/01_Kiegeszito_funkcio_javaslat.md](03_alkalom/01_Kiegeszito_funkcio_javaslat.md) | Kiegészítő funkció javaslat: webkamerás vonalkód-beolvasás + címke- és leltárjegyzőkönyv-generálás |
| [03_alkalom/02_Technologiai_dontesek.md](03_alkalom/02_Technologiai_dontesek.md) | A közös technológiai részdöntések összefoglalója |
| [03_alkalom/03_Adatmodell_v1.md](03_alkalom/03_Adatmodell_v1.md) | Adatmodell v1: ER-diagram, táblák, kulcsok, indexek, megszorítások, EF Core |
| [03_alkalom/04_Excel_lekepezes.md](03_alkalom/04_Excel_lekepezes.md) | Az Excel forrásadatok leképezése az adatmodellre, importfolyamat, tesztesetek |
| [03_alkalom/05_Fejlesztesi_utemterv.md](03_alkalom/05_Fejlesztesi_utemterv.md) | Fejlesztési ütemterv, GitHub mérföldkövek, a 4. alkalom feladatai |
| [03_alkalom/Dokumentacio/](03_alkalom/Dokumentacio/) | Dokumentáció v0.2: Bevezetés + Rendszerterv (technológiák, adatmodell) |
| [docs/dontesi_naplo.md](docs/dontesi_naplo.md) | Döntési napló: feltételezések, alternatívák, döntések, következmények |

### 4. alkalom – Prototípus / proof of concept

| Fájl | Mire való |
|---|---|
| [LeltarKezelo.sln](LeltarKezelo.sln) | A teljes megoldás (kliens, szerver, közös projekt, tesztek) |
| [04_alkalom/01_Szerver_PoC.md](04_alkalom/01_Szerver_PoC.md) | Szerver PoC: rétegek, végpontok, hitelesítés, konfiguráció, kliens–szerver kommunikáció |
| [04_alkalom/Dokumentacio/](04_alkalom/Dokumentacio/) | Dokumentáció: architektúra, kommunikáció, szerver PoC fejezetek; v0.4 összefésült dokumentáció |
| [04_alkalom/02_Adatbazis_es_tesztadatok.md](04_alkalom/02_Adatbazis_es_tesztadatok.md) | Adatbázis: első migráció, teljes séma, kezdeti adatok, demóadatok, ellenőrzések, CI |
| [04_alkalom/03_Teendok_az_5_alkalomig.md](04_alkalom/03_Teendok_az_5_alkalomig.md) | A 4. alkalom állapota és a feladatok az 5. alkalomig |
| [04_alkalom/Prezentacio/](04_alkalom/Prezentacio/) | 10 perces prezentáció előadói jegyzetekkel |
| [db/](db/) | Demóadat-szkript és az adatbázis-megszorítások ellenőrző szkriptje |
| [docs/AI_hasznalati_naplo.md](docs/AI_hasznalati_naplo.md) | A mesterséges intelligencia használatának naplója (kötelező) |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Közreműködési szabályok: ágak, commitok, pull requestek |

## Forráskód

### Szerkezet

```
LeltarKezelo.sln
├── src/
│   ├── Kliens/     WPF asztali alkalmazás (net8.0-windows)
│   ├── Szerver/    ASP.NET Core Web API (net8.0)
│   │   ├── Controllers/       HTTP végpontok
│   │   ├── Szolgaltatasok/    üzleti logika, hitelesítés
│   │   └── Adat/              EF Core DbContext, entitások, konfigurációk, kezdeti adatok
│   │       └── Migraciok/     EF Core migrációk
│   └── Kozos/      A kliens és a szerver közös típusai (DTO-k, felsorolások), API-útvonalak
├── tests/
│   └── Szerver.Tesztek/   A szerver API integrációs tesztjei és az adatmodell tesztjei (xUnit)
├── db/             Demóadat-szkript, az adatbázis-megszorítások ellenőrzése
└── .github/workflows/     CI: fordítás és tesztek minden pushnál és pull requestnél
```

### Szükséges eszközök

| Eszköz | Verzió |
|---|---|
| .NET SDK | 8.0 vagy újabb (a `global.json` újabb fő verziót is elfogad) |
| Microsoft SQL Server | 2019 vagy újabb, Express vagy Developer kiadás |
| EF Core parancssori eszköz | `dotnet tool install --global dotnet-ef --version 8.*` |
| Fejlesztőkörnyezet | Visual Studio 2022 (.NET asztali és ASP.NET munkaterhelés) vagy JetBrains Rider |

### Első indítás

1. **Helyi beállítások.** Másold le a mintafájlt, és töltsd ki a saját adataiddal:

   ```bash
   cp src/Szerver/appsettings.Local.example.json src/Szerver/appsettings.Local.json
   ```

   - `ConnectionStrings:Leltar` – a saját SQL Server példányod (a mintában `localhost\SQLEXPRESS`, Windows-hitelesítés;
     alapértelmezett, név nélküli példánynál – pl. Developer kiadás – `Server=localhost`);
   - `Jwt:Kulcs` – legalább 32 karakteres véletlen szöveg, pl. PowerShellben:
     `$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)`;
   - `KezdoAdmin` – fejlesztői módban, üres felhasználótábla esetén ezzel a névvel és jelszóval jön létre az első
     adminisztrátor.

   Az `appsettings.Local.json` **nem kerül a repóba** (`.gitignore`). Minden érték környezeti változóval is megadható,
   pl. `ConnectionStrings__Leltar`, `Jwt__Kulcs`.

2. **Adatbázis létrehozása** a repóban lévő migrációkból (a `src/Szerver/Adat/Migraciok` mappa). A migráció a teljes
   sémát és a kezdeti adatokat (kódtípusok, eszközállapotok, szerepkörök) is létrehozza:

   ```bash
   dotnet ef database update --project src/Szerver
   ```

3. **Demóadatok betöltése (nem kötelező).** Üres, frissen migrált adatbázisba tölt 240 eszközt kódokkal, helyiségeket,
   felelősöket, egy lezárt és egy nyitott leltárt, valamint öt demó felhasználót szerepkörönként
   (a felhasználóneveket és a közös demójelszót a szkript fejléce tartalmazza). Az adatbázis nevét igazítsd a sajátodhoz:

   ```bash
   sqlcmd -S localhost -E -C -d LeltarKezelo -f 65001 -i db/demoadatok.sql
   ```

   Ha demóadatokat töltesz be, a `KezdoAdmin` nem jön létre (mert már van felhasználó), helyette a demó felhasználókkal
   lehet belépni. Az adatbázis-megszorítások ellenőrzése: `sqlcmd ... -i db/ellenorzes.sql` (semmit nem módosít).
   Részletek: [04_alkalom/02_Adatbazis_es_tesztadatok.md](04_alkalom/02_Adatbazis_es_tesztadatok.md).

4. **Szerver indítása:**

   ```bash
   dotnet run --project src/Szerver --launch-profile https
   ```

   A Swagger felület: <https://localhost:7080/swagger> (HTTP-n: <http://localhost:5080/swagger>). Első HTTPS-indítás
   előtt: `dotnet dev-certs https --trust`.

5. **Kliens indítása** (külön terminálban vagy Visual Studióban több indítási projekttel):

   ```bash
   dotnet run --project src/Kliens
   ```

### Tesztek

```bash
dotnet test
```

A szerver tesztjei memóriabeli adatbázissal, a valódi HTTP-csővezetéken keresztül futnak, így SQL Server nélkül is
lefuttathatók. Az adatmodell tesztjei (`AdatmodellTesztek`) az SQL Server-specifikus szabályokat (szűrt indexek,
számított oszlop, CHECK megszorítások, törlési szabályok) és azt ellenőrzik, hogy a migrációk naprakészek-e.
A GitHub Actions ugyanezt futtatja minden pushnál és pull requestnél (`.github/workflows/ci.yml`).

### Gyors próba Swaggerben

1. `POST /api/auth/login` a `KezdoAdmin` adataival (vagy egy demó felhasználóval) → a válaszban kapott `token`
   értékét másold ki.
2. Jobb felül **Authorize** → illeszd be a tokent.
3. `GET /api/eszkozok?kereses=...&oldal=1&oldalMeret=50` → lapozott eszközlista.

### Főbb könyvtárak

| Könyvtár | Verzió | Hol | Mire |
|---|---|---|---|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.11 | Szerver | ORM, SQL Server elérés |
| Microsoft.EntityFrameworkCore.Design | 8.0.11 | Szerver | Migrációk (`dotnet ef`) |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.11 | Szerver | JWT token ellenőrzése |
| Microsoft.Extensions.Identity.Core | 8.0.11 | Szerver | Jelszó-hash (`PasswordHasher`, PBKDF2) |
| Swashbuckle.AspNetCore | 6.6.2 | Szerver | OpenAPI leírás, Swagger UI |
| xUnit, Microsoft.AspNetCore.Mvc.Testing, EF Core InMemory | 2.5.3 / 8.0.11 | Tesztek | Integrációs tesztek |

> A `.md` fájlokban lévő diagramok Mermaid formátumúak – GitHubon és VS Code-ban (Mermaid bővítménnyel) ábraként jelennek meg.

## Az 1. alkalom teendői – gyors checklist

- [ ] 3 fős csoport megalakult, nevek, Neptun-kódok, elérhetőségek rögzítve (`03_Csoport_es_szerepek.md`)
- [ ] Elsődleges szerepek kiosztva
- [ ] Csapatmegállapodás elfogadva (kommunikációs csatorna, határidők, heti egyeztetés)
- [ ] GitHub repository létrehozva, mindhárom tag hozzáadva
- [ ] Oktató hozzáadva a repóhoz: **nagyrobertemail@gmail.com**
- [ ] A `repo_sablon` tartalma feltöltve első commitként
- [ ] Letöltve a kari dolgozatsablon (Word vagy LaTeX) – döntés, melyiket használjuk
- [ ] Nyitott kérdések feltéve az oktatónak (`06_Kerdesek_az_oktatohoz.md`)
- [ ] 2. alkalomig tartó feladatok kiosztva (`05_Teendok_a_2_alkalomig.md`)
