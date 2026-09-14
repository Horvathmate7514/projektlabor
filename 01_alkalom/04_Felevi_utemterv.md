# Féléves ütemterv

A hivatalos mérföldkövekre épülő belső terv. A „Fő felelős” oszlop a `03_Csoport_es_szerepek.md` szerepeire utal
(A = kliens, B = szerver, C = adat/minőség). **Minden alkalom végén** be kell mutatni a következő mérföldkőig tervezett
feladatokat is.

| Alk. | Mérföldkő | Kötelező eredmény | Dok. cél | Prez. | Fő felelős |
|---|---|---|---|---|---|
| 1. | Projektfeladat bemutatása | Csoport, szerepek, munkamegosztás, repó, csapatmegállapodás | – | – | Mindenki |
| 2. | Probléma megismerése, ötletelés | Hasonló rendszerek, funkciólista, felhasználási esetek, feltételezések, technológiajelöltek, első architektúra- és adatmodell-vázlat | ~5 old. | 5 perc | Mindenki |
| 3. | Tervezési alapok | Végleges (jóváhagyott) technológia, komponensek, adatmodell (ER), fő képernyők, leltárfolyamat, **kiegészítő funkció javaslat**, fejlesztési ütemterv | 10–15 old. | 10+5 perc | B (arch.), C (adat), A (képernyők) |
| 4. | Prototípus / PoC | Adatbázis-, backend-, GUI-PoC; működő kliens–szerver kommunikáció; rendezett repó; tesztadatok | 20 old. | 10+5 perc | Mindenki a saját rétegén |
| 5. | Forrásadatok, alapadatbázis | Excel import, eszközlista, keresés, alapszűrés | 25 old. | 10+5 perc | C (import), A (lista), B (API) |
| 6. | Leltározás alapfunkciói | Körzetválasztás, vonalkód-beolvasás + szimuláció, érvényes/érvénytelen kód, többféle kód, leolvasás mentése | 30 old. | 10+5 perc | B (logika), A (scan UI) |
| 7. | Mennyiségek, eltérő körzet | Darabszámok, elvárt vs. tényleges, hiány/többlet, másik körzet rögzítése és jelzése | 35 old. | 10+5 perc | B, A |
| 8. | Helyszín, felelős, típus | Helyiségek, sorozatos beolvasás helyiségben, felelős hozzárendelés, bővíthető típusok, szűrés ezekre | 40 old. | 10+5 perc | C (modell), A (UI) |
| 9. | Leltár-összehasonlítás | Időszakok elkülönítése, megtalált/hiányzó/többlet/más körzet/eltérés nézetek | ~40 old. | 10+5 perc | B (logika), A (nézetek) |
| 10. | Állapotok, történetiség | Logikai törlés, állapotok + időbélyeg, változástörténet lekérdezése | 45–50 old. | 10+5 perc | C (történetiség), B |
| 11. | Admin adatkezelés, export | Eszköz felvitel/módosítás, összetett szűrés, oszlopválasztás, Excel riportok | 50–55 old. | 10+5 perc | C (export), A (szűrés UI) |
| 12. | Kiegészítő funkció, integráció | Extra funkció kész, hibakezelés, GUI egységesítés, használhatóság | 55–60 old. | 10+5 perc | Mindenki |
| 13. | Tesztelés, véglegesítés | Átfogó teszt, tesztesetek dokumentálva, repó rendezve, próbaelőadás | ≥ 60 old. | 10+5 perc | C (teszt), mindenki |
| 14. | Záróprezentáció | Teljes bemutató, végleges dokumentáció | Végleges | 8–9 perc | Mindenki |

> Megjegyzés: a kiírásban a 9. alkalomnál „körülbelül 40 oldal”, míg a táblázatban 40–45 oldal szerepel – mi a
> **magasabb** értéket célozzuk, hogy legyen tartalék.

## Dokumentációs fejezetek előzetes gazdái

| Fejezet | Tartalom | Gazda |
|---|---|---|
| 1. Bevezetés | Probléma, háttér, célok, hasonló rendszerek, követelmények, felhasználási esetek, feltételezések | Mindenki (B koordinál) |
| 2. Rendszerterv | Architektúra, komponensek, adatmodell, kommunikáció, technológiák és indoklás, jogosultságok | B + C |
| 3. Dizájn és fejlesztés | GUI tervezés, implementációs döntések, import/export, kiegészítő funkció, problémák és megoldások | A + mindenki |
| 4. Tesztelés és validáció | Stratégia, tesztesetek, eredmények, hibás/rendkívüli esetek | C |
| 5. Összefoglalás | Eredmények, továbbfejlesztés, munkamegosztás, egyéni hozzájárulás, AI-használat | Mindenki |

## Minden prezentáció állandó szerkezete (10 perces változat)

1. Probléma és feladat (1 perc)
2. A rendszer célja (0,5 perc)
3. Felépítés és működés – architektúra, adatmodell (2 perc)
4. Fontosabb funkciók (1,5 perc)
5. GUI / élő demó (3 perc)
6. Technikai megvalósítás kulcselemei (1 perc)
7. Aktuális eredmények (0,5 perc)
8. Következő mérföldkő tervei (0,5 perc)

> Tipp: a diasort egyetlen, folyamatosan frissített fájlként vezessük – így minden héten csak az aktuális állapotot
> kell átvezetni, nem újrakezdeni.
