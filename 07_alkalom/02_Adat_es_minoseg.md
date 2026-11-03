# Mennyiségek: adatok, ellenőrzés és tesztek

**Felelős:** Kiss Barnabás · **Állapot:** a 7. alkalomra elkészült, SQL Serveren kipróbálva

A 7. alkalom elvárása, hogy a leltározás ne csak „megtalált / nem talált” logikával működjön, hanem a mennyiségi
eltéréseket és a más körzetben előkerült eszközöket is kezelje. A szerverlogikát Horváth Máté készítette el
([01_Mennyisegek_es_elteresek_szerver.md](01_Mennyisegek_es_elteresek_szerver.md)). Ez a leírás az adat- és minőségi
oldalt mutatja be: a demóadatokat, az ellenőrzést, az import mennyiségkezelését, a teszteket és a kód átnézését.

## 1. Mi készült el?

| Feladat | Hol | Eredmény |
|---|---|---|
| A nyitott leltár demóadatainak bővítése | `db/demoadatok.sql` | Minden tételállapot és minősítés bemutatható; 7 hibás régi minősítés javítva |
| Az ellenőrző szkript bővítése | `db/ellenorzes.sql` | 15 megszorítás-próba, független összehasonlítás körzetenként, 10 következetességi ellenőrzés |
| A mennyiség értelmezése az importban | `.../Import/ImportErtekek.cs`, `ImportSzolgaltatas.cs` | „30 szett”, „1 200”, „30,0” is helyes; a rossz érték nem írja felül a meglévő darabszámot |
| Mennyiségi mintafájl | `db/minta/forrasadatok_mennyisegek.xlsx` | A szöveges mennyiségek és az ismételt import kézi kipróbálásához |
| Végigmenő tesztek | `tests/Szerver.Tesztek/MennyisegVegigTesztek.cs` | Excel → pillanatkép → beolvasás → összesítés, a valódi végpontokon |
| Tesztek valódi SQL Serveren | `tests/Szerver.Tesztek/SqlServerTesztek.cs` | 7 teszt, környezeti változóval kapcsolható |
| A mennyiségkezelés kódjának átnézése és javítása | 5. pont, `OsszesitesSzolgaltatas.cs` | 2 hiba javítva, tesztekkel (D-041) |
| Teljesítménymérés | 7. pont | 20 000 eszközzel is elfogadható válaszidők |
| Dokumentáció | `07_alkalom/Dokumentacio/` | v0.6: új *4. Tesztelés és validáció* fejezet, 3.4.6, 3.5 frissítése |

## 2. Demóadatok

A 6. alkalomig a nyitott 2026-os leltárban minden leolvasás hibátlan volt, így a 7. alkalom témáját nem lehetett rajta
bemutatni: nem volt részleges mennyiség, többlet, ismételt, sztornózott és ismeretlen kódú leolvasás. A szkript továbbra
is determinisztikus, és csak üres, frissen migrált adatbázisba tölthető.

