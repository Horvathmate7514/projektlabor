# Munkamegosztás – pontosítás

Az 1. alkalmon kiosztott elsődleges szerepek (A – kliens, B – szerver, C – adat/minőség) alapján pontosított
feladatkörök a 3–5. alkalomig tartó időszakra.

## 1. Tagok és szerepek

| Szerep | Név | Modulgazda (kódterület) | Dokumentációs fejezetek |
|---|---|---|---|
| **A** – Kliens és felhasználói élmény | Bárkányi Máté | `client/` – nézetek, ViewModel-ek, beolvasás-kezelő, API-kliens | GUI tervezés, felhasználói folyamatok, használhatóság, képernyők |
| **B** – Szerver és üzleti logika | Horváth Máté | `server/` – API, hitelesítés, leltározási és összehasonlító szolgáltatások | Architektúra, kommunikáció, üzleti szabályok, technológiai indoklás, jogosultságok |
| **C** – Adat, import/export és minőség | Kiss Barnabás | `server/` adatréteg, migrációk, import, export; `tests/` | Adatmodell, adatkezelés és történetiség, import/export, tesztelés |

## 2. Modulgazdák a teljes félévre

| Modul | Fő felelős | Társfelelős (review) |
|---|---|---|
| Hitelesítés, jogosultság | B | A |
| Eszközlista, keresés, szűrés (UI) | A | C |
| Eszközlista, szűrés (API, lekérdezések) | C | B |
| Excel import | C | B |
| Leltározó képernyő, demó mód | A | B |
| Beolvasás-minősítés, mennyiségkezelés | B | C |
| Helyiség, felelős, eszköztípus | C | A |
| Leltár-összehasonlítás (logika) | B | C |
| Leltár-összehasonlítás (nézetek) | A | B |
| Állapotok, történetiség, audit | C | B |
| Excel riportok | C | A |
| Kiegészítő funkció | *választás után döntjük el* | |
| CI, Docker Compose, README | B | C |
| Dokumentáció-összefésülés, formai ellenőrzés | *Dok.-szerkesztő* | mindenki |

> Minden tag **legalább egy teljes, végigjárható funkcióért** (kliens + szerver + adat) is felel a félév során, hogy
> a prezentációkon mindenki a teljes rendszerre vonatkozó kérdésekre is tudjon válaszolni.

## 3. Feladatok a 2. és 3. alkalom között

| Feladat | A | B | C |
|---|---|---|---|
| Technológiai részdöntések előkészítése (`05_Technologiai_osszevetes.md` 3. pont) | MVVM, WPF megjelenés | kommunikáció, hitelesítés | adatbázis, adatelérés, Excel |
| WPF + ASP.NET Core jóváhagyatása az oktatóval | | **felelős** | |
| Architektúra v1 (komponensek, felelősségek) | véleményez | **felelős** | véleményez |
| Adatmodell / ER-diagram v1 | véleményez | véleményez | **felelős** |
| Fő képernyők drótvázai (wireframe) | **felelős** | véleményez | véleményez |
| Leltározási folyamat terve (állapotok, hibás esetek) | UI-folyamat | **felelős** | adatoldal |
| Kiegészítő funkció javaslat (1 oldalas leírás) | közösen | közösen | közösen |
| Fejlesztési ütemterv GitHub Milestone-okkal | | | **felelős** |
| Dokumentáció 10–15 oldal | saját fejezetek | saját fejezetek | saját fejezetek |
| 10 perces prezentáció | GUI rész | architektúra rész | adatmodell rész |

## 4. Az egyéni hozzájárulás követése

- Minden feladat **GitHub Issue**, `felelos:A` / `felelos:B` / `felelos:C` címkével és mérföldkővel.
- Minden kódváltozás **Pull Request**, amelyet egy másik tag hagy jóvá.
- A dokumentációs fejezetek elején (munkaváltozatban) megjegyzés jelzi a szerzőt; a végleges változat
  *Munkamegosztás és egyéni hozzájárulás* fejezete ezt összesíti.
- Heti megbeszélés-jegyzet (`docs/megbeszelesek/`) rögzíti a vállalásokat és azok teljesülését.
- Havonta rövid önértékelés: mit csináltam, mit tanultam, hol akadtam el.

## 5. Becsült heti ráfordítás

| Tevékenység | Fő / hét |
|---|---|
| Fejlesztés | 4–6 óra |
| Dokumentáció | 1–2 óra |
| Megbeszélés, review | 1 óra |
| Prezentáció-előkészítés | 0,5–1 óra |
