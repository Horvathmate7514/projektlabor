# A 4. alkalom állapota és teendők az 5. alkalomig

Rövidítés: **A** = Bárkányi Máté (kliens), **B** = Horváth Máté (szerver), **C** = Kiss Barnabás (adat, minőség).

## 1. A 4. alkalom elvárásai – állapot (2026-09-29)

| Elvárás (kiírás) | Állapot | Megjegyzés |
|---|---|---|
| Adatbázis proof of concept | ✅ | Teljes séma, első migráció, demóadatok, SQL Serveren kipróbálva (C) |
| Backend proof of concept | ✅ | Szerver, JWT, eszközlista és részletek végpont, 13 API-teszt (B) |
| Frontend / GUI proof of concept | ❌ | A WPF kliens még üres ablak (A) |
| Első működő kliens–szerver kommunikáció | ⚠️ | A szerver oldala kész és Swaggerből kipróbálható; a kliensből még nincs hívás (A) |
| A GitHub repository megfelelő kialakítása | ⚠️ | Szerkezet, README, CI, CONTRIBUTING, AI-napló kész; a GitHub-beállítások (mérföldkövek, issue-k, `main` védelme) hiányoznak |
| A fejlesztőeszközök és könyvtárak rögzítése | ✅ | README *Főbb könyvtárak*, dokumentáció 3.3 |
| Első működő kód bemutatása | ⚠️ | Szerver + Swagger + demóadatok bemutatható; kliens nélkül |
| Tesztadatok létrehozása vagy betöltése | ✅ | `db/demoadatok.sql` |
| Dokumentáció kb. 20 oldal | ✅ | v0.4: 38 oldal (összefésült); a 3.1 GUI alfejezet és a leltárfolyamat terve hiányzik |
| 10 perces prezentáció | ✅ | `04_alkalom/Prezentacio/` – a GUI-dia helyőrző |

## 2. Az 5. alkalom elvárása

*Forrásadatok és az alapadatbázis:* Excel-import, az importált adatok adatbázisban, az eszközök megjelenítése,
keresés, alapszűrés, az adatmodell első működő változatának ellenőrzése, alapvető adatkezelési funkciók.
**Dokumentáció: kb. 25 oldal** (már teljesül, a hangsúly a tartalmi bővítésen van).

## 3. Teendők

### Pótlandó a korábbi alkalmakból

| # | Feladat | Felelős | Miért fontos |
|---|---|---|---|
| P1 | **Leltározási folyamat terve**: a leltáridőszak és a leolvasás állapotai, a hibás esetek táblázata; dokumentáció *2.6* | B | A 3. alkalom elvárása volt; a 6–7. alkalom leltárlogikája erre épül |
| P2 | D-004 kiegészítése (csapattapasztalat), az oktatói jóváhagyás rögzítése | B | A kiírás szerint a technológiát jóvá kell hagyatni |
| P3 | **WPF kliens alapjai**: bejelentkezés, token kezelése, eszközlista a meglévő végpontról | A | A 4. alkalom elvárása; nélküle a rendszer nem mutatható be végponttól végpontig |
| P4 | Drótvázak, stíluskönyvtár választása (D-016); dokumentáció *3.1* | A | A 3. alkalom elvárása; a GUI minősége kiemelt értékelési szempont |
| P5 | GitHub: mérföldkövek, issue-k, `main` ág védelme, CI-futás ellenőrzése | C + mindenki | Repository-kezelés, egyéni hozzájárulás követhetősége |
| P6 | AI-napló kitöltése (név, ellenőrzés módja) | Mindenki | Kötelező, értékelési szempont |

### Az 5. alkalom feladatai

| # | Feladat | Felelős | Kimenet |
|---|---|---|---|
| 1 | Az Excel mintafájl bekérése az oktatótól; addig saját tesztfájl a várt oszlopokkal | C | `db/minta_forrasadatok.xlsx` |
| 2 | Import szolgáltatás ClosedXML-lel: fejlécfelismerés, oszlop-hozzárendelés, validálás, próbafuttatás, tranzakciós mentés, `ImportFutas`/`ImportHiba` naplózás (`03_alkalom/04_Excel_lekepezes.md`) | C | Szolgáltatás + tesztek |
| 3 | Import végpontok: feltöltés, próbafuttatás eredménye, jóváhagyás, importnapló | B | `POST /api/importok` stb. |
| 4 | Törzsadat-végpontok: leltárkörzetek, kódtípusok, eszköztípusok, állapotok, helyiségek (a kliens szűrőihez) | B | Végpontok + tesztek |
| 5 | Szerepkör szerinti korlátozás a jogosultsági mátrix alapján | B | `[Authorize(Roles = …)]`, tesztek |
| 6 | Eszközlista szűrés bővítése: típus, állapot, helyiség, felelős; választható oszlopok előkészítése | B + C | Szűrőparaméterek, indexek |
| 7 | Kliens: eszközlista szűrőkkel, lapozással; import képernyő (fájlválasztás, eredmény, hibalista) | A | WPF nézetek |
| 8 | Import tesztesetek (`04_Excel_lekepezes.md`, 5. pont) automatikus tesztként | C | Tesztek |
| 9 | Dokumentáció: *3.5 Az Excel-import megvalósítása* (C), végpontok (B), GUI (A) | Mindenki | v0.5 |
| 10 | Prezentáció frissítése élő demóval: import → eszközlista a kliensben | Mindenki | Diasor |
