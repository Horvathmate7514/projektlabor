# Adatbázis: migráció, kezdeti adatok, demóadatok

**Felelős:** Kiss Barnabás · **Állapot:** a 4. alkalomra elkészült, SQL Serveren kipróbálva

A 4. alkalom adatbázis-feladatai (`03_alkalom/05_Fejlesztesi_utemterv.md`, 2., 7. és 9. pont) és a szerver PoC-ban
jelzett átadás (`01_Szerver_PoC.md`, 8. pont): az első migráció, a további entitások, a kezdeti adatok, a
tesztadat-feltöltő és a CI.

## 1. Mi készült el?

| # | Feladat | Eredmény |
|---|---|---|
| 2 | Adatbázis-séma és első migráció | `src/Szerver/Adat/Migraciok/…_Kezdeti.cs` – az adatmodell v1 **mind a 23 táblája** |
| – | A PoC-ból hiányzó entitások | Leltáridőszak, elvárt tétel, leolvasás, kiegészítő és ellenőrzése, helyiség, elhelyezés, felelős személy és hozzárendelés, állapotváltozás, import, audit napló, körzetjogosultság |
| – | Kezdeti adatok | Kódtípusok, eszközállapotok, szerepkörök – a migrációval együtt kerülnek be (D-022) |
| 7 | Tesztadat-feltöltő | `db/demoadatok.sql` – determinisztikus SQL-szkript, 240 eszközzel (D-023) |
| – | Megszorítások ellenőrzése | `db/ellenorzes.sql` – 12 szándékosan hibás művelet + leltár-összehasonlítás |
| – | Adatmodell tesztjei | `tests/Szerver.Tesztek/AdatmodellTesztek.cs` – 21 teszt, köztük a migrációk naprakészsége |
| 9 | GitHub Actions | `.github/workflows/ci.yml` – fordítás és tesztek minden pushnál és pull requestnél |

## 2. A séma

A migráció a `03_alkalom/03_Adatmodell_v1.md` szerinti modellt valósítja meg. A PoC nyolc entitásához (Horváth Máté)
15 új tábla csatlakozik:

| Terület | Új táblák |
|---|---|
| Leltározás | `LeltarIdoszak`, `ElvartTetel`, `Leolvasas`, `KiegeszitoEllenorzes` |
| Történeti adatok | `Helyiseg`, `Elhelyezes`, `FelelosSzemely`, `FelelosHozzarendeles`, `AllapotValtozas`, `Kiegeszito` |
| Rendszer | `ImportFutas`, `ImportHiba`, `AuditNaplo`, `FelhasznaloLeltarkorzet` (jogosult körzetek) |

A modell szabályai az adatbázisban is érvényesülnek, nem csak a szerver kódjában:

| Szabály | Megvalósítás | Forrás |
|---|---|---|
| Az aktív kódok normalizált értéke egyedi | Tárolt számított oszlop + szűrt egyedi index (`WHERE [Aktiv] = 1`) | D-012 |
| Egy eszköznek egyszerre egy aktuális helye és felelőse van | Szűrt egyedi index (`WHERE [ErvenyesIg] IS NULL`) | D-011 |
| Egy eszköz egyszer szerepel egy leltár pillanatképében | Egyedi index (`LeltarIdoszakId`, `EszkozId`) | D-003 |
| Sztornózott leolvasáshoz indoklás kell | `CK_Leolvasas_Sztorno` | UC-08 |
| Ismeretlen kódú leolvasáshoz nem tartozik eszköz, minden máshoz igen | `CK_Leolvasas_Eszkoz` | F-15 |
| Lezárt leltárnak van lezárási időpontja | `CK_LeltarIdoszak_Lezaras` | F-18 |
| A minősítés, a beviteli mód és a többi felsorolás csak érvényes értéket vehet fel | `CHECK … IN (…)`, nagybetűs szövegként tárolva | D-024 |
| Nincs fizikai törlés kaszkáddal | Minden idegen kulcs `Restrict` | D-001 |
| Az összehasonlítás gyors | Index (`LeltarIdoszakId`, `EszkozId`) INCLUDE (`Mennyiseg`, `Minosites`, `Sztornozva`) | 03_Adatmodell_v1 |

A migráció összesen 21 CHECK megszorítást, 6 szűrt indexet és 38 idegen kulcsot tartalmaz (mind `Restrict`).

