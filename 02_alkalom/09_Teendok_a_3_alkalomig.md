# Teendők a 3. alkalomig – Tervezési alapok

**Elvárás a 3. alkalomra:** végleges és jóváhagyott technológia, komponensek, architektúra v1, adatmodell, fő képernyők
tervei, leltározási folyamat terve, **kiegészítő funkció javaslat**, fejlesztési ütemterv.
**Dokumentáció:** 10–15 oldal. **Prezentáció:** 10 perc + 5 perc kérdés.

## 1. Technológia

| # | Feladat | Felelős | Kész |
|---|---|---|---|
| 1.1 | A WPF + ASP.NET Core választás indoklásának kiegészítése (csapat tapasztalata, elvetett alternatívák) | B | [ ] |
| 1.2 | Egyeztetés más csoportokkal, ki mit választ | B | [ ] |
| 1.3 | Rövid, írásos technológiai összefoglaló az oktatónak | B | [ ] |
| 1.4 | **Oktatói jóváhagyás** megszerzése, rögzítése a döntési naplóban | B | [ ] |
| 1.5 | Részdöntések (`03_alkalom/02_Technologiai_dontesek.md`) – kész, kivéve a WPF megjelenést (Bárkányi Máté) | Mindenki | [x] |
| 1.6 | Minden tag lefuttat egy minimális WPF kliens → ASP.NET Core szerver hívást | Mindenki | [ ] |

## 2. Tervezés

| # | Feladat | Felelős | Kimenet | Kész |
|---|---|---|---|---|
| 2.1 | Architektúra v1 véglegesítése (komponens- és telepítési diagram) | B | Diagram + leírás | [ ] |
| 2.2 | Adatmodell véglegesítése (`03_alkalom/03_Adatmodell_v1.md`) | C | ER-diagram + entitásleírások | [x] |
| 2.3 | Nyitott adatmodell- és architektúra-kérdések lezárása | B, C | Döntési napló bejegyzések | [ ] |
| 2.4 | Fő képernyők drótvázai: Bejelentkezés, Főoldal, Eszközlista, Eszköz részletei, **Leltározás**, Összehasonlítás, Import, Riportok, Törzsadatok | A | Figma / draw.io vázlatok | [ ] |
| 2.5 | Vizuális irány: színpaletta, visszajelzési színek (OK/más körzet/ismételt/ismeretlen), tipográfia | A | Mini stílusútmutató | [ ] |
| 2.6 | Leltározási folyamat terve: állapotdiagram a leltáridőszakra és a leolvasásra; hibás esetek táblázata | B | Diagramok + táblázat | [ ] |
| 2.7 | Jogosultsági mátrix (szerepkör × művelet) | B | Táblázat | [ ] |
| 2.8 | Az Excel oszlopainak leképezése (`03_alkalom/04_Excel_lekepezes.md`) – a mintafájl alapján pontosítandó | C | Leképezési táblázat | [~] |

## 3. Kiegészítő funkció javaslat

| # | Feladat | Felelős | Kész |
|---|---|---|---|
| 3.1 | Csapatszavazás a jelöltekről – **döntés: webkamerás beolvasás + címke- és jegyzőkönyv-generálás** | Mindenki | [x] |
| 3.2 | 1 oldalas javaslat a kiválasztott funkciókról (`03_alkalom/01_Kiegeszito_funkcio_javaslat.md`) – átnézendő | Mindenki | [ ] |
| 3.3 | Bemutatás az oktatónak a 3. alkalmon, jóváhagyás rögzítése | Koordinátor | [ ] |

## 4. Projektmenedzsment

| # | Feladat | Felelős | Kész |
|---|---|---|---|
| 4.1 | GitHub Milestone-ok létrehozása a 4–14. alkalomra (parancs: `03_alkalom/05_Fejlesztesi_utemterv.md`) | C | [ ] |
| 4.2 | Issue-k felvétele a 4. alkalom PoC feladataira (lista: `05_Fejlesztesi_utemterv.md`) | Koordinátor | [ ] |
| 4.3 | Fejlesztési ütemterv (`03_alkalom/05_Fejlesztesi_utemterv.md`) | C | [x] |
| 4.4 | Branch protection bekapcsolása a `main` ágon | Git-gazda | [ ] |

## 5. Dokumentáció (10–15 oldal)

| # | Rész | Felelős | Oldal | Kész |
|---|---|---|---|---|
| 5.1 | Bevezetés fejezet javítása a 2. alkalom visszajelzései alapján | Mindenki | 5 → 6 | [ ] |
| 5.2 | 2.1 Architektúra áttekintése | B | ~2 | [ ] |
| 5.3 | 2.2 Választott technológiák és indoklásuk (dokumentáció v0.2) | közös | ~2 | [x] |
| 5.4 | 2.3 Adatmodell (dokumentáció v0.2) | C | ~2 | [x] |
| 5.5 | 2.4 A leltározási folyamat terve | B | ~1 | [ ] |
| 5.6 | 3.1 Felhasználói felület tervezése (drótvázak) | A | ~2 | [ ] |
| 5.7 | Kiegészítő funkció javaslat | Mindenki | ~1 | [ ] |

## 6. Prezentáció (10 perc + 5 perc kérdés)

A teljes projektet az alapoktól kell bemutatni (probléma → cél → felépítés → funkciók → GUI tervek → technika →
eredmények → következő lépések), a 2. alkalom diasorának frissítésével és bővítésével.

- [ ] Diasor frissítése
- [ ] Időre próba (10 perc)
- [ ] Felkészülés a várható kérdésekre: *Miért ez a technológia? Miért így kezelitek a dupla beolvasást? Hogyan
      reprodukálható egy régi leltár? Mi a kiegészítő funkció szakmai tartalma?*