| Eszköz | Mi történik a leltárban | Tételállapot |
|---|---|---|
| 5 – székkészlet (30 db), MIK-A | 12 + 15 db | **Részleges**, hiány: 3 |
| 149 – székkészlet (30 db), MIK-A | 20 + 13 db, a második leolvasás többletként minősül | **Többlet**: 3 |
| 13 – multiméter-készlet (10 db), MIK-A | 6 + 4 db | **Rendben** |
| 29 – székkészlet (30 db), MIK-B | 30 db, majd 5 db tévesen, amelyet sztornóztak | **Rendben** |
| 31 – asztalkészlet (15 db), MIK-B | 9 db a sajátjában, 4 db a MIK-A-ban | **Részleges**, hiány: 2, két talált körzet |
| 151 – asztalkészlet (15 db), MIK-A | mind a 15 db a MIK-B-ben | **Más körzetben** |
| 45 – tárgyalószék-készlet (6 db), MIK-B | 6 + 2 db | **Többlet**: 2 |
| 7 – asztalkészlet (15 db), MIK-A | nem olvasták be | **Hiányzik**: 15 |
| 9 – nyomtató (egyedi), MIK-A | 2-es mennyiséggel, elgépelés | **Többlet**: 1 |
| 12 – multiméter (egyedi), MIK-A | előbb a MIK-B-ben, majd a sajátjában (ismételt) | **Rendben**, két talált körzet |
| 6 – oktatói asztal (egyedi), MIK-A | véletlen dupla beolvasás | **Rendben**, 1 db |
| 8 – iratszekrény (egyedi), MIK-A | az első leolvasást sztornózták, az ismételt megmaradt | **Rendben** (D-036) |
| 50 – monitor (egyedi), MIK-I | a MIK-A-ban került elő | **Más körzetben** |
| 74 – selejtezett monitor, GTK-G | előkerült, nem aktív | **Nem elvárt** |
| 238 – wifi pont, GTK-G | a nyitás után vették fel, utólag elvárt lett | **Rendben** |
| 240 – vetítővászon, GTK-G | a nyitás után vették fel, nem került az elvártak közé | **Nem elvárt** |

Ezen felül 3 ismeretlen kódú leolvasás is van, köztük egy lecserélt, inaktív régi címke (`L-REGI-25`).

**A 2026-os leltár összesítése** (`GET /api/leltaridoszakok/2/osszesites`):

| Körzet | Elvárt tétel | Rendben | Részleges | Hiányzik | Többlet | Más körzetben | Ide került | Nem elvárt | Ismeretlen | Elvárt db | Beolvasott db |
|---|---|---|---|---|---|---|---|---|---|---|---|
| GTK-G | 45 | 1 | 0 | 44 | 0 | 0 | 0 | 2 | 0 | 159 | 1 |
| KONYV | 22 | 0 | 0 | 22 | 0 | 0 | 0 | 0 | 0 | 79 | 0 |
| KOZP | 24 | 0 | 0 | 24 | 0 | 0 | 0 | 0 | 0 | 81 | 0 |
| MIK-A | 46 | 35 | 1 | 7 | 2 | 1 | 2 | 0 | 1 | 160 | 135 |
| MIK-B | 46 | 38 | 1 | 6 | 1 | 0 | 2 | 0 | 2 | 151 | 126 |
| MIK-I | 45 | 0 | 0 | 44 | 0 | 1 | 0 | 0 | 0 | 116 | 1 |
| **Összesen** | **228** | **74** | **2** | **147** | **3** | **2** | **4** | **2** | **3** | **746** | **263** |

**Javított régi adatok.** A tárolt minősítések visszajátszása (3. pont) a 2025-ös leltárban 7 sort talált, amelyet a
szerver másképp minősített volna: 6 sztornózott dupla beolvasás `OK` volt (5 ismételt és 1 többlet helyett), egy más
körzetben talált többlet pedig `TOBBLET` (a D-027 szerint `MAS_KORZET`). A darabszámokat ez nem érintette.

**Változott számok:** 321 leolvasás (307 helyett), 456 elvárt tétel (457 helyett). A többi adat változatlan.

## 3. Az ellenőrző szkript

A `db/ellenorzes.sql` három részből áll, és nem módosítja az adatbázist.

1. **Megszorítások.** A korábbi 12 próba mellé 3 új került: nulla darabos leolvasás, nulla elvárt mennyiség a
   pillanatképben, nem létező felvételi ok. Mind a 15 megfelelt.
2. **Összehasonlítás körzetenként, mindkét leltárra.** A darabszámot és a tételállapotot (D-036, D-037) **tisztán
   SQL-ből** számolja, a szerver kódjától függetlenül. Eredménye körzetről körzetre, minden oszlopban egyezik az
   összesítő végponttal, a 2025-ös és a 2026-os leltárban is. A 2025-ös leltár: 228 elvárt tételből 167 rendben,
   16 részleges, 26 hiányzik, 7 többlet, 12 más körzetben. A kategóriák most kizárják egymást; a korábbi szkript számai
   (177, 17, 26, 8, 12) átfedtek, mert a más körzetben talált tétel a többiben is szerepelt.
