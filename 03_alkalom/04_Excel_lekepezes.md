# Excel forrásadatok leképezése az adatmodellre

**Felelős:** Kiss Barnabás · **Könyvtár:** ClosedXML (D-008) · **Állapot:** terv, a mintafájl alapján pontosítandó

> **Teendő:** a tényleges Excel mintafájlt még nem kaptuk meg. Az alábbi leképezés a feladatkiírásban említett
> mezőkre épül (SAP szám, leltári szám 1–2, megnevezés, gyártási szám, érték). A fájl megérkezésekor a táblázatot
> az oszlopok tényleges nevével és sorrendjével kell frissíteni. Kérdés az oktatóhoz: mikor kapjuk meg a mintafájlt?

## 1. Leképezési táblázat

| Excel oszlop (várt) | Cél | Átalakítás | Validáció | Hiba esetén |
|---|---|---|---|---|
| SAP szám | `EszkozKod` (KodTipus = „SAP szám”) + párosítási kulcs | szóközök levágása, nagybetűsítés | kötelező, egyedi az állományon belül | sor elutasítása, bejegyzés az importnaplóba |
| Leltári szám 1 | `EszkozKod` (KodTipus = „Leltári szám 1”) | szóközök levágása, vezető nullák megtartása | nem kötelező; ha van, egyedinek kell lennie | sor elutasítása ütközésnél |
| Leltári szám 2 | `EszkozKod` (KodTipus = „Leltári szám 2”) | ugyanaz | ugyanaz | ugyanaz |
| Gyártási szám | `EszkozKod` (KodTipus = „Gyártási szám”) | szóközök levágása | nem kötelező; ütközés esetén figyelmeztetés | a kód kihagyása, a sor mehet tovább |
| Megnevezés | `Eszkoz.Megnevezes` | szóközök levágása, többszörös szóköz összevonása | kötelező, max. 200 karakter | sor elutasítása |
| Mennyiség | `Eszkoz.ElvartMennyiseg` | egész számmá alakítás | > 0; ha hiányzik, alapértéke 1 | figyelmeztetés, alapérték használata |
| Mennyiségi egység | `Eszkoz.MennyisegiEgyseg` | szöveg | max. 20 karakter | levágás, figyelmeztetés |
| Érték | `Eszkoz.Ertek` | pénznemre alakítás (tizedesjel, ezreselválasztó) | nem negatív | figyelmeztetés, üresen hagyás |
| Beszerzés dátuma | `Eszkoz.BeszerzesDatuma` | Excel dátum vagy szöveg felismerése | érvényes dátum, nem jövőbeli | figyelmeztetés, üresen hagyás |
| Leltárkörzet (kód vagy név) | `Eszkoz.LeltarkorzetId` | keresés a `Leltarkorzet` táblában | kötelező; ismeretlen körzet | választható: automatikus létrehozás vagy sor elutasítása |
| Eszköztípus / kategória | `Eszkoz.EszkozTipusId` | keresés az `EszkozTipus` táblában | nem kötelező | új típus felvétele jóváhagyás után |
| Felelős személy | `FelelosSzemely` + `FelelosHozzarendeles` | név és/vagy azonosító alapján keresés | nem kötelező | új személy felvétele, figyelmeztetéssel |
| Megjegyzés | `Eszkoz.Megjegyzes` | szöveg | max. 1000 karakter | levágás |
| *(ismeretlen további oszlopok)* | egyelőre nem tároljuk | – | – | az importnapló felsorolja őket |

**Amit az import nem tölt ki:** az eszköz **helyét** (`Elhelyezes`) és az **aktuális állapotot** a leltározás, illetve
a felhasználó adja meg. Minden importált eszköz kezdő állapota „aktív”.

## 2. Az importálás folyamata

1. **Feltöltés.** A leltárfelelős kiválasztja az `.xlsx` fájlt, a kliens feltölti a szerverre (F-05).
2. **Munkalap és fejléc.** A szerver felsorolja a munkalapokat, és felismeri a fejlécsort (az első olyan sor,
   amelyben a várt oszlopnevek többsége szerepel).
3. **Oszlop-hozzárendelés.** A felhasználó a felületen összepárosítja az Excel oszlopait a rendszer mezőivel.
   A felismert párosítás előre kitöltve jelenik meg, és menthető **importsablonként**, hogy a következő importnál ne
   kelljen újra beállítani.
4. **Validálás (próbafuttatás).** A rendszer a teljes fájlt ellenőrzi mentés nélkül, és összesítést ad:
   hány sor rendben, hány figyelmeztetés, hány hiba, és mit talált duplikáltnak.
5. **Import.** Csak a felhasználó jóváhagyása után ír az adatbázisba, **egyetlen tranzakcióban**. A hibás sorok
   kimaradnak, a többi bekerül (F-04).
6. **Napló.** Az eredmény `ImportFutas` és `ImportHiba` sorokban rögzül, és Excelben is letölthető, hogy a hibás
   sorokat javítás után újra be lehessen tölteni.

## 3. Ismételt import

- A párosítás **SAP szám** alapján történik (F-02).
- Meglévő eszköznél a rendszer **mezőnként** mutatja a különbséget, és a felhasználó dönt a frissítésről.
- Az Excelből eltűnt eszközöket **nem töröljük**: „a forrásban már nem szerepel” jelzést kapnak, és a leltárfelelős
  dönt a további sorsukról (F-03).
- Minden módosítás az `AuditNaplo`-ba kerül.

## 4. Technikai megjegyzések (ClosedXML)

- A beolvasás `XLWorkbook` objektummal, munkalap és sorok bejárásával történik.
- A cellák típusát mindig ellenőrizni kell: a dátum és a szám Excelben számként érkezik, a szöveges cellákban viszont
  elírt formátum is lehet.
- Az üres sorokat és a teljesen üres oszlopokat ki kell hagyni.
- A **vezető nullák** miatt a kódokat mindig szövegként olvassuk ki; ha a cella számként jött, a formázott értéket
  vesszük alapul.
- Nagy fájl esetén érdemes soronként feldolgozni, és a hibákat gyűjteni, nem az első hibánál megállni.
- Csak `.xlsx` formátumot kezelünk; `.xls` esetén egyértelmű hibaüzenet jelenik meg (D-008).

## 5. Tesztelés

| Teszteset | Várt eredmény |
|---|---|
| Hibátlan fájl 50 sorral | 50 eszköz, kódokkal, hiba nélkül |
| Hiányzó megnevezés | az adott sor elutasítva, a többi bekerül |
| Ütköző leltári szám két soron | mindkét sor elutasítva, egyértelmű hibaüzenettel |
| Vezető nullás kód (`000123`) | a kód pontosan így kerül be |
| Szám formátumú SAP szám | szövegként, változatlan értékkel kerül be |
| Ugyanaz a fájl kétszer | másodjára nincs új eszköz, csak változatlan párosítás |
| Módosított érték ismételt importnál | a különbség megjelenik, jóváhagyás után frissül |
| Üres munkalap | érthető hibaüzenet, nem összeomlás |
