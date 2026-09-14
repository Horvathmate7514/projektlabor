# Feltételezések (első változat)

A kiírás szerint a hiányzó részleteket **ésszerű mérnöki feltételezésekkel** kell pótolni, és ezeket rögzíteni kell.
Az alábbi lista a 2. alkalomra készült első változat; a csapat és az oktató visszajelzése alapján pontosítjuk.
A véglegesített feltételezések részletes indoklása a `repo_sablon/docs/dontesi_naplo.md` fájlba kerül.

Állapot: 🟢 elfogadott · 🟡 javaslat, megerősítendő · 🔴 oktatói válasz szükséges

## 1. Forrásadatok és import

| ID | Feltételezés | Indoklás | Állapot |
|---|---|---|---|
| F-01 | Az Excel egy sora egy **leltári tételt** jelent, amely több darabból is állhat (mennyiség oszlop). | A kiírás példája: 30 darabos székkészlet egy rekordon. | 🟡 |
| F-02 | Az **SAP szám** a forrásrendszerbeli elsődleges azonosító; ismételt importnál ezzel párosítjuk a rekordokat. | Könyvelési rendszerből származó export esetén ez a legstabilabb kulcs. | 🔴 (mintafájl kell) |
| F-03 | Ismételt import **nem töröl**: az Excelből eltűnt tételeket jelöljük, a döntést a leltárfelelős hozza meg. | Adatvesztés elkerülése, követhetőség. | 🟡 |
| F-04 | Hibás sorok (hiányzó kötelező mező, ütköző kód) **nem állítják le** a teljes importot; importhiba-riport készül. | Valós Excel-adatok ritkán hibátlanok. | 🟡 |
| F-05 | Az import a **szerveren** fut; a kliens csak feltölti a fájlt. | Üzleti logika és validáció a szerver feladata. | 🟢 |

## 2. Kódok és azonosítás

| ID | Feltételezés | Indoklás | Állapot |
|---|---|---|---|
| F-06 | Egy eszközhöz tetszőleges számú, **típussal jelölt** kód tartozhat (SAP szám, leltári szám 1, leltári szám 2, gyártási szám, belső vonalkód…). A kódtípusok bővíthetők. | A kiírás 5. pontja; bővíthetőség. | 🟢 |
| F-07 | Egy kódérték az aktív kódok között **globálisan egyedi** kell legyen. Ütközés esetén az import hibát jelez, beolvasáskor választóablak jelenik meg. | Egyértelmű azonosítás; a gyártási számok eltérő gyártóknál egyezhetnek. | 🟡 |
| F-08 | Beolvasáskor a kódot **normalizáljuk** (vezető/záró szóközök, kis-nagybetű); a vezető nullák jelentősek. | Vonalkódolvasók eltérő beállításai. | 🟡 |
| F-09 | A vonalkódolvasó a kód végén **Enter** billentyűt küld; ez zárja le a beolvasást. | A billentyűzetként működő olvasók tipikus gyári beállítása. | 🟡 |

## 3. Leltározási logika

| ID | Feltételezés | Indoklás | Állapot |
|---|---|---|---|
| F-10 | Minden beolvasás egy **eseményként** tárolódik (nem egy „megtalálva” jelzőként); a darabszámok ezekből számolódnak. | Teljes követhetőség, javíthatóság, történetiség. | 🟢 |
| F-11 | Leolvasás **nem törölhető**, csak indoklással sztornózható. | A kiírás a törlést általánosan kerüli; hivatalos leltáreredmény. | 🟢 |
| F-12 | Egyedi (elvárt 1 db) eszköz ismételt beolvasása **nem növeli** a darabszámot, csak figyelmeztet és naplóz. | A dupla beolvasás szinte mindig véletlen. | 🟡 |
| F-13 | Mennyiséges eszköznél az elvártat meghaladó beolvasás **többletként** rögzül, nem utasítjuk el. | A többlet valós helyzet lehet (pl. nyilvántartáson kívüli székek). | 🟡 |
| F-14 | Más körzethez tartozó eszköz beolvasása **megerősítés nélkül** rögzül, figyelmeztető jelöléssel. | Helyszíni munka gyorsasága; az eltérés utólag szűrhető. | 🟡 |
| F-15 | Ismeretlen kód beolvasása is **rögzül**, eszköz nélkül. | Nyilvántartásból hiányzó eszközök feltárása. | 🟡 |
| F-16 | A leltáridőszak megnyitásakor a rendszer **pillanatképet** készít az elvárt állományról (mely eszközök, milyen körzettel és mennyiséggel voltak elvártak). | Egy korábbi leltár eredménye akkor is reprodukálható, ha a forrásadatok azóta változtak. | 🟡 |
| F-17 | Egyszerre több leltáridőszak is lehet nyitva (pl. éves és rendkívüli leltár), de a beolvasó képernyőn mindig egy van kiválasztva. | Rendkívüli leltár (pl. vezetőváltáskor) valós igény. | 🟡 |
| F-18 | Lezárt leltáridőszak **csak olvasható**. | Hivatalos eredmény védelme. | 🟢 |

