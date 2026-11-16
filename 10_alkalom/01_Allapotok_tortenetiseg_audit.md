# Eszközállapotok, történetiség és audit napló – szerveroldal

**Felelős:** Kiss Barnabás · **Állapot:** a szerveroldal és a tesztek elkészültek, SQL Serveren kipróbálva; a kliens képernyői (Bárkányi Máté) még hiányoznak

A 10. alkalom elvárása: a rendszerben ne vesszenek el visszamenőleg az adatok, és egy korábbi állapot lehetőleg
rekonstruálható legyen. Az állapotok, az állapotváltozások és az audit napló táblái az adatmodell v1 óta megvannak,
ezért **sémaváltozás és új migráció nem kellett**. Döntések: D-045–D-047 (javasolt).

## 1. Mi készült el?

| Elvárás (kiírás, 10. alkalom) | Megvalósítás |
|---|---|
| Eszközök állapotának kezelése; selejtezett, elveszett, ellopott és egyéb állapotok | `POST /api/eszkozok/{id}/allapot` a nyolc állapot bármelyikére (F-25) |
| Törlés helyett logikai törlés | Törlő végpont nincs; a megszűnt állapot jelölő, a törzsadat inaktiválható (D-001, D-043) |
| Időbélyegek kezelése | Állapotváltozás, hely, felelős, módosítás és naplósor mind időbélyeggel, UTC-ben (F-29) |
| Az állapotváltozások nyomon követése | `AllapotValtozas` sor: régi és új állapot, kötelező indoklás, ügyiratszám, felhasználó, időpont (F-26) |
| Korábbi állapotok és leltárak megőrzése | Hely- és felelőstörténet érvényességi időszakkal; a leltárak pillanatképe és leolvasásai változatlanok |
| Az adatok történetének lekérdezése | `GET /api/eszkozok/{id}/tortenet`, `GET /api/eszkozok/{id}/pillanat?idopont=`, `GET /api/auditnaplo` |

## 2. Végpontok

| Metódus és útvonal | Jogosultság | Leírás |
|---|---|---|
| `POST /api/eszkozok/{id}/allapot` `{ "eszkozAllapotId": 6, "indoklas": "...", "ugyiratszam": null }` | admin, leltárfelelős | Állapotváltás; a válasz az eszköz aktuális hozzárendelései |
| `GET /api/eszkozok/{id}/tortenet` | bejelentkezett, látható körzet | Állapot-, hely- és felelőstörténet, szereplés a leltárakban, adatmódosítások |
| `GET /api/eszkozok/{id}/pillanat?idopont=2026-03-01T00:00:00Z` | bejelentkezett, látható körzet | Az eszköz állapota, helye és felelőse a megadott időpontban |
| `GET /api/auditnaplo?entitas=&entitasId=&felhasznaloId=&tol=&ig=&oldal=&oldalMeret=` | admin, leltárfelelős | Az audit napló lapozva, legújabb elöl |

Hibák: hiányzó vagy 500 karakternél hosszabb indoklás és nem létező állapot 400; az eszköz már a kért állapotban van
409; nem létező eszköz 404; leltározó és megtekintő nem válthat állapotot, és nem látja a naplót (403).

## 3. Állapotváltás (D-045)

- Az indoklás kötelező, az ügyiratszám nem (lopásnál, selejtezésnél hasznos).
- **Megszűnt állapotba** (selejtezett, elveszett, ellopott, átadva) lépéskor az eszköz aktuális helye és felelőse
  **lezárul**. A történet megmarad, de egy megszűnt eszköz nem áll helyiségben, és nincs gazdája. A demóadatok is így
  épülnek fel, és az ellenőrző szkript ezt külön pontban vizsgálja.
- Nem megszűnt állapotváltásnál (pl. javítás alatt) a hely és a felelős megmarad.
- **Megszűnt eszköz visszaállítható** (például előkerült): ez is állapotváltás indoklással. A hely és a felelős nem
  áll vissza magától, a leltárfelelős adja meg újra.
- **Hatása a leltárra:** a megszűnt eszköz nem kerül az új leltár pillanatképébe (D-030); ha mégis beolvassák, a
  leolvasás *nem aktív* minősítéssel rögzül, és a tétel *nem elvárt*. A már nyitott leltár pillanatképe nem változik.

## 4. Az eszköz története

A `tortenet` válasz öt listát ad vissza:

| Lista | Tartalom |
|---|---|
| `allapotok` | Minden állapotváltás: időpont, régi és új állapot, indoklás, ügyiratszám, ki rögzítette |
| `helyek` | Helytörténet: helyiség, érvényesség, forrás (leltár vagy kézi), a leolvasás azonosítója, ki rögzítette |
| `felelosok` | Felelőstörténet: személy, érvényesség, ki rögzítette |
| `leltarak` | Leltáranként: elvárt és beolvasott mennyiség, tételállapot (D-037), talált körzetek, **helyiségenkénti mennyiség**, utolsó beolvasás |
| `modositasok` | Az eszköz és a kódjai audit naplója |

A „ki rögzítette” leltárból származó helynél a leltározó, kézi áthelyezésnél és felelősváltásnál az audit napló szerinti
felhasználó. A leltárak számítása ugyanaz a függvény, mint az összesítésben (`OsszesitesSzolgaltatas.Szamol`), így a
két nézet nem térhet el egymástól.

