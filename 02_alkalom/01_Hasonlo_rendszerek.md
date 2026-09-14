# Hasonló rendszerek vizsgálata

**Cél:** megérteni, hogyan oldják meg a valós eszköznyilvántartó és készletkezelő rendszerek azokat a problémákat,
amelyekkel nekünk is foglalkoznunk kell, és ebből tanulságokat levonni a saját tervezéshez.

> A leírások a rendszerek nyilvános dokumentációja és termékoldalai alapján készültek. A dokumentációba kerülés előtt
> a felelős tag **ellenőrizze a forrásokat** (verzió, dátum), és hivatkozza őket az irodalomjegyzékben.

## 1. A vizsgált rendszerek

| Rendszer | Típus | Fő fókusz | Licenc / elérés |
|---|---|---|---|
| **Snipe-IT** | Webes eszköznyilvántartó | IT-eszközök nyilvántartása, kiadása, auditja | Nyílt forráskódú (AGPL), saját üzemeltetés vagy felhő |
| **GLPI** | Webes ITSM + eszközkezelő | IT-eszközök automatikus leltára, helpdesk | Nyílt forráskódú (GPL) |
| **Odoo Inventory + Barcode** | ERP modul | Raktárkészlet, fizikai leltár, vonalkódos műveletek | Community (nyílt) / Enterprise (fizetős) |
| **SAP Asset Accounting (FI-AA)** | Vállalatirányítási rendszer modulja | Befektetett eszközök könyvelése, értékcsökkenés | Kereskedelmi |
| **Sortly** | Felhőalapú (SaaS), mobil-első | Kisvállalati készlet- és eszköznyilvántartás | Előfizetéses |

## 2. Részletes megfigyelések

### 2.1 Snipe-IT
- Minden eszköznek van egy **eszközcímke (asset tag)**, emellett gyári szám; ezekből vonalkód/QR-címke nyomtatható.
- Eszközök **kiadhatók (check-out)** felhasználónak, helyszínnek vagy másik eszköznek, és visszavehetők.
- **Audit funkció:** eszközönként rögzíthető az utolsó és a következő ellenőrzés időpontja; a lejárt auditok listázhatók.
- Eszközkategóriák, modellek, gyártók; **egyedi mezők** (custom fields) kategóriánként.
- Külön kezeli az egyedi eszközöket és a **mennyiséges tételeket** (tartozékok, fogyóeszközök, alkatrészek).
- Minden műveletről **eseménynapló** (action log) készül.
- CSV-import, REST API, LDAP/SAML bejelentkezés.

**Gyengeségek a mi szempontunkból:** webes (nálunk tilos), nincs leltározási *időszak* fogalom és elvárt/tényleges
összehasonlítás; az eszköz fő azonosítója egyetlen címke.

### 2.2 GLPI
- Az IT-eszközöket egy telepített **ügynök (GLPI Agent)** automatikusan feltérképezi és frissíti.
- **Entitások** hierarchiája: több szervezeti egység ugyanabban a rendszerben, egymástól elkülönített jogosultsággal.
- Helyszínek hierarchiája, pénzügyi és adminisztratív adatok (leltári szám, beszerzés, garancia).
- Minden objektumnak **teljes módosítástörténete** van (ki, mikor, melyik mezőt mire változtatta).
- Részletes, **profil alapú jogosultságkezelés**.

**Gyengeségek:** IT-központú, bútorok/mérőműszerek fizikai leltározására kevésbé alkalmas; összetett, nehezen tanulható felület.

### 2.3 Odoo Inventory + Barcode
- **Fizikai leltár:** a rendszer a „nyilvántartott” és a „megszámolt” mennyiséget egymás mellett mutatja, a különbséget
  jóváhagyás után könyveli.
- **Hierarchikus raktárhelyek** (raktár → sor → polc).
- Vonalkódos alkalmazás: a vonalkódolvasó billentyűzetként vagy mobilkamerával is használható.
- Egy termékhez a termék és a **csomagolási egység** saját vonalkódja is tartozhat (pl. 1 db vs. 12 db-os karton).
- Tétel- és gyári szám (lot/serial) követés.

**Gyengeségek:** fogyó/raktári készletre optimalizált, a tárgyi eszköz felelőse, állapottörténete kevésbé hangsúlyos;
teljes ERP bevezetést feltételez.

### 2.4 SAP Asset Accounting (FI-AA)
- A befektetett eszközöket **eszközkarton** írja le: **főszám és alszám** (egy eszköz részei, bővítései alszámon).
- Az eszközkartonon **leltári szám**, utolsó leltár dátuma, leltári megjegyzés rögzíthető.
- Az olyan adatok, mint a költséghely, telephely és **helyiség**, **időfüggően** tárolódnak – a korábbi értékek
  visszakereshetők.