## 4. Kiegészítők

| ID | Feltételezés | Indoklás | Állapot |
|---|---|---|---|
| F-19 | Kiegészítő lehet **önálló leltári tétel** (saját kóddal, pl. monitor) vagy **nem leltárköteles tartozék** (pl. egér, csak leírással). | Valós nyilvántartásokban mindkettő előfordul. | 🟡 |
| F-20 | A fő eszköz beolvasásakor a kiegészítők *feltételezetten megtaláltak*; ha a kiegészítő saját kódját is beolvassák, *közvetlenül ellenőrzött* lesz. | A kiírás 7. pontja. | 🟢 |
| F-21 | A fő eszköz–kiegészítő kapcsolat érvényességi időszakkal tárolódik (egy monitor másik géphez kerülhet). | Történetiség. | 🟡 |

## 5. Hely, felelős, állapot

| ID | Feltételezés | Indoklás | Állapot |
|---|---|---|---|
| F-22 | A helyiség az **épület + szobaszám** párossal egyedi; opcionálisan emelet, megnevezés, vonalkód. | Egyetemi környezetben több épület van azonos szobaszámokkal. | 🟡 |
| F-23 | Az eszköz aktuális helye a **legutolsó érvényes helytörténeti bejegyzés**; ez leltári beolvasásból vagy kézi áthelyezésből származhat. | A forrásadat nem tartalmaz helyet (kiírás 10. pont). | 🟢 |
| F-24 | Egy eszköznek egy időben **egy** felelős személye van; a hozzárendelés időszakos. | Egyértelmű felelősség, történetiség. | 🟡 |
| F-25 | Eszközállapotok: *aktív, javítás alatt, kölcsönadva, selejtezésre javasolt, selejtezett, elveszett, ellopott, átadva (más intézménynek)*. Az állapotlista bővíthető, a megszűnt állapotok jelölővel különülnek el. | A kiírás példái + valós életciklus. | 🟡 |
| F-26 | Minden állapotváltozáshoz időbélyeg, felhasználó és **kötelező indoklás** tartozik (lopásnál opcionálisan ügyiratszám). | Követhetőség, ellenőrizhetőség. | 🟡 |

## 6. Rendszer és üzemeltetés

| ID | Feltételezés | Indoklás | Állapot |
|---|---|---|---|
| F-27 | A felhasználó adatai: név, e-mail, felhasználónév, jelszó-hash, szerepkör(ök), szervezeti egység, jogosult körzetek, aktív jelző, utolsó belépés. | A kiírás kerülendőnek tartja a csak név+jelszó modellt. | 🟡 |
| F-28 | A kliens működéséhez **hálózati kapcsolat** szükséges (az offline mód kiegészítő funkció jelölt). | Egyszerűbb alapmegoldás, a bővítés később lehetséges. | 🟡 |
| F-29 | Az időbélyegeket **UTC**-ben tároljuk, a kliens helyi időben jeleníti meg. | Egyértelműség, nyári időszámítás. | 🟢 |
| F-30 | A felhasználói felület nyelve magyar; a szövegek erőforrásfájlban vannak (később fordítható). | Célközönség; bővíthetőség. | 🟢 |
| F-31 | Személyes adatot (felelős személy) csak a feladathoz szükséges mértékben tárolunk. | Adatvédelmi (GDPR) megfontolás. | 🟢 |
| F-32 | A kliens Windows rendszerű munkaállomásokon fut. | A választott WPF csak Windowson érhető el; egyetemi irodai környezetben ez jellemző. | 🟡 |
