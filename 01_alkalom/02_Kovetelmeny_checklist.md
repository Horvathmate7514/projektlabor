# Követelmény-checklist

A kiírás 19 pontjának kipipálható, a félév során vezetendő listája.
Az „Alkalom” oszlop azt jelzi, a hivatalos ütemterv szerint mikorra kell elkészülnie.

Jelölés: `[ ]` nincs kész · `[~]` folyamatban · `[x]` kész

---

## 1. Alapfogalmak (adatmodell) – 3. alkalomra megtervezve

| Fogalom | Első értelmezésünk (2. alkalomig pontosítandó) |
|---|---|
| Eszköz | Leltári nyilvántartásban szereplő tétel (lehet több darab is) |
| Leltári forrásadat | Az Excelből importált, elvárt állományt leíró adat |
| Leltárkörzet | Szervezeti/fizikai egység, amelyhez eszközök tartoznak és amit egyben leltároznak |
| Leltározási időszak | Egy konkrét leltár (pl. 2026. évi leltár), kezdettel, zárással, státusszal |
| Leolvasás | Egy eszköz beolvasásának eseménye: ki, mikor, hol, melyik kóddal, hány darab |
| Helyiség | Fizikai hely (épület, szobaszám), ahol a leolvasás történt |
| Felelős személy | Akinek a nevén az eszköz szerepel / aki használati szempontból felel érte |
| Eszköztípus | Bővíthető kategória (bútor, számítógép, monitor…) |
| Kiegészítő | Fő eszközhöz kapcsolódó tartozék (monitor, egér, dokkoló…) |
| Eszközkód | Az eszközhöz tartozó egy vagy több azonosító, típussal (SAP szám, leltári szám 1/2, gyártási szám…) |

## 2. Funkcionális követelmények

### 2.1 Forrásadatok importálása (5. alkalom)
- [ ] Excel állomány beolvasása
- [ ] Adatok mentése adatbázisba (a működés **nem** az Excelből történik)
- [ ] Mezőleképezés: SAP szám, leltári szám 1–2, megnevezés, gyártási szám, érték, + saját mezők
- [ ] Hibás/hiányos sorok kezelése, importnapló
- [ ] Ismételt import kezelése (duplikáció, frissítés) – *eldöntendő*

### 2.2 Vonalkódos leltározás (6. alkalom)
- [ ] Beolvasás aktív beviteli mezőbe (olvasó = billentyűzet)
- [ ] Működés olvasó nélkül: Ctrl+V / demó mód / billentyűkombinációs tesztkód-generálás
- [ ] Érvényes és érvénytelen kód kezelése, egyértelmű visszajelzés

### 2.3 Leltárkörzetek (6–7. alkalom)
- [ ] Aktív leltárkörzet kiválasztása
- [ ] Másik körzethez tartozó érvényes eszköz felismerése
- [ ] Ilyen leolvasás **rögzítése** (nem eldobása) és UI-jelzése
- [ ] Eltérés későbbi lekérdezhetősége

### 2.4 Többféle eszközkód (6. alkalom)
- [ ] Egy eszközhöz több, típusos kód
- [ ] Azonosítás bármely kód alapján
- [ ] Egyértelműség biztosítása (kódütközés kezelése)

### 2.5 Mennyiségek (7. alkalom)
- [ ] Elvárt / beolvasott / hiányzó darabszám
- [ ] Túlolvasás kezelése – *eldöntendő*
- [ ] Véletlen ismételt beolvasás kezelése – *eldöntendő*
- [ ] Leolvasás javítása / visszavonása (nyomon követhetően)

### 2.6 Kiegészítők (8. alkalom körül)
- [ ] Fő eszköz – kiegészítő kapcsolat kezelése
- [ ] Fő eszköz beolvasásakor kiegészítők alapértelmezetten megtaláltnak tekintése
- [ ] Megkülönböztethető: **közvetlenül ellenőrzött** vs. **feltételezett**

### 2.7 Leltározási időszakok (9. alkalom)
- [ ] Forrásadatok és leolvasások elkülönített tárolása
- [ ] Összehasonlítás: beolvasott / nem beolvasott / túlolvasott / másik körzetből / egyező / eltérő mennyiség
- [ ] Korábbi időszakok megőrzése

### 2.8 Eszközadatok kezelése (10–11. alkalom)
- [ ] Új eszköz, módosítás, keresés, szűrés
- [ ] **Nincs fizikai törlés**
- [ ] Állapotok: aktív, selejtezett, elveszett, ellopott, + saját (pl. javítás alatt, kölcsönadva)
- [ ] Állapotváltozás időbélyeggel, történet lekérdezhető
- [ ] Jogosultsághoz kötött szerkesztés

