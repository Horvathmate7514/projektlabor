# Csoport, szerepek és csapatmegállapodás

## 1. Csoporttagok

| # | Név | Neptun-kód | E-mail | GitHub felhasználónév | Telefon (opcionális) |
|---|---|---|---|---|---|
| 1 | Bárkányi Máté | ZB1FQ0 | | | |
| 2 | Horváth Máté | LIOA2U | | | |
| 3 | Kiss Barnabás | A8VIKC | | | |

**Csoport neve / azonosítója:** ____________________

**GitHub repository URL:** ____________________

## 2. Elsődleges szerepek

A kiírás szerint mindenkinek érdemi, **azonosítható** munkát kell végeznie, és **mindenkinek értenie kell a teljes
rendszert** (a prezentáción bárkitől kérdezhetnek bármit). Ezért a szerepek *elsődleges felelősséget* jelentenek,
nem kizárólagos munkaterületet.

Javasolt felosztás (az 1. alkalmon véglegesítendő):

### Szerep A – Kliens és felhasználói élmény felelős
- Asztali GUI felépítése, képernyőtervek (wireframe → mockup → implementáció)
- Vonalkódos beolvasó képernyő, demó/teszt mód
- Egységes vizuális stílus, visszajelzések, hibaüzenetek
- Kliensoldali kommunikációs réteg (szerverhívások)
- **Dokumentáció:** GUI tervezése, felhasználói folyamatok, használhatósági döntések

### Szerep B – Szerver és üzleti logika felelős
- Szerver architektúra, API/protokoll tervezése
- Leltározási logika: azonosítás többféle kóddal, mennyiségek, másik körzet, kiegészítők
- Hitelesítés, jogosultságok
- Leltár-összehasonlítás logikája
- **Dokumentáció:** architektúra, kommunikáció, üzleti szabályok, technológiai indoklás

### Szerep C – Adat, import/export és minőség felelős
- Adatmodell, adatbázis-séma, migrációk, történetiség
- Excel import (validáció, hibakezelés) és Excel riportok
- Tesztadatok, tesztelési stratégia, automatikus tesztek
- **Dokumentáció:** adatmodell, adatkezelés, tesztelés és validáció

### Közös / rotáló felelősségek

| Felelősség | Gazda | Leírás |
|---|---|---|
| Projektkoordinátor | | Heti egyeztetés szervezése, ütemterv követése, oktatóval kapcsolattartás |
| Git-gazda | | Branch-szabályok, PR-ok rendben tartása, repó rendezettsége |
| Dokumentáció-szerkesztő | | Sablon, formai egységesség, fejezetek összefésülése, oldalszám-cél követése |
| Prezentáció-koordinátor | | Diasor összeállítása, időmérés, bemutató előtti próbaindítás |
| AI-napló gazda | | Figyel arra, hogy mindenki vezesse az AI-használatot |

> A szerepek a félév során **cserélhetők**, ha a munkaterhelés ezt indokolja – ezt a dokumentáció munkamegosztási
> fejezetében rögzíteni kell.

## 3. Felelősségi mátrix (RACI)

R = végrehajtja · A = felel érte (elszámoltatható) · C = konzultál · I = tájékoztatva

| Terület | A (kliens) | B (szerver) | C (adat/minőség) |
|---|---|---|---|
| Követelmények, felhasználási esetek | R | A | R |
| Technológiaválasztás | C | A | C |
| Adatmodell | C | C | A/R |
| Szerver API | C | A/R | C |
| GUI tervek | A/R | C | C |
| Vonalkódos leltározás (end-to-end) | R | A | R |
| Excel import | I | C | A/R |
| Excel export / riportok | C | R | A |
| Leltár-összehasonlítás | R | A/R | C |
| Tesztelés | R | R | A |
| Saját kiegészítő funkció | *közösen döntjük el* | | |
| Dokumentáció | R | R | R |
| Prezentáció | R | R | R |

## 4. Csapatmegállapodás

Az alábbiakat a csoport az 1. alkalmon közösen elfogadja.

### 4.1 Kommunikáció
- Elsődleges csatorna: ____________ (pl. Discord / Teams / Messenger csoport)
- Válaszidő hétköznap: legfeljebb **24 óra**
- Feladatkövetés: **GitHub Issues + Projects tábla** (így az egyéni hozzájárulás visszakereshető)

### 4.2 Egyeztetések
- Heti belső megbeszélés: ____________ (nap, időpont, 30 perc)
- Minden megbeszélésről rövid jegyzet a repó `docs/megbeszelesek/` mappájába
- Minden labor előtt legalább **1 nappal**: prezentáció- és demópróba

### 4.3 Határidők és felelősség
- Mindenki csak olyan határidőt vállal, amit tartani tud
- Ha valaki nem tud határidőt tartani, **legalább 2 nappal előtte** jelzi
- Ha egy probléma tartósan fennáll (elérhetetlenség, nem elvégzett feladat, használhatatlan eredmény,
  felborult munkamegosztás), a csoport **haladéktalanul jelzi az oktatónak** – nem a félév végén

### 4.4 Kódolási és Git szabályok
- `main` ágra közvetlenül nem commitolunk; feature branch + Pull Request
- Minden PR-t **legalább egy másik tag** átnéz (így mindenki ismeri a többiek kódját is)
- Commit üzenetek magyarul vagy angolul, de egységesen, értelmesen (lásd `repo_sablon/CONTRIBUTING.md`)
- Titkos adat (jelszó, kulcs, connection string) **soha** nem kerül a repóba

### 4.5 AI-használat
- Az AI használata megengedett, de **mindenki vezeti** a `docs/AI_hasznalati_naplo.md` fájlt
- AI által generált kódot csak az commitolhat, aki **érti, el tudja magyarázni és módosítani tudja**
- AI-kódot is ugyanúgy review-zunk és tesztelünk

### 4.6 Dokumentáció
- Mindenki hetente hozzájárul a saját fejezeteihez
- Az oldalszám-célokat közösen követjük (lásd `04_Felevi_utemterv.md`)

### Elfogadás

| Név | Dátum | Aláírás / jóváhagyás |
|---|---|---|
| | | |
| | | |
| | | |