3. **Következetesség** (0 eltérés = megfelelt):

| # | Ellenőrzés | Eredmény |
|---|---|---|
| 1 | A tárolt minősítés megegyezik a szabályok szerintivel (visszajátszás, D-027) | megfelelt |
| 2 | A beolvasott kód a leolvasáshoz rendelt eszköz kódja | megfelelt |
| 3 | Az ismeretlen kód valóban nem aktív kód | megfelelt |
| 4 | A leltározó csak a hozzá rendelt körzetben olvasott be | megfelelt |
| 5 | Leolvasás csak az időszak kezdete után és a lezárása előtt | megfelelt |
| 6 | A sztornó a leolvasás után történt | megfelelt |
| 7 | Körzetenként a tételállapotok összege az elvárt tételek száma | megfelelt |
| 8 | Az összes elvárt és beolvasott darab különbsége = összes hiány − összes többlet | megfelelt |
| 9 | A nyitott leltárban minden tételállapotra van példa | megfelelt |
| 10 | A nyitott leltárban minden minősítésre van példa | megfelelt |

A szkript a mintafájlok importja és újabb beolvasások után is 25/25 eredményt ad.

## 4. A mennyiség az Excel-importban

A darabszámok mostantól elvárt és tényleges mennyiséget hasonlítanak össze, ezért egy rosszul beolvasott mennyiség
hamis hiányt vagy többletet okozna. Korábban a „30 szett” és a „30,0” figyelmeztetéssel 1 darab lett.

| Cella | Eredmény | Megjegyzés |
|---|---|---|
| `30` (szám) | 30 | |
| `1 200` | 1200 | Szóközzel tagolt szám |
| `30,0` vagy `30.00` | 30 | Szövegként tárolt egész |
| `30 db` | 30, egység: db | |
| `12 szett` | 12, egység: szett | Az *Egység* oszlop erősebb, mint a cellába írt egység |
| `1.000` vagy `1,000` | figyelmeztetés | Nem egyértelmű: ezer vagy egy; szándékosan nem találgatunk |
| `2,5`, `sok`, `0`, `-3` | figyelmeztetés | Új eszköznél 1, **meglévőnél a régi darabszám marad** |

**Ismételt import nyitott leltár közben.** A törzsadat frissül, de a leltár a nyitáskori pillanatképből dolgozik (D-003):
ha a székkészlet 30-ról 32-re változik, a folyamatban lévő leltár 30-at vár, a következő már 32-t. A nyitás után importált
eszköz *nem elvárt* tétel, amíg a leltárfelelős utólag fel nem veszi.

**A mintafájl** (`db/minta/forrasadatok_mennyisegek.xlsx`) a demóadatokon: 6 új, 1 módosított (30 → 32 db), 2 változatlan,
0 hibás, 2 figyelmeztetés. A *Mit mutat a sor?* oszlop soronként leírja, mit vizsgál; az import ismeretlen oszlopként
jelzi, nem tárolja.

## 5. A mennyiségkezelés kódjának átnézése

A modulfelosztás szerint a mennyiségkezelés fő felelőse Horváth Máté, a társfelelős Kiss Barnabás. Az átnézés szerint a
számítás és a jogosultság az általános esetekben helyes, és SQL Serveren is működik. Két hibát találtunk. Mindkettőt
futó teszt igazolta, és mindkettő javítva van az `OsszesitesSzolgaltatas`-ban (D-041).