### 2.9 Helyiségek (8. alkalom)
- [ ] Helyiség létrehozása
- [ ] Helyiség kiválasztása után sorozatos beolvasás
- [ ] Hely a leltározási/helykezelési adatok között (**nem** a forrásrekordban)
- [ ] Helyváltozás és korábbi hely lekérdezése

### 2.10 Felelős személy (8. alkalom)
- [ ] Hozzárendelés az adatmodellben (történettel)
- [ ] Keresés és szűrés felelős szerint

### 2.11 Eszköztípusok (8. alkalom)
- [ ] Nem beégetett, bővíthető kategóriák

### 2.12 Keresés és szűrés (5., 11. alkalom)
- [ ] Szűrés: körzet, időszak, típus, helyiség, felelős, állapot, azonosító, megnevezés
- [ ] Felhasználó által választható oszlopok
- [ ] Nagy adatmennyiség kezelése (lapozás/virtualizáció)

### 2.13 Riportok és export (11. alkalom)
- [ ] Excel export: leltározott, hiányzó, többlet, más körzetből, típus szerinti, helyiségenkénti, felelősönkénti, állapot, mennyiségi eltérés
- [ ] Üzemszerűen használható formátum (fejléc, összesítés, formázás) – nem nyers dump

### 2.14 Architektúra (4. alkalomtól)
- [ ] Kliens: UI, felhasználói műveletek, megjelenítés, szerverszolgáltatások hívása
- [ ] Szerver: üzleti logika, adatkezelés, adatbázis-elérés, válaszok
- [ ] Indokolt kommunikációs protokoll

### 2.15 Bővíthetőség
- [ ] Moduláris felépítés, új követelmény újratervezés nélkül beépíthető

### 2.16 Saját kiegészítő funkció (3. alkalom: javaslat, 12. alkalom: kész)
- [ ] Ötletek gyűjtése
- [ ] Bemutatás és **oktatói jóváhagyás**
- [ ] Megvalósítás és dokumentálás

## 3. Nem funkcionális / folyamat követelmények

- [ ] GitHub repó, oktató hozzáadva, README futtatási leírással
- [ ] Nincs titok a repóban (`.env.example`, `.gitignore`)
- [ ] Folyamatos commitok mindhárom tagtól
- [ ] Kari sablon szerinti dokumentáció, oldalszám-célok tartása
- [ ] AI-használati napló vezetése
- [ ] Döntési napló (feltételezés – alternatívák – szempontok – következmények)
- [ ] Tesztelési stratégia és teszteredmények
- [ ] Munkamegosztás és egyéni hozzájárulás dokumentálása

## 4. A dokumentációban kötelezően megjelenő témák

- [ ] Probléma és háttér
- [ ] Követelmények és felhasználási esetek
- [ ] Alkalmazott feltételezések
- [ ] Architektúra és komponensek
- [ ] Adatmodell és adatkezelés
- [ ] Kliens és szerver működése
- [ ] Kommunikáció megvalósítása
- [ ] Technológiák és indoklásuk
- [ ] GUI tervezése
- [ ] Fontosabb implementációs döntések
- [ ] Tesztelési stratégia és eredmények
- [ ] Felmerült problémák és megoldásuk
- [ ] Munkamegosztás
- [ ] Egyéni hozzájárulások
- [ ] Kiegészítő feladatok
- [ ] Mesterséges intelligencia használata

## 5. Első körben eldöntendő kérdések (2–3. alkalomig)

| # | Kérdés | Lehetséges irányok |
|---|---|---|
| 1 | Kliens technológia | WPF / Avalonia / Qt / JavaFX / PySide |
| 2 | Szerver technológia | ASP.NET Core / Spring Boot / FastAPI / Qt-alapú szerver |
| 3 | Kommunikáció | REST (HTTP+JSON) / gRPC / WebSocket (valós idejű leolvasás-frissítés) |
| 4 | Adatbázis | PostgreSQL / MS SQL / MySQL / SQLite (csak fejlesztéshez) |
| 5 | Felhasználók és szerepkörök | pl. Adminisztrátor, Leltárfelelős, Leltározó, Megtekintő |
| 6 | Hitelesítés | Saját felhasználókezelés + token (JWT) / egyetemi SSO szimulálása |
| 7 | Túlolvasás | Engedjük és jelöljük / figyelmeztetés + megerősítés / tiltás |
| 8 | Dupla olvasás | Időablakos figyelmeztetés / visszavonás gomb / leolvasásnapló javítással |
| 9 | Történetiség | Eseménytábla (audit log) / temporális (valid_from–valid_to) táblák |
| 10 | Offline leltározás | Kell-e, ha megszakad a hálózat? (lehetséges kiegészítő funkció) |
