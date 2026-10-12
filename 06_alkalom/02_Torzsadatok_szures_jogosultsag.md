# Törzsadat-végpontok, eszközlista-szűrés és láthatóság

**Felelős:** Horváth Máté · **Állapot:** elkészült (az 5. alkalom szerverfeladatainak pótlása)

Az 5. alkalom teendőlistájában (`04_alkalom/03_Teendok_az_5_alkalomig.md`) a szerverhez tartozó feladatok közül ez a
dokumentum a 4–6. pontot írja le: a kliens szűrőihez szükséges törzsadat-végpontokat, a szerepkör szerinti
korlátozást és az eszközlista bővített szűrését. A leltározási folyamat terve (P1) a
[01_Leltarozas_szerver.md](01_Leltarozas_szerver.md)-ben található.

## 1. Állapot

| # | Feladat (5. alkalom) | Állapot |
|---|---|---|
| P1 | Leltározási folyamat terve | ✅ `01_Leltarozas_szerver.md`, 2–5. pont |
| P2 | D-004: oktatói jóváhagyás rögzítése | ✅ `docs/dontesi_naplo.md` |
| 3 | Import végpontok | ✅ Kiss Barnabás készítette el az import szolgáltatással együtt – lásd [03_Excel_import.md](03_Excel_import.md), 6. pont |
| 4 | Törzsadat-végpontok | ✅ körzetek, kódtípusok, eszköztípusok, állapotok, helyiségek, felelősök |
| 5 | Szerepkör szerinti korlátozás | ✅ új végpontok szerepkör szerint, eszközlista és részletek körzet szerint |
| 6 | Eszközlista szűrés bővítése | ✅ típus (altípusokkal), állapot, helyiség, felelős; hely és felelős oszlop |

## 2. Törzsadat-végpontok

Minden bejelentkezett felhasználó olvashatja őket; a kliens a választólistákat (szűrők, körzetválasztó) ezekből tölti.

| Útvonal | Válasz | Megjegyzés |
|---|---|---|
| `GET /api/leltarkorzetek` | `LeltarkorzetAdat[]` | `leltarozhato` jelző a bejelentkezett felhasználóra |
| `GET /api/kodtipusok` | `KodTipusAdat[]` | csak aktívak |
| `GET /api/eszkoztipusok` | `EszkozTipusAdat[]` | `szuloId` alapján a kliens fát építhet; csak aktívak |
| `GET /api/eszkozallapotok` | `EszkozAllapotAdat[]` | sorrend szerint; `megszunt` jelzi a selejtezett, elveszett stb. állapotot |
| `GET /api/helyisegek` | `HelyisegAdat[]` | épület, majd szobaszám szerint; csak aktívak |
| `GET /api/felelosok` | `FelelosAdat[]` | csak név és szervezeti egység – e-mail és azonosító nem kerül ki (F-31) |

## 3. Az eszközlista szűrése

`GET /api/eszkozok?kereses=&leltarkorzetId=&eszkozTipusId=&eszkozAllapotId=&helyisegId=&felelosId=&oldal=&oldalMeret=`

| Paraméter | Jelentés |
|---|---|
| `kereses` | Részszó a megnevezésben vagy kezdőrész bármely aktív kódban (4. alkalom óta) |
| `leltarkorzetId` | Az eszköz körzete |
| `eszkozTipusId` | A típus **és minden altípusa** – pl. az „Informatika” szűrő a „Monitor” típusú eszközöket is hozza |
| `eszkozAllapotId` | Az eszköz aktuális állapota |
| `helyisegId` | Az eszköz **aktuális** helye: csak a lezáratlan (`ErvenyesIg IS NULL`) elhelyezés számít (D-011) |
| `felelosId` | Az eszköz **aktuális** felelőse, ugyanígy |

A szűrők kombinálhatók (ÉS kapcsolat). A listaelem két új mezőt kapott a választható oszlopokhoz: `helyiseg`
(„A épület A-102”) és `felelos`. A típusfa kicsi, ezért a szerver egyszer betölti, és a leszármazottakat a memóriában
gyűjti össze; a hely és a felelős allekérdezéssel kerül a listába, ezeket a szűrt egyedi indexek
(`IX_Elhelyezes_EszkozId_Aktualis`, `IX_FelelosHozzarendeles_EszkozId_Aktualis`) szolgálják ki. Új index vagy migráció
nem kellett.

## 4. Láthatóság szerepkör szerint

A jogosultsági mátrix (`04_alkalom/01_Szerver_PoC.md`, 4.1) „Eszközlista, részletek megtekintése” sora most érvényesül:

| Szerepkör | Mit lát? |
|---|---|
| Admin, Leltárfelelős | Minden eszközt |
| Leltározó | A hozzá rendelt körzetek (`FelhasznaloLeltarkorzet`) eszközeit |
| Megtekintő | Azoknak a körzeteknek az eszközeit, amelyek szervezeti egysége megegyezik a sajátjával |
| Több szerepkör | A láthatók uniója |

A listából a nem látható eszközök kimaradnak; egy nem látható eszköz részleteinek lekérése 403-at ad. A szabály a
`KorzetJogosultsag` szolgáltatásban van, ugyanott, ahol a beolvasás körzetjogosultsága, így a két szabály nem térhet el
egymástól. A beolvasás ettől független: egy más körzetbe tartozó eszköz beolvasásakor a leltározó megkapja az eszköz
adatait (a minősítés `MAS_KORZET`), mert az eltérés jelzése a cél.

## 5. Tesztek

`tests/Szerver.Tesztek/EszkozlistaSzuresTesztek.cs` – 11 teszt:

| Terület | Tesztek |
|---|---|
| Törzsadatok | mind a négy új lista tartalma, hitelesítés nélkül 401 |
| Szűrés | típus altípusokkal, állapot, helyiség (csak aktuális), felelős (a listaelem mezőivel), kombinált szűrés |
| Láthatóság | leltárfelelős mindent, leltározó csak a saját körzetét (idegen részletek 403), megtekintő a saját egységét, körzet nélküli leltározó semmit |

A teljes tesztkészlet: **85/85 sikeres**, a fordítás figyelmeztetés nélküli. A lekérdezéseket a demóadatokkal feltöltött
SQL Server adatbázison is kipróbáltuk (240 eszköz: a leltározó és a megtekintő 144-et lát, a „Bútor” típusszűrő az
altípusokkal 80-at ad).