| # | Súly | Eset | A javítás előtt | Most |
|---|---|---|---|---|
| 1 | Közepes | Egyedi eszköz első beolvasása után újabb beolvasás 5-ös mennyiséggel | A képernyő azt írja: „A darabszám nem nőtt” (`ISMETELT`), az összesítés viszont 5 db, **többlet: 4** | 1 db, rendben (D-025) |
| 2 | Kicsi | Nem elvárt egyedi eszköz kétszer beolvasva, az első sztornózva | Beolvasott: 0, pedig az eszköz előkerült | Beolvasott: 1 |

**Az 1. hiba oka és javítása.** Az `OsszesitesSzolgaltatas.Szamol` egyedi eszköznél az összes leolvasás mennyiségének
maximumát vette, az ismételtekét is. Az ismételt leolvasás most legfeljebb 1 darabot jelent:

```csharp
static int Darab(IEnumerable<Talalat> t) =>
    t.Select(x => x.Minosites == Minosites.Ismetelt ? 1 : x.Mennyiseg).DefaultIfEmpty(0).Max();
return (Darab(talalatok), Darab(talalatok.Where(t => t.KorzetId == elvartKorzetId)));
```

A `db/ellenorzes.sql` ugyanígy számol, ezért a szkript és a végpont ilyen sorokkal is egyezik. A demóadatokban nincs
ilyen sor, így a demó számai nem változtak.

**A 2. hiba oka és javítása.** A nem elvárt tétel `elvart = 0` értékkel a mennyiséges ágra került, így az ismételt sor nem
számított. Az „egyedi-e” kérdést nem elvárt tételnél most a törzsadat `ElvartMennyiseg` értéke dönti el, ahogy a
`BeolvasasSzolgaltatas` is teszi. A tételen megjelenő elvárt mennyiség továbbra is 0.

A javítás előtt a hibákra írt 6 teszt elbukott, utána mind sikeres. Változatlan szabály, hogy a többletként minősített
leolvasás megtartja a mennyiségét: egy egyedi eszköz első beolvasása 2-es mennyiséggel 1 többletet ad.

## 6. Tesztek

| Tesztosztály | Teszt | Mit ellenőriz |
|---|---|---|
| `AdatmodellTesztek` | 21 | Séma, indexek, megszorítások, migráció naprakészsége |
| `ApiTesztek` | 13 | Bejelentkezés, jogosultság, keresés, lapozás |
| `EszkozlistaSzuresTesztek` | 11 | Törzsadatok, szűrés, láthatóság |
| `LeltarozasTesztek` | 40 | Időszak, beolvasás, minősítés, sztornó, demókód |
| `OsszesitesTesztek`, `UtolagosFelvetelTesztek` | 24 | Darabszám, tételállapot, körzetösszesítő, utólagos felvétel |
| `ImportTesztek` | 54 | Import szolgáltatás (22 új: szöveges mennyiség, egység, rossz érték, mezőnkénti különbség) |
| `ImportVegpontTesztek` | 15 | Import végpontok |
| `MennyisegVegigTesztek` | 16 | **Új:** a teljes lánc Exceltől az összesítésig |
| `SqlServerTesztek`, `SqlServerDarabszamTesztek` | 8 | **Új:** csak SQL Serverrel futnak |
| **Összesen** | **202** | SQL Server nélkül 194 sikeres + 8 kihagyott; SQL Serverrel mind a 202 sikeres |

A `MennyisegVegigTesztek` esetei:
- Excelből importált készlet: részleges → többlet → sztornóval vissza;
- ismételt import nyitott leltár közben: a pillanatkép nem változik, a következő leltár már az új értéket várja;
- nyitás után importált készlet: nem elvárt → utólagos felvétel → részleges;
- a cellába írt egység megjelenik a beolvasás üzenetében és az összesítésben;
- készlet részben más körzetben, teljesen más körzetben, a kettő együtt többet ad;
- egyedi eszköz 2-es mennyiséggel, majd sztornó után helyesen;
- egyedi eszköz előbb más körzetben, majd a sajátjában;
- egyedi eszköz ismételt beolvasása 5-ös mennyiséggel: 1 db, rendben;
- nem elvárt egyedi eszköz két beolvasása, az első sztornója után: 1 db;
- nem elvárt készlet két beolvasása: a mennyiségek összeadódnak;
- minden leolvasás sztornója után újra hiányzik;
- a körzetösszesítő belső egyensúlya;
- lezárt leltár: az összesítés nem változik, új beolvasás 409;
- a megtekintő csak a saját egységét látja.