## 5. Korábbi időpont visszakeresése (D-047)

A `pillanat` végpont a történeti táblákból adja vissza az eszköz akkori állapotát, helyét és felelősét:

- **állapot:** az időpont előtti utolsó állapotváltás új állapota; ha előtte nem volt váltás, az első későbbi váltás
  régi állapota; ha egyáltalán nem volt váltás, a mostani;
- **hely, felelős:** az az érvényességi időszak, amelybe az időpont esik;
- ha az eszköz akkor még nem létezett (`letezett = false`), a többi mező üres.

Az eszköz többi mezőjének (megnevezés, körzet, mennyiség, érték, kódok) korábbi értéke az audit napló régi és új
értékeiből olvasható ki; ezekre külön „pillanat” számítás nem készült.

## 6. Audit napló (S01, D-046)

A naplózás **automatikus**: az adatréteg minden mentéskor a változáskövetőből állítja elő a sorokat, ezért a
szolgáltatásoknak nem kell külön naplózniuk, és új funkció sem maradhat ki.

| Mit naplóz | Mit nem, és miért |
|---|---|
| Eszköz, eszközkód, eszköztípus, helyiség, felelős személy, leltárkörzet, kiegészítő-kapcsolat, leltáridőszak, felhasználó | Leolvasás, állapotváltozás, importnapló: ezek maguk is eseménynaplók |
| Kézi áthelyezés és felelősváltás (más nem őrzi meg, ki rögzítette) | Leltárból származó hely: a leolvasás azonosítja |
| | A felhasználó jelszó-hash-e és utolsó belépése: nem kerül a naplóba |

- Egy sor: entitás, azonosító, művelet (`LETREHOZAS`, `MODOSITAS`), a megváltozott mezők régi és új értéke JSON-ként,
  a felhasználó és az időpont. Például: `{"EszkozTipusId":null}` → `{"EszkozTipusId":11}`.
- Módosításnál a sor **ugyanabban a mentésben** íródik, mint az adat. Új elemnél az azonosító csak a mentés után
  ismert, ezért a naplósor második mentésben készül; a kettőt SQL Serveren tranzakció fogja össze.
- Az új eszköz kódjai az eszköz létrehozási sorában szerepelnek, nem külön sorokban: egy 20 000 soros import így
  eszközönként egy naplósort ír.
- Az eszköz módosításakor a `Modositva` és a `ModositottaId` mező is automatikusan kitöltődik.
- A kérést végző felhasználót egy rövid köztes réteg adja át az adatrétegnek a `Program.cs`-ben.
- A demóadat-szkript közvetlenül az adatbázisba ír, ezért a demóadatokról nincs naplósor.

## 7. Megvalósítás

| Rész | Hol |
|---|---|
| Közös típusok | `src/Kozos/Eszkozok/EszkozKezeles.cs`, `EszkozTortenet.cs`, `src/Kozos/Naplo/NaploAdatok.cs` |
| Állapotváltás | `src/Szerver/Szolgaltatasok/EszkozKezelesSzolgaltatas.cs` (`AllapotAsync`) |
| Történet, pillanat, naplólista | `.../TortenetSzolgaltatas.cs`, `Controllers/EszkozKezelesController.cs`, `NaploController.cs` |
| Audit napló | `src/Szerver/Adat/AuditKeszito.cs`, `LeltarDbContext.cs` (`SaveChanges`) |

**Mások fájljait érintő változás (átnézendő):** a `Program.cs`-be (Horváth Máté) egy rövid köztes réteg és a
szolgáltatások regisztrációja került. Az automatikus naplózás Horváth Máté műveleteire is hat: a leltáridőszak
létrehozása, nyitása és lezárása ezentúl naplósort ír, a kódja változtatása nélkül.

## 8. Tesztek

| Tesztosztály | Teszt | Mit ellenőriz |
|---|---|---|
| `AllapotTortenetTesztek` | 15 | Megszűnés és visszaállítás, hibás váltás, jogosultság, pillanatkép és beolvasás megszűnt eszközzel, korábbi időpont, audit sorok, import naplózása |
| `SqlServerTortenetTesztek` | 1 | Állapotváltás, naplózás és történet valódi SQL Serveren, utána az ellenőrző szkript 30 pontja |

A teljes csomag: **251 teszt**, SQL Server nélkül 242 sikeres és 9 kihagyott, SQL Serverrel mind a 251 sikeres.

## 9. Nyitott pontok

- **Kliens (Bárkányi Máté):** állapotváltás az eszköz részleteinél, történet- és naplónézet.
- **Felhasználókezelés** még nincs, ezért a felhasználók módosításáról egyelőre csak a létrehozás naplózódik.
- **A napló mérete:** nincs archiválás vagy törlés; a félév adatmennyiségénél ez nem gond.
- **Az összesítés és a történet** a megszűnt eszközt a nyitott leltárban továbbra is elvártként mutatja (pillanatkép).
  Hogy ezt a 9. alkalom összehasonlító nézetei külön jelöljék-e, arról Horváth Máténak kell döntenie.
- **A D-045–D-047 döntések „javasolt” állapotúak**, a csoport jóváhagyására várnak.
