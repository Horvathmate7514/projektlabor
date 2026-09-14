# Funkciólista (priorizált)

Priorizálás **MoSCoW** módszerrel:
- **Must** – kötelező, a kiírásból következik
- **Should** – nem kötelező, de egy valós használatra szánt rendszerben indokolt
- **Could** – kiegészítő funkció jelöltek (oktatói jóváhagyás szükséges)
- **Won't** – ebben a félévben tudatosan nem valósítjuk meg

## 1. Must – kötelező funkciók

| ID | Funkció | Kiírás pont | Tervezett alkalom |
|---|---|---|---|
| M01 | Bejelentkezés, szerepkör alapú hozzáférés | 18 | 4–5. |
| M02 | Excel forrásállomány importálása adatbázisba, importnaplóval | 2 | 5. |
| M03 | Eszközlista megjelenítése, keresés, alapszűrés | 13 | 5. |
| M04 | Leltárkörzetek kezelése, aktív körzet kiválasztása | 4 | 6. |
| M05 | Leltározási időszak létrehozása, megnyitása, lezárása | 8 | 6. |
| M06 | Vonalkódos beolvasás (olvasó = billentyűzet) | 3 | 6. |
| M07 | Beolvasás szimulálása olvasó nélkül (Ctrl+V, demó mód, tesztkód-generálás) | 3 | 6. |
| M08 | Eszköz azonosítása bármely hozzárendelt kód alapján | 5 | 6. |
| M09 | Érvénytelen / ismeretlen kód kezelése | 3, 18 | 6. |
| M10 | Mennyiségek: elvárt / beolvasott / hiányzó / többlet | 6 | 7. |
| M11 | Ismételt és túlbeolvasás kezelése, leolvasás javítása | 6 | 7. |
| M12 | Más leltárkörzetből beolvasott eszköz rögzítése és jelzése | 4 | 7. |
| M13 | Helyiségek kezelése, sorozatos beolvasás helyiségben | 10 | 8. |
| M14 | Helytörténet (áthelyezés, korábbi hely lekérdezése) | 10 | 8., 10. |
| M15 | Felelős személy hozzárendelése, keresés/szűrés | 11 | 8. |
| M16 | Bővíthető eszköztípusok | 12 | 8. |
| M17 | Kiegészítők kapcsolata, közvetlen vs. feltételezett ellenőrzés | 7 | 8. |
| M18 | Leltár-összehasonlítás a forrásadatokkal | 8 | 9. |
| M19 | Eszközállapotok (aktív, selejtezett, elveszett, ellopott…), logikai törlés, időbélyeg | 9 | 10. |
| M20 | Állapot- és adattörténet lekérdezése | 9 | 10. |
| M21 | Eszköz felvitele és módosítása (jogosultsághoz kötve) | 9 | 11. |
| M22 | Összetett szűrés, választható oszlopok | 13 | 11. |
| M23 | Excel riportok (hiányzó, többlet, más körzet, helyiség, felelős, típus, állapot, eltérés) | 14 | 11. |
| M24 | Legalább egy saját kiegészítő funkció | 17 | 12. |

## 2. Should – életszerűséget növelő funkciók

| ID | Funkció | Miért indokolt? |
|---|---|---|
| S01 | **Eseményalapú audit napló** minden adatmódosításról | Követhetőség, vitás esetek visszakeresése |
| S02 | **Importhiba-riport** (hibás, hiányos, ütköző kódú sorok) | A valós Excel-adatok ritkán hibátlanok |
| S03 | Ismételt import (frissítés) változásjelentéssel | A forrásrendszer adatai év közben változnak |
| S04 | **Azonnali, jól látható visszajelzés** beolvasáskor (szín, ikon, hang) | A leltározó nem a képernyőt nézi, hanem az eszközt |
| S05 | Mennyiség megadása egyszerre (pl. „+30 db”) | 30 szék egyenkénti beolvasása életszerűtlen |
| S06 | Ismeretlen kódok listája és utólagos eszközhöz rendelése | Nyilvántartásból hiányzó eszközök feltárása |
| S07 | Leltár előrehaladás (%) körzetenként, helyiségenként | A leltárfelelős lássa, hol tart a munka |
| S08 | Lezárt leltáridőszak zárolása (csak olvasható) | Hivatalos leltáreredmény ne változzon utólag |
| S09 | Felhasználónként mentett nézet-beállítások (oszlopok, szűrők) | Nagy adatmennyiségnél gyorsítja a munkát |
| S10 | Selejtezett/elveszett eszköz előkerülésének jelzése | Az állapot javítását igényli |
| S11 | Billentyűzetről teljesen kezelhető leltározó képernyő | A beolvasó mellett nincs idő egerezni |

## 3. Could – kiegészítő funkció jelöltek

A 3. alkalomra ezekből választunk 1–2-t az oktatónak bemutatandó javaslathoz.

| ID | Kiegészítő funkció | Leírás | Szakmai többlet | Becsült méret | Kockázat |
|---|---|---|---|---|---|
| K1 | **Offline leltározás és szinkronizáció** | Hálózat nélkül (pl. pincében, rossz wifi mellett) is lehessen beolvasni; a kliens helyben tárolja a leolvasásokat, és visszatéréskor szinkronizál, ütközéskezeléssel | Elosztott adatkezelés, ütközésfeloldás, helyi adatbázis | Nagy | Közepes–magas |
| K2 | **Valós idejű, több leltározós munka** | Több kolléga párhuzamosan leltároz; élő előrehaladás-kijelző és figyelmeztetés, ha ugyanazt az eszközt más már beolvasta | Szerver→kliens push (.NET alatt pl. SignalR), konkurenciakezelés | Közepes | Közepes |
| K3 | **Vonalkódcímke-generálás és nyomtatás** | Hiányzó vagy sérült címkék pótlása; címkeív PDF-be, helyiségenként vagy kijelölt eszközökre | Vonalkód-szabványok, nyomtatási elrendezés | Kicsi–közepes | Alacsony |
| K4 | **Leltárjegyzőkönyv generálása** | Lezárt időszakról hivatalos, aláírásra alkalmas PDF jegyzőkönyv (bizottság, eltérések, indoklások) | Dokumentumgenerálás, hivatalos folyamat támogatása | Közepes | Alacsony |
| K5 | **Eszközmozgatási / selejtezési kérelem munkafolyamat** | Kérelem → jóváhagyás → végrehajtás, értesítésekkel | Állapotgép, jogosultsági lánc | Közepes | Közepes |
| K6 | **Anomália- és trendriport** | Évek óta elő nem kerülő, gyakran vándorló vagy mindig más körzetből előkerülő eszközök kiemelése, idősoros diagramokkal | Történeti adatelemzés, vizualizáció | Közepes | Alacsony |
| K7 | **Helyiségtérkép / alaprajz nézet** | Épületszintenkénti alaprajzon színekkel jelzett leltárállapot | Egyedi grafikus komponens | Nagy | Magas |

**A választás a csapat döntése a 3. alkalomig** (csapatszavazás a fenti jelöltekről, vagy saját ötlet).

## 4. Won't – ebben a félévben nem

| Funkció | Indok |
|---|---|
| Webes vagy böngészős kliens | A kiírás tiltja |
| Élő integráció a forrásrendszerrel (SAP) | Nincs hozzáférés; az Excel-import a kijelölt adatátadási mód |
| RFID alapú leltározás | Hardverigény, nem része a feladatnak |
| Könyvelési funkciók (értékcsökkenés) | A forrásrendszer feladata |
| Natív mobilalkalmazás | Az asztali kliens a követelmény; későbbi továbbfejlesztési lehetőség |