**Közös projekt:** a leltárral kapcsolatos felsorolások (`Minosites`, `BeviteliMod`, `LeltarIdoszakStatusz` stb.) a
`Kozos/Leltar` mappába kerültek, mert a kliensnek is szüksége lesz rájuk (pl. a beolvasás visszajelzésének színéhez).

## 3. Kezdeti adatok és demóadatok

| | Kezdeti adatok | Demóadatok |
|---|---|---|
| Hol | `Adat/KezdetiAdatok.cs` → a migráció része | `db/demoadatok.sql` |
| Mit | 5 kódtípus, 8 eszközállapot (4 megszűnt), 4 szerepkör | Felhasználók, körzetek, eszközök, leltárak stb. |
| Mikor kerül be | Minden telepítésnél, a `dotnet ef database update` során | Csak fejlesztői vagy demó adatbázisba, kézzel |

### A demóadatok tartalma

| Adat | Mennyiség | Megjegyzés |
|---|---|---|
| Felhasználók | 5 | Szerepkörönként legalább egy; a két leltározó különböző körzetekre jogosult |
| Leltárkörzetek | 6 | Három mérnöki kari épület, GTK, könyvtár, központi igazgatás |
| Eszköztípusok | 20 | Kétszintű hierarchia (pl. Informatika → Monitor) |
| Helyiségek | 38 | Épület + szobaszám, helyiség-vonalkóddal (`H-A-101`) |
| Felelős személyek | 14 | Kitalált nevek, egy inaktív; egy közülük felhasználóhoz is kötve |
| Eszközök | 240 | 24 mintából; ebből 4 mennyiséges készlet (pl. 30 db-os székkészlet) |
| Eszközkódok | 689 | Eszközönként 2–5 kód; vezető nullás régi leltári számok; 9 lecserélt, inaktív címke |
| Kiegészítők | 60 | Számítógép + monitor, laptop + dokkoló (önálló eszközök), billentyűzet, töltő (leírt tartozékok) |
| Helytörténet | 206 sor | 15 eszköz a leltárkor más helyiségben került elő (két sor: régi és új hely) |
| Felelőstörténet | 178 sor | Felelősváltás 2025. március 1-jén több eszköznél |
| Állapotváltozások | 22 | Kétlépéses selejtezés (javasolt → selejtezett), elveszett, ellopott, javítás alatt |
| Leltáridőszakok | 2 | 2025: lezárt, 228 elvárt tétel · 2026: nyitott, 229 elvárt tétel |
| Leolvasások | 307 | Minden minősítésre van példa, köztük sztornózott is |
| Importfutás | 1 | Részben sikeres, 4 tipikus importhibával |

A szkript **determinisztikus**: nem használ véletlenszámot, minden érték az eszköz sorszámából számolódik, így minden
futtatás ugyanazt az adatot adja. Ez teszi a bemutatót és a kézi teszteket megismételhetővé.

**Biztonság:** a szkript leáll, ha az adatbázis nincs migrálva vagy már tartalmaz adatot, és egyetlen tranzakcióban
fut, így hiba esetén semmi sem marad félkészen. A demó felhasználók közös jelszava a szkript fejlécében áll; ez
**kizárólag demó adatbázisban** használható. A jelszó a szerverrel azonos módon előállított PBKDF2-hash formájában
tárolódik.

## 4. Ellenőrzés SQL Serveren

A migrációt és a demóadatokat SQL Server 2025 (Developer kiadás) példányon, a `LeltarKezelo_Proba` adatbázisban
futtattuk. A `db/ellenorzes.sql` eredménye:

| # | Ellenőrzés | Várt | Eredmény |
|---|---|---|---|
| 1 | Aktív kód ismétlése más írásmóddal (`"  40000001 "`) | elutasítva | ✅ megfelelt |
| 2 | Ugyanaz az érték inaktív kódként | elfogadva | ✅ megfelelt |
| 3 | Két aktuális hely egy eszköznek | elutasítva | ✅ megfelelt |
| 4 | Nulla elvárt mennyiség | elutasítva | ✅ megfelelt |
| 5 | Nem létező minősítés | elutasítva | ✅ megfelelt |
| 6 | Sztornó indoklás nélkül | elutasítva | ✅ megfelelt |
| 7 | Ismeretlen kódú leolvasás eszközzel | elutasítva | ✅ megfelelt |
| 8 | Eszköz fizikai törlése | elutasítva | ✅ megfelelt |
| 9 | Lezárt leltár lezárási időpont nélkül | elutasítva | ✅ megfelelt |
| 10 | Eszköz önmaga kiegészítője | elutasítva | ✅ megfelelt |
| 11 | Eszköz kétszer egy pillanatképben | elutasítva | ✅ megfelelt |
| 12 | A normalizált kód automatikus kitöltése | `BV-PROBA-01` | ✅ megfelelt |

