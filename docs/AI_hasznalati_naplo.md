# Mesterséges intelligencia használati napló

A kiírás szerint a dokumentációban fel kell tüntetni:
- milyen AI-eszközt használtunk;
- milyen célra;
- milyen jellegű segítséget adott;
- mely részek elkészítésében vagy módosításában;
- hogyan ellenőriztük az eredmény helyességét.

> **Fontos:** az AI által generált eredményért a hallgató felel. Aki a saját kódját nem érti, nem tudja megmagyarázni
> vagy módosítani, **elégtelen (1)** osztályzatot kap. Csak olyan, AI segítségével készült részt commitolj, amelyet
> teljes egészében értesz, és a bemutatón el tudsz magyarázni.

## Napló

A *Név* és az *Ellenőrzés módja* oszlopot mindig annak kell kitöltenie, aki az eszközt használta.

| Dátum | Név | AI-eszköz | Cél, feladat | A segítség jellege | Érintett rész | Ellenőrzés módja |
|---|---|---|---|---|---|---|
| 2026-09-14 | *(kitöltendő)* | Claude (Claude Code) | Az 1–2. alkalom anyagai: a kiírás értelmezése, szerepek, ütemterv, hasonló rendszerek, funkciólista, felhasználási esetek, feltételezések, technológiai összevetés, architektúra- és adatmodell-vázlat | Összefoglalás, ötletelés, szövegezés, sablonkészítés | `01_alkalom/`, `02_alkalom/` (dokumentáció v0.1, prezentáció) | *(kitöltendő – pl. összevetés a kiírással, a források ellenőrzése, átdolgozás csapatmegbeszélésen)* |
| 2026-09-22 | *(kitöltendő)* | Claude (Claude Code) | A dokumentáció összevetése a szakdolgozati követelményekkel, javítások; a prezentáció jegyzetei és oldalszámai | Ellenőrzés, szövegezés | `02_alkalom/` | *(kitöltendő)* |
| 2026-09-24 | *(kitöltendő)* | Claude (Claude Code) | A 3. alkalom közös része és az adatmodell: kiegészítő funkció javaslat, technológiai döntések, döntési napló, adatmodell v1, Excel-leképezés, ütemterv, dokumentáció v0.2 | Ötletelés, tervezési javaslatok, szövegezés; a döntéseket a csapat hozta meg | `03_alkalom/`, `docs/dontesi_naplo.md` | *(kitöltendő)* |
| 2026-09-29 | *(kitöltendő)* | Claude (Claude Code) | A 4. alkalom adatbázis-része: az adatmodell hiányzó entitásai, EF Core konfigurációk, első migráció, kezdeti adatok, demóadat-szkript, megszorítás-ellenőrző szkript, adatmodell-tesztek, CI; közös rész: dokumentáció v0.4 összefésülése, AI-napló, közreműködési szabályok, teendők, prezentáció | Kódgenerálás, tesztgenerálás, szövegezés | `src/Szerver/Adat/`, `src/Kozos/Leltar/`, `db/`, `tests/…/AdatmodellTesztek.cs`, `.github/workflows/`, `04_alkalom/` | Az AI által végzett ellenőrzések: fordítás 0 figyelmeztetéssel, 34/34 teszt, a migráció és a demóadatok futtatása SQL Server 2025-ön, 12/12 megszorítás-ellenőrzés, a szerver indítása a feltöltött adatbázissal. *(Kitöltendő: a saját átnézés, kódmagyarázat, módosítás.)* |

### A segítség jellegének kategóriái (egységes kitöltéshez)
- **Ötletelés** – funkciók, alternatívák felvetése
- **Magyarázat** – technológia, fogalom, hibaüzenet megértése
- **Kódgenerálás** – kódrészlet előállítása
- **Kódmódosítás, refaktorálás**
- **Hibakeresés**
- **Tesztgenerálás**
- **Szövegezés** – a dokumentáció nyelvi javítása, strukturálása
- **Összefoglalás** – a kiírás vagy a szakirodalom kivonatolása

### Ellenőrzési módok
- Kézi kódátnézés és megértés, átnézés egy másik csapattaggal (pull request)
- Futtatás és kézi kipróbálás
- Automatikus teszt
- Összevetés a hivatalos dokumentációval vagy a szakirodalommal
- Összevetés a feladatkiírással

### Felkészülés a bemutatóra
Az AI segítségével készült kód minden részéről a felelős tagnak tudnia kell válaszolni legalább ezekre:
mit csinál, miért így, milyen alternatíva lett volna, és hogyan módosítaná egy új követelmény esetén.
