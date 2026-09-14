# A féléves feladat értelmezése

## 1. Egy mondatban

Háromfős csoportban egy **kliens–szerver architektúrájú, asztali GUI-s leltározó és leltárkezelő rendszert** kell
megtervezni, elkészíteni, tesztelni, **legalább 60 oldalban dokumentálni** és **rendszeresen prezentálni** –
úgy, mintha egy egyetem valóban ezt használná az éves leltárhoz és az eszközök hosszú távú nyilvántartásához.

## 2. Mit jelent ez a gyakorlatban?

A tárgy **nem programozási feladat**, hanem egy „kicsinyített szakdolgozat” csoportban. A program csak egy része az
értékelésnek; **a dokumentáció és a prezentáció kiemelt súllyal számít**.

### Kemény (nem alkudható) feltételek

| Terület | Elvárás |
|---|---|
| Csoport | 3 fő, egyéni hozzájárulás legyen azonosítható |
| Architektúra | Valódi kliens–szerver; üzleti logika és adatbázis-elérés **a szerveren** |
| Kliens | **Asztali** grafikus felület (Qt/C++, WPF, .NET MAUI, Avalonia, WinUI, Uno, Python GUI, Java GUI…). **Webes kliens tilos** (Electron, böngészős felület stb. sem) |
| Technológia | Fejlesztés előtt egyeztetni kell az oktatóval; csoportok lehetőleg eltérő technológiát válasszanak |
| Adatok | Excel → **adatbázis** import; a rendszer az adatbázisból dolgozik, nem az Excelből |
| Törlés | Nincs fizikai rekordtörlés; állapotjelző + időbélyeg, visszakereshető történet |
| Dokumentáció | Kari sablon (Word/LaTeX), min. 60 oldal, **folyamatosan** készül |
| Fejezetek | Bevezetés · Rendszerterv · Dizájn és fejlesztés · Tesztelés és validáció · Összefoglalás |
| Git | GitHub, folyamatos commitok, oktató hozzáadva (nagyrobertemail@gmail.com), README, **titok nem kerülhet fel** |
| Kiegészítő funkció | Legalább egy saját, érdemi funkció – **csak oktatói jóváhagyás után** valósítható meg |
| AI | Használható, de dokumentálni kell; aki nem érti a saját kódját → **elégtelen (1)** |
| Prezentáció | Minden alkalommal a **teljes** projekt bemutatása az alapoktól, nem csak az újdonságok |
| Bemutató | Saját gépen, előre kipróbált környezetben, minden tag jelen van |

### Időkeretek a prezentációkhoz

| Hét | Időtartam |
|---|---|
| 2. hét | 5 perc |
| 3–13. hét | 10 perc előadás + 5 perc kérdés |
| 14. hét | 8–9 perc (záró) |

## 3. Amit a kiírás szándékosan nyitva hagy

A kiírás többször hangsúlyozza, hogy **a hiányzó részleteket nekünk kell kitalálni, és ezt dokumentálni kell**.
Minden ilyen döntésnél négy dolgot kell rögzíteni (erre szolgál a `repo_sablon/docs/dontesi_naplo.md`):

1. milyen **feltételezést** tettünk,
2. milyen **alternatívák** merültek fel,
3. milyen **szempontok** alapján döntöttünk,
4. milyen **következményei** vannak a döntésnek.

Nyitott területek (a kiírás 18. pontja alapján): adatmodell, adatbázis-szerkezet, kommunikációs protokoll,
jogosultságok, GUI felépítése, állapotok kezelése, hibás beolvasások kezelése, módosítások naplózása,
exportok szerkezete, tesztelési stratégia.

> **Csapda:** a „minimális” megoldás kifejezetten kerülendő. Pl. a felhasználó ne csak felhasználónév + jelszó legyen,
> ha a szerepkörök, szervezeti egység, elérhetőség stb. indokolt.

## 4. Értékelési szempontok és mit jelentenek nekünk

| Szempont | Mire figyeljünk |
|---|---|
| Funkcionalitás | Vállalt funkciók működjenek, a leltárfolyamat végigjárható legyen |
| Architektúra | Tiszta rétegek, indokolt technológiai döntések |
| GUI | Áttekinthető, következetes, „valódi szoftver” benyomása; nagy adatmennyiségnél is használható |
| **Dokumentáció** ⭐ | Szakmai mélység, döntések indoklása, folyamatos haladás, formai követelmények |
| Git | Rendszeres, értelmes commitok mindhárom tagtól; rendezett repó |
| **Prezentáció** ⭐ | Érthető, teljes, mindenki tud válaszolni a saját részére vonatkozó kérdésekre |
| Egyéni hozzájárulás | Commitok, issue-k, dokumentációs fejezetek alapján azonosítható legyen |
| AI-használat | Szabályos dokumentálás + a kód valódi ismerete |

⭐ = kiemelt súly

## 5. A leltározási probléma lényege (szakmai mag)

A rendszer két, **szigorúan szétválasztott** adatvilágot kezel:

- **Elvárt állomány (forrásadatok):** mi *kellene*, hogy meglegyen – Excelből importált eszközök, kódjaik, mennyiségeik,
  leltárkörzetük. Ezt admin módosíthatja, de a történetét meg kell őrizni.
- **Leltározási eredmények:** mi *került elő* egy adott leltározási időszakban – leolvasások, darabszámok, helyiség,
  ki és mikor olvasta be, direkt vagy csak a fő eszköz alapján tekintett-e meglévőnek egy kiegészítőt.

A rendszer értéke a kettő **összehasonlításában** van: megtalált / hiányzó / túlolvasott / másik körzetből előkerült /
mennyiségi eltérés – és ezek Excel-riportként exportálhatók.

Legfontosabb szakmai kihívások, amikre már most érdemes gondolni:

1. **Többféle kód ugyanahhoz az eszközhöz** – egyértelmű azonosítás, kódütközés kezelése.
2. **Mennyiséges eszközök** (pl. 30 szék egy rekordon) – túlolvasás, dupla olvasás, visszavonás.
3. **Másik leltárkörzet eszköze** – nem dobjuk el, rögzítjük, jelezzük, később lekérdezhető.
4. **Kiegészítők** – „közvetlenül ellenőrzött” vs. „fő eszköz alapján feltételezett” státusz.
5. **Helyiség** – nem a forrásrekordba kerül, hanem külön, történettel.
6. **Történetiség** – állapotváltozások, helyváltozások, felelősváltozások időbélyeggel.
7. **Vonalkódolvasó szimuláció** – billentyűzetként viselkedik; demó módban olvasó nélkül is működnie kell.
8. **Változó követelmények** – a kiírás a félév során bővülhet, ezért bővíthető architektúra kell.

## 6. Legnagyobb kockázatok

| Kockázat | Megelőzés |
|---|---|
| A dokumentáció a végére marad | Heti oldalszám-cél, minden tagnak saját fejezet |
| Egy tag „eltűnik” vagy nem teljesít | Csapatmegállapodás, issue-alapú feladatkiosztás, **azonnali jelzés az oktatónak** |
| Túl bonyolult / ismeretlen technológia | 4. alkalomra működő PoC mindhárom rétegre |
| Valaki nem érti a más (vagy AI) által írt kódot | Kódáttekintés (PR review), mindenki ismerje a teljes architektúrát |
| Az Excel-import minősége rossz | Korai adatelemzés, validáció, importhiba-riport |
| Bemutatón nem indul el a program | Bemutató előtti „próbaindítás” checklist, demó adatbázis |