**A 2025. évi leltár összehasonlítása** (ugyanebből a szkriptből):

| Kategória | Tétel |
|---|---|
| Elvárt tétel összesen | 228 |
| Megtalált (elvárt = beolvasott) | 177 |
| Részben megtalált (mennyiségi hiány) | 17 |
| Hiányzó | 26 |
| Többlet | 8 |
| Más körzetből előkerült | 12 |
| Ismeretlen kódú beolvasás | 5 |
| Ismételt beolvasás (nem számít bele) | 5 |
| Sztornózott beolvasás | 6 |

**Végponttól végpontig:** a szerver a próba-adatbázissal elindult; a demó leltározóval a bejelentkezés sikeres, az
eszközlista a normalizált kódkeresésre (`" l-2021"`, kisbetűvel és szóközzel) is talál, az eszköz részletei minden
kódot kódtípussal adnak vissza, hibás jelszóra 401 a válasz.

## 5. Automatikus tesztek

`tests/Szerver.Tesztek/AdatmodellTesztek.cs` – az EF Core modelljét vizsgálja, adatbázis-kapcsolat nélkül:

| Teszt | Mit véd |
|---|---|
| `A_migraciok_naprakeszek` | Ha valaki migráció nélkül módosítja a modellt, a teszt (és a CI) elbukik. Kipróbálva: egy mezőhossz átírására hibát jelez. |
| `Nincs_kaszkadolt_torles` | Minden idegen kulcs `Restrict` |
| `Az_aktiv_kodok_normalizalt_erteke_egyedi` | Számított oszlop képlete, szűrt egyedi index |
| `Egy_eszkoznek_egyszerre_egy_aktualis_sora_lehet` (2 eset) | Hely- és felelőstörténet szűrt indexe |
| `A_leolvasas_osszehasonlito_indexe_…` | Az összehasonlítást kiszolgáló index |
| `A_megszoritas_letezik` (7 eset) | A kulcsfontosságú CHECK megszorítások |
| `A_minosites_megszoritasa_minden_erteket_enged` | A CHECK és a C# felsorolás összhangja |
| Felsorolás-tesztek (5 eset) | Oda-vissza alakítás (`MasKorzet` ↔ `MAS_KORZET`) |
| Kezdeti adatok (2 teszt) | A szerepkörök egyeznek a közös projekttel; van aktív és megszűnt állapot |

Eredmény: **34/34 sikeres** (13 API-teszt + 21 adatmodell-teszt), Debug és Release módban is.

**Amit ezek nem fednek le:** a megszorítások tényleges működését az SQL Serveren a `db/ellenorzes.sql` ellenőrzi;
ez jelenleg kézzel futtatandó, mert a CI-ban nincs SQL Server.

## 6. CI

`.github/workflows/ci.yml` – Windows futtatón (a WPF kliens miatt): visszaállítás, fordítás Release módban, tesztek,
a teszteredmények letölthető mellékletként. Indul a `main` és a `200` ágra történő pushnál, minden pull requestnél
és kézzel is. A lépéseket helyben lefuttattuk (0 figyelmeztetés, 34/34 teszt); a GitHubon az első futás a push után
lesz látható az *Actions* fülön.

## 7. Nyitott kérdések, következő lépések

- Az `AuditNaplo` tábla megvan, de a kitöltése (a `SaveChanges` felüldefiniálásával, a bejelentkezett felhasználóval)
  a 10. alkalom feladata.
- A leltárkörzet-hozzárendelés története (eszköz átkerül másik körzetbe) továbbra is nyitott (Adatmodell v1, 6. pont).
- A forrás Excel ismeretlen többletoszlopainak tárolása a mintafájl megérkezése után dönthető el.
- Az 5. alkalomra: Excel import szolgáltatás a ClosedXML-lel, az `ImportFutas` / `ImportHiba` táblák kitöltésével.