- Könyvelési fókusz: értékcsökkenés, aktiválás, selejtezés (kivezetés) könyvelése.
- A fizikai leltár támogatása jellemzően listák/riportok formájában történik, a helyszíni beolvasást külső megoldás végzi.

**Tanulság:** a feladatban szereplő **„SAP szám”** arra utal, hogy a forrás Excel egy ilyen könyvelési rendszer
exportja. A mi rendszerünk nem helyettesíti, hanem **kiegészíti** azt a helyszíni leltározással.

### 2.5 Sortly
- Mobil-első felület, **mappák** (helyszínek) szerinti rendezés, fotók az eszközökhöz.
- Mennyiség eszközönként, **alacsony készlet riasztás**.
- Vonalkód/QR-címkék generálása a rendszerből.
- Egyedi mezők, tevékenységnapló, egyszerű riportok, export.

**Gyengeségek:** felhőben tárolt adatok (egyetemi adatkezelés szempontjából kérdéses), kisvállalati méretre szabott,
nincs leltárkörzet és leltárösszehasonlítás.

## 3. Összehasonlítás a mi követelményeinkkel

✅ = jól támogatott · ◐ = részben · ✗ = nem jellemző

| Követelmény | Snipe-IT | GLPI | Odoo | SAP FI-AA | Sortly |
|---|---|---|---|---|---|
| Excel/CSV import | ✅ | ✅ | ✅ | ✅ | ✅ |
| Vonalkódos beolvasás | ◐ | ◐ | ✅ | ✗ | ✅ |
| Több kód egy eszközhöz | ◐ | ◐ | ◐ | ◐ | ✗ |
| Mennyiséges tételek | ◐ | ✗ | ✅ | ◐ | ✅ |
| Leltárkörzet | ◐ | ◐ (entitás) | ◐ (raktár) | ◐ | ✗ |
| Leltározási időszak + összehasonlítás | ✗ | ✗ | ✅ | ◐ | ✗ |
| Kiegészítők kapcsolata | ✅ | ✅ | ✗ | ◐ (alszám) | ✗ |
| Helyiség és helytörténet | ◐ | ◐ | ✅ | ✅ | ◐ |
| Felelős személy | ✅ | ✅ | ✗ | ◐ | ✗ |
| Állapotok, logikai törlés | ✅ | ✅ | ◐ | ✅ | ◐ |
| Teljes módosítástörténet | ✅ | ✅ | ◐ | ✅ | ◐ |
| Asztali kliens | ✗ | ✗ | ✗ | ◐ (SAP GUI) | ✗ |

**Következtetés:** egyik vizsgált rendszer sem fedi le egyszerre a *helyszíni, vonalkódos, körzet- és időszakalapú
leltározást* és a *hosszú távú, történeti eszköznyilvántartást*. Ez indokolja egy célrendszer fejlesztését.

## 4. Tanulságok, amelyeket beépítünk

| # | Tanulság | Honnan | Hatás a tervünkre |
|---|---|---|---|
| T1 | Minden változás legyen eseményként naplózva | GLPI, Snipe-IT | Audit napló + állapot-/hely-/felelősváltozás táblák |
| T2 | Az időfüggő adatok (hely, felelős) érvényességi időszakkal tárolódjanak | SAP FI-AA | `ervenyes_tol` / `ervenyes_ig` mezők |
| T3 | A nyilvántartott és a megszámolt mennyiség egymás mellett jelenjen meg | Odoo | Összehasonlító nézet: elvárt / beolvasott / eltérés |
| T4 | Egy tételhez több vonalkód tartozhat | Odoo | Külön `EszkozKod` tábla kódtípussal |
| T5 | Egyedi eszköz és mennyiséges tétel eltérő kezelést igényel | Snipe-IT | Leolvasási szabályok eszközönkénti elvárt mennyiség alapján |
| T6 | A fő eszköz és részei/tartozékai kapcsolata legyen explicit | SAP (alszám), Snipe-IT | Kiegészítő-kapcsolat tábla |
| T7 | Címkenyomtatás a hiányzó/sérült címkék pótlására | Snipe-IT, Sortly | Kiegészítő funkció jelölt |
| T8 | Egyszerű, gyors beolvasó felület a helyszíni munkához | Sortly, Odoo Barcode | Letisztult, nagy visszajelzésű leltározó képernyő |
| T9 | Szervezeti egységenként elkülönített jogosultság | GLPI | Szerepkörök + körzethez kötött jogosultság |

## 5. Források (ellenőrizendő, dokumentációban hivatkozandó)

- Snipe-IT – https://snipeitapp.com/ és https://snipe-it.readme.io/
- GLPI – https://glpi-project.org/ és https://glpi-user-documentation.readthedocs.io/
- Odoo Inventory / Barcode – https://www.odoo.com/documentation/ (Inventory & MRP → Inventory, Barcode)
- SAP Asset Accounting – https://help.sap.com/ (Asset Accounting, asset master data)
- Sortly – https://www.sortly.com/