**A `SqlServerTesztek`** azt ellenőrzi, amit a memóriabeli adatbázis nem tud: létrehoz egy ideiglenes adatbázist,
lefuttatja a migrációkat és a demóadatokat, majd:
- az ellenőrző szkript mindhárom részét;
- az összesítő szolgáltatás egyezését a független SQL-számítással, mindkét leltárra;
- a demóleltár 16 eszközének várt állapotát;
- a beolvasást és a sztornót a számított kódoszlopon át;
- a két javított hibát: külön ideiglenes adatbázison, megmaradó leolvasásokkal, utána a szkript és a független számítás is egyezik;
- az importot mindhárom mintafájllal.

A többi író teszt a végén visszagörget, az ideiglenes adatbázisok törlődnek. Futtatás:

```powershell
$env:LELTAR_TESZT_SQLSERVER = "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True"; dotnet test
```

**Érzékeli-e a tesztkészlet a hibát?** Próbaként szándékosan elrontottuk a tételállapot szabályát: 7 teszt bukott el,
köztük a két SQL Serveres összehasonlítás. A kódot visszaállítottuk.

## 7. Teljesítmény

Ideiglenes adatbázison, fejlesztői (Debug) fordítással, helyi SQL Serveren: **20 240 elvárt tétel, 30 000 leolvasás**.

| Művelet | Válaszidő |
|---|---|
| Körzetösszesítő (`/osszesites`) | 0,2–0,6 s |
| Tétellista, 200 elem (`/tetelek`) | 0,4 s |
| Csak az eltérések egy körzetben | 0,2–0,4 s |
| Eszközlista kereséssel | 0,1–0,3 s |
| Egy beolvasás | 0,02 s (az első 0,25 s) |
| Import próbafuttatása, 20 000 sor | 3 s (a válasz 2,8 MB) |
| Import végrehajtása, 20 000 új eszköz | 11 s |

Az eredmény alapján most nem kell új index vagy SQL-oldali csoportosítás. A tartalék akkor fogy el, ha a leolvasások
száma több százezerre nő.

## 8. Hogyan próbáld ki

A demóadatok csak üres adatbázisba tölthetők, ezért a `LeltarKezelo_Proba` tartalmát újra kell tölteni. Ez **törli az
adatbázisban lévő próbaadatokat** (pl. a korábbi importokat), ezért ezt csak akkor futtasd, ha nincs rájuk szükség:

```bash
sqlcmd -S localhost -E -C -Q "DROP DATABASE LeltarKezelo_Proba"
```

```bash
dotnet ef database update --project src/Szerver
```

```bash
sqlcmd -S localhost -E -C -d LeltarKezelo_Proba -f 65001 -i db/demoadatok.sql
```

```bash
sqlcmd -S localhost -E -C -d LeltarKezelo_Proba -f 65001 -i db/ellenorzes.sql -W -s "|"
```

Ezután `GET /api/leltaridoszakok/2/osszesites` és `GET /api/leltaridoszakok/2/tetelek?csakElteresek=true` mutatja az
eltéréseket.

## 9. Nyitott pontok

- **A valódi forrásfájl** még mindig nem érkezett meg az oktatótól.
- **Az SQL Serveres tesztek a CI-ban nem futnak**, csak helyben (D-039).
- **A dokumentáció** Horváth Máté 7. alkalmi fejezetét még nem tartalmazza: ez az ő anyaga, az `.md` változat kész.
- **Az összesítés a kliensben** még nem jelenik meg (Bárkányi Máté).
