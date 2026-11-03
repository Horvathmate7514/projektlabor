/*
================================================================================
  Leltárkezelő – az adatbázis-megszorítások és a leltáradatok ellenőrzése
  Felelős: Kiss Barnabás · 4. alkalom, a 7. alkalomra bővítve
--------------------------------------------------------------------------------
  1. rész: szándékosan hibás műveleteket próbál végrehajtani, és ellenőrzi, hogy
     az adatbázis elutasítja-e őket. Minden egy tranzakcióban fut, amelyet a végén
     visszagörget – az adatbázis tartalma NEM változik.
  2. rész: mindegyik leltáridőszak összehasonlítása az elvárt állománnyal, körzetenként.
     A darabszám és a tételállapot a szervertől függetlenül, tisztán SQL-ből számolódik
     (D-036, D-037), így a szerver összesítő végpontjának ellenőrzésére is használható.
  3. rész: a leltáradatok belső következetessége, köztük a tárolt minősítések
     visszajátszása a szerver szabályai szerint (D-027).

  A demóadatok betöltése után futtatandó (db/demoadatok.sql).
================================================================================
*/
-- A szűrt indexek és a számított oszlop miatt kötelező beállítások (az sqlcmd alapértéke eltér)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @Eredmeny TABLE (Sorszam int IDENTITY, Ellenorzes nvarchar(200), Vart nvarchar(20), Kapott nvarchar(20), Uzenet nvarchar(400));

BEGIN TRANSACTION;

-- 1. Aktív kód ismétlése eltérő írásmóddal (szóköz, kisbetű) – a normalizált érték egyedi (D-012)
BEGIN TRY
    INSERT dbo.EszkozKod (EszkozId, KodTipusId, Ertek, Aktiv) VALUES (2, 5, N'  40000001 ', 1);
    INSERT @Eredmeny VALUES (N'Aktív kód ismétlése más írásmóddal', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Aktív kód ismétlése más írásmóddal', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 2. Ugyanaz az érték inaktív (lecserélt) kódként megengedett
BEGIN TRY
    INSERT dbo.EszkozKod (EszkozId, KodTipusId, Ertek, Aktiv) VALUES (2, 5, N'40000001', 0);
    INSERT @Eredmeny VALUES (N'Ugyanaz az érték inaktív kódként', N'elfogadva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Ugyanaz az érték inaktív kódként', N'elfogadva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 3. Második aktuális hely ugyanannak az eszköznek (D-011)
BEGIN TRY
    INSERT dbo.Elhelyezes (EszkozId, HelyisegId, ErvenyesTol, ErvenyesIg, Forras)
    SELECT TOP (1) EszkozId, HelyisegId, SYSUTCDATETIME(), NULL, 'KEZI' FROM dbo.Elhelyezes WHERE ErvenyesIg IS NULL;
    INSERT @Eredmeny VALUES (N'Két aktuális hely egy eszköznek', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Két aktuális hely egy eszköznek', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 4. Nulla elvárt mennyiség
BEGIN TRY
    UPDATE dbo.Eszkoz SET ElvartMennyiseg = 0 WHERE Id = 1;
    INSERT @Eredmeny VALUES (N'Nulla elvárt mennyiség', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Nulla elvárt mennyiség', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 5. Ismeretlen minősítés
BEGIN TRY
    UPDATE dbo.Leolvasas SET Minosites = 'ROSSZ' WHERE Id = 1;
    INSERT @Eredmeny VALUES (N'Nem létező minősítés', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Nem létező minősítés', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 6. Sztornó indoklás nélkül (UC-08)
BEGIN TRY
    UPDATE dbo.Leolvasas SET Sztornozva = 1, SztornoIdopont = SYSUTCDATETIME(), SztornoIndoklas = NULL WHERE Id = 1;
    INSERT @Eredmeny VALUES (N'Sztornó indoklás nélkül', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Sztornó indoklás nélkül', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 7. Ismeretlen kódú leolvasáshoz eszköz nem tartozhat
BEGIN TRY
    UPDATE dbo.Leolvasas SET EszkozId = 1 WHERE Minosites = 'ISMERETLEN_KOD';
    INSERT @Eredmeny VALUES (N'Ismeretlen kód eszközzel', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Ismeretlen kód eszközzel', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 8. Eszköz fizikai törlése, amíg kódok és leolvasások hivatkoznak rá (D-001, Restrict)
BEGIN TRY
    DELETE dbo.Eszkoz WHERE Id = 1;
    INSERT @Eredmeny VALUES (N'Eszköz fizikai törlése', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Eszköz fizikai törlése', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 9. Lezárt leltáridőszak lezárási időpont nélkül (F-18)
BEGIN TRY
    UPDATE dbo.LeltarIdoszak SET Lezarva = NULL WHERE Statusz = 'LEZART';
    INSERT @Eredmeny VALUES (N'Lezárt időszak lezárási idő nélkül', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Lezárt időszak lezárási idő nélkül', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 10. Eszköz saját magának kiegészítője
BEGIN TRY
    INSERT dbo.Kiegeszito (FoEszkozId, KiegeszitoEszkozId, Mennyiseg, ErvenyesTol) VALUES (1, 1, 1, SYSUTCDATETIME());
    INSERT @Eredmeny VALUES (N'Eszköz önmaga kiegészítője', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Eszköz önmaga kiegészítője', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 11. Ugyanaz az eszköz kétszer egy leltáridőszak pillanatképében
BEGIN TRY
    INSERT dbo.ElvartTetel (LeltarIdoszakId, EszkozId, LeltarkorzetId, ElvartMennyiseg, FelvitelOka)
    SELECT TOP (1) LeltarIdoszakId, EszkozId, LeltarkorzetId, ElvartMennyiseg, 'UTOLAGOS' FROM dbo.ElvartTetel;
    INSERT @Eredmeny VALUES (N'Eszköz kétszer egy pillanatképben', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Eszköz kétszer egy pillanatképben', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 12. Normalizált érték automatikus kitöltése
BEGIN TRY
    INSERT dbo.EszkozKod (EszkozId, KodTipusId, Ertek, Aktiv) VALUES (2, 5, N'  bv-proba-01 ', 1);
    IF EXISTS (SELECT 1 FROM dbo.EszkozKod WHERE ErtekNorm = N'BV-PROBA-01')
        INSERT @Eredmeny VALUES (N'Normalizált kód automatikus kitöltése', N'elfogadva', N'elfogadva', N'ErtekNorm = BV-PROBA-01');
    ELSE
        INSERT @Eredmeny VALUES (N'Normalizált kód automatikus kitöltése', N'elfogadva', N'hibás érték', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Normalizált kód automatikus kitöltése', N'elfogadva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 13. Nulla darabos leolvasás (a darabszám a leolvasásokból számolódik, D-036)
BEGIN TRY
    UPDATE dbo.Leolvasas SET Mennyiseg = 0 WHERE Id = 1;
    INSERT @Eredmeny VALUES (N'Nulla darabos leolvasás', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Nulla darabos leolvasás', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 14. Nulla elvárt mennyiség a pillanatképben
BEGIN TRY
    UPDATE dbo.ElvartTetel SET ElvartMennyiseg = 0 WHERE Id = (SELECT MIN(Id) FROM dbo.ElvartTetel);
    INSERT @Eredmeny VALUES (N'Nulla elvárt mennyiség a pillanatképben', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Nulla elvárt mennyiség a pillanatképben', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

-- 15. Ismeretlen felvételi ok az elvárt tételnél (pillanatkép vagy utólagos, D-003)
BEGIN TRY
    UPDATE dbo.ElvartTetel SET FelvitelOka = 'ROSSZ' WHERE Id = (SELECT MIN(Id) FROM dbo.ElvartTetel);
    INSERT @Eredmeny VALUES (N'Nem létező felvételi ok', N'elutasítva', N'elfogadva', NULL);
END TRY
BEGIN CATCH
    INSERT @Eredmeny VALUES (N'Nem létező felvételi ok', N'elutasítva', N'elutasítva', ERROR_MESSAGE());
END CATCH;

ROLLBACK TRANSACTION;   -- semmi nem marad meg a próbákból

SELECT Sorszam, Ellenorzes,
       CASE WHEN Vart = Kapott THEN N'MEGFELELT' ELSE N'HIBA' END AS Eredmeny,
       Vart, Kapott, LEFT(Uzenet, 120) AS Uzenet
FROM @Eredmeny ORDER BY Sorszam;

--------------------------------------------------------------------------------
-- 2. rész: a leltáridőszakok összehasonlítása az elvárt állománnyal, körzetenként
--    Beolvasott darabszám (D-036): a nem sztornózott leolvasásokból; egyedi eszköznél (elvárt = 1)
--    a mennyiségek maximuma (az ismételt leolvasás legfeljebb 1 darabot jelent, D-025),
--    különben a nem ismételt leolvasások összege.
--    Tételállapot (D-037): nem elvárt → hiányzik → más körzetben → részleges / rendben / többlet.
--------------------------------------------------------------------------------
IF OBJECT_ID(N'tempdb..#Tetel') IS NOT NULL DROP TABLE #Tetel;
SELECT x.LeltarIdoszakId AS Idoszak, x.EszkozId, t.LeltarkorzetId AS ElvartKorzet, ISNULL(t.ElvartMennyiseg, 0) AS Elvart,
       CASE WHEN t.ElvartMennyiseg = 1
            THEN MAX(CASE WHEN l.Minosites = 'ISMETELT' THEN 1 ELSE ISNULL(l.Mennyiseg, 0) END)
            ELSE SUM(CASE WHEN l.Minosites <> 'ISMETELT' THEN l.Mennyiseg ELSE 0 END) END AS Beolvasott,
       CASE WHEN t.ElvartMennyiseg = 1
            THEN MAX(CASE WHEN l.AktivKorzetId <> t.LeltarkorzetId OR l.Id IS NULL THEN 0 WHEN l.Minosites = 'ISMETELT' THEN 1 ELSE l.Mennyiseg END)
            ELSE SUM(CASE WHEN l.AktivKorzetId = t.LeltarkorzetId AND l.Minosites <> 'ISMETELT' THEN l.Mennyiseg ELSE 0 END) END AS Sajat,
       CAST(NULL AS varchar(20)) AS Allapot
INTO #Tetel
FROM (SELECT LeltarIdoszakId, EszkozId FROM dbo.ElvartTetel
      UNION
      SELECT LeltarIdoszakId, EszkozId FROM dbo.Leolvasas WHERE EszkozId IS NOT NULL AND Sztornozva = 0) x
LEFT JOIN dbo.ElvartTetel t ON t.LeltarIdoszakId = x.LeltarIdoszakId AND t.EszkozId = x.EszkozId
LEFT JOIN dbo.Leolvasas l ON l.LeltarIdoszakId = x.LeltarIdoszakId AND l.EszkozId = x.EszkozId AND l.Sztornozva = 0
GROUP BY x.LeltarIdoszakId, x.EszkozId, t.LeltarkorzetId, t.ElvartMennyiseg;

UPDATE #Tetel SET Allapot = CASE
    WHEN ElvartKorzet IS NULL THEN 'NEM_ELVART'
    WHEN Beolvasott = 0 THEN 'HIANYZIK'
    WHEN Sajat = 0 THEN 'MAS_KORZETBEN'
    WHEN Beolvasott < Elvart THEN 'RESZLEGES'
    WHEN Beolvasott = Elvart THEN 'RENDBEN'
    ELSE 'TOBBLET' END;

-- Egy eszköz mely körzetekben került elő (nem sztornózott leolvasások)
IF OBJECT_ID(N'tempdb..#Talalat') IS NOT NULL DROP TABLE #Talalat;
SELECT DISTINCT LeltarIdoszakId AS Idoszak, EszkozId, AktivKorzetId AS KorzetId
INTO #Talalat
FROM dbo.Leolvasas WHERE EszkozId IS NOT NULL AND Sztornozva = 0;

IF OBJECT_ID(N'tempdb..#Korzet') IS NOT NULL DROP TABLE #Korzet;
SELECT i.Id AS Idoszak, k.Id AS KorzetId, k.Kod AS Korzet,
       (SELECT COUNT(*) FROM #Tetel t WHERE t.Idoszak = i.Id AND t.ElvartKorzet = k.Id) AS ElvartTetelek,
       (SELECT COUNT(*) FROM #Tetel t WHERE t.Idoszak = i.Id AND t.ElvartKorzet = k.Id AND t.Allapot = 'RENDBEN') AS Rendben,
       (SELECT COUNT(*) FROM #Tetel t WHERE t.Idoszak = i.Id AND t.ElvartKorzet = k.Id AND t.Allapot = 'RESZLEGES') AS Reszleges,
       (SELECT COUNT(*) FROM #Tetel t WHERE t.Idoszak = i.Id AND t.ElvartKorzet = k.Id AND t.Allapot = 'HIANYZIK') AS Hianyzik,
       (SELECT COUNT(*) FROM #Tetel t WHERE t.Idoszak = i.Id AND t.ElvartKorzet = k.Id AND t.Allapot = 'TOBBLET') AS Tobblet,
       (SELECT COUNT(*) FROM #Tetel t WHERE t.Idoszak = i.Id AND t.ElvartKorzet = k.Id AND t.Allapot = 'MAS_KORZETBEN') AS MasKorzetben,
       (SELECT COUNT(*) FROM #Tetel t JOIN #Talalat f ON f.Idoszak = t.Idoszak AND f.EszkozId = t.EszkozId
         WHERE t.Idoszak = i.Id AND f.KorzetId = k.Id AND t.ElvartKorzet <> k.Id) AS IdeKerult,
       (SELECT COUNT(*) FROM #Tetel t JOIN #Talalat f ON f.Idoszak = t.Idoszak AND f.EszkozId = t.EszkozId
         WHERE t.Idoszak = i.Id AND f.KorzetId = k.Id AND t.Allapot = 'NEM_ELVART') AS NemElvart,
       (SELECT COUNT(*) FROM dbo.Leolvasas l
         WHERE l.LeltarIdoszakId = i.Id AND l.AktivKorzetId = k.Id AND l.EszkozId IS NULL AND l.Sztornozva = 0) AS IsmeretlenKod,
       ISNULL((SELECT SUM(t.Elvart) FROM #Tetel t WHERE t.Idoszak = i.Id AND t.ElvartKorzet = k.Id), 0) AS ElvartMennyiseg,
       ISNULL((SELECT SUM(t.Beolvasott) FROM #Tetel t WHERE t.Idoszak = i.Id AND t.ElvartKorzet = k.Id), 0) AS BeolvasottMennyiseg
INTO #Korzet
FROM dbo.LeltarIdoszak i CROSS JOIN dbo.Leltarkorzet k;

SELECT Idoszak, Korzet, ElvartTetelek, Rendben, Reszleges, Hianyzik, Tobblet, MasKorzetben, IdeKerult, NemElvart, IsmeretlenKod,
       ElvartMennyiseg, BeolvasottMennyiseg
FROM (
    SELECT 0 AS Sor, Idoszak, Korzet, ElvartTetelek, Rendben, Reszleges, Hianyzik, Tobblet, MasKorzetben, IdeKerult, NemElvart, IsmeretlenKod,
           ElvartMennyiseg, BeolvasottMennyiseg
    FROM #Korzet
    WHERE ElvartTetelek > 0 OR IdeKerult > 0 OR NemElvart > 0 OR IsmeretlenKod > 0
    UNION ALL
    SELECT 1, Idoszak, N'Összesen', SUM(ElvartTetelek), SUM(Rendben), SUM(Reszleges), SUM(Hianyzik), SUM(Tobblet), SUM(MasKorzetben),
           SUM(IdeKerult), SUM(NemElvart), SUM(IsmeretlenKod), SUM(ElvartMennyiseg), SUM(BeolvasottMennyiseg)
    FROM #Korzet GROUP BY Idoszak) o
ORDER BY Idoszak, Sor, Korzet;

--------------------------------------------------------------------------------
-- 3. rész: a leltáradatok következetessége (0 eltérés = megfelelt)
--------------------------------------------------------------------------------
DECLARE @Kov TABLE (Sorszam int IDENTITY, Ellenorzes nvarchar(200), Elteresek int);

-- Visszajátszás: minden leolvasásnak azt a minősítést kell viselnie, amelyet a szerver a beolvasás pillanatában adott volna (D-027).
-- „Eddig” = az időben korábbi, akkor még nem sztornózott, nem ismételt leolvasások összege (D-025).
;WITH Visszajatszas AS (
    SELECT l.Minosites, l.AktivKorzetId, l.Mennyiseg,
           ISNULL(t.ElvartMennyiseg, e.ElvartMennyiseg) AS Elvart,
           ISNULL(t.LeltarkorzetId, e.LeltarkorzetId) AS ElvartKorzet,
           ISNULL(a.Megszunt, 0) AS Megszunt,
           ISNULL((SELECT SUM(k.Mennyiseg) FROM dbo.Leolvasas k
                   WHERE k.LeltarIdoszakId = l.LeltarIdoszakId AND k.EszkozId = l.EszkozId AND k.Minosites <> 'ISMETELT'
                     AND (k.Idopont < l.Idopont OR (k.Idopont = l.Idopont AND k.Id < l.Id))
                     AND (k.Sztornozva = 0 OR k.SztornoIdopont > l.Idopont)), 0) AS Eddig
    FROM dbo.Leolvasas l
    JOIN dbo.Eszkoz e ON e.Id = l.EszkozId
    LEFT JOIN dbo.ElvartTetel t ON t.LeltarIdoszakId = l.LeltarIdoszakId AND t.EszkozId = l.EszkozId
    OUTER APPLY (SELECT TOP (1) ea.Megszunt FROM dbo.AllapotValtozas av
                 JOIN dbo.EszkozAllapot ea ON ea.Id = av.UjAllapotId
                 WHERE av.EszkozId = l.EszkozId AND av.Idopont <= l.Idopont
                 ORDER BY av.Idopont DESC, av.Id DESC) a
)
INSERT @Kov
SELECT N'A tárolt minősítés megegyezik a szabályok szerintivel (visszajátszás)', COUNT(*)
FROM Visszajatszas
WHERE Minosites <> CASE WHEN Elvart = 1 AND Eddig >= 1 THEN 'ISMETELT'
                        WHEN Megszunt = 1 THEN 'NEM_AKTIV'
                        WHEN ElvartKorzet <> AktivKorzetId THEN 'MAS_KORZET'
                        WHEN Eddig + Mennyiseg > Elvart THEN 'TOBBLET'
                        ELSE 'OK' END;

INSERT @Kov
SELECT N'A beolvasott kód a leolvasáshoz rendelt eszköz kódja', COUNT(*)
FROM dbo.Leolvasas l LEFT JOIN dbo.EszkozKod k ON k.Id = l.EszkozKodId
WHERE l.EszkozId IS NOT NULL
  AND (k.Id IS NULL OR k.EszkozId <> l.EszkozId OR k.ErtekNorm <> UPPER(LTRIM(RTRIM(l.BeolvasottKod))));

INSERT @Kov
SELECT N'Az ismeretlen kód valóban nem aktív kód', COUNT(*)
FROM dbo.Leolvasas l JOIN dbo.EszkozKod k ON k.ErtekNorm = UPPER(LTRIM(RTRIM(l.BeolvasottKod))) AND k.Aktiv = 1
WHERE l.Minosites = 'ISMERETLEN_KOD';

INSERT @Kov
SELECT N'A leltározó csak a hozzá rendelt körzetben olvasott be', COUNT(*)
FROM dbo.Leolvasas l
WHERE NOT EXISTS (SELECT 1 FROM dbo.FelhasznaloSzerepkor fs JOIN dbo.Szerepkor s ON s.Id = fs.SzerepkorokId
                  WHERE fs.FelhasznalokId = l.FelhasznaloId AND s.Nev IN (N'Admin', N'Leltarfelelos'))
  AND NOT EXISTS (SELECT 1 FROM dbo.FelhasznaloLeltarkorzet fk
                  WHERE fk.FelhasznaloId = l.FelhasznaloId AND fk.JogosultKorzetekId = l.AktivKorzetId);

INSERT @Kov
SELECT N'Leolvasás csak az időszak kezdete után és a lezárása előtt', COUNT(*)
FROM dbo.Leolvasas l JOIN dbo.LeltarIdoszak i ON i.Id = l.LeltarIdoszakId
WHERE i.Statusz = 'TERVEZETT' OR CAST(l.Idopont AS date) < i.Kezdete OR (i.Lezarva IS NOT NULL AND l.Idopont > i.Lezarva);

INSERT @Kov
SELECT N'A sztornó a leolvasás után történt', COUNT(*)
FROM dbo.Leolvasas WHERE Sztornozva = 1 AND SztornoIdopont < Idopont;

INSERT @Kov
SELECT N'Körzetenként a tételállapotok összege az elvárt tételek száma', COUNT(*)
FROM #Korzet WHERE Rendben + Reszleges + Hianyzik + Tobblet + MasKorzetben <> ElvartTetelek;

INSERT @Kov
SELECT N'Az összes elvárt és beolvasott darab különbsége = összes hiány − összes többlet', COUNT(*)
FROM (SELECT Idoszak,
             SUM(Elvart - Beolvasott) AS Kulonbseg,
             SUM(CASE WHEN Elvart > Beolvasott THEN Elvart - Beolvasott ELSE 0 END) AS Hiany,
             SUM(CASE WHEN Beolvasott > Elvart THEN Beolvasott - Elvart ELSE 0 END) AS Tobblet
      FROM #Tetel GROUP BY Idoszak) o
WHERE Kulonbseg <> Hiany - Tobblet;

INSERT @Kov
SELECT N'A nyitott leltárban minden tételállapotra van példa (demóadat)', 6 - COUNT(DISTINCT t.Allapot)
FROM #Tetel t JOIN dbo.LeltarIdoszak i ON i.Id = t.Idoszak WHERE i.Statusz = 'NYITOTT';

INSERT @Kov
SELECT N'A nyitott leltárban minden minősítésre van példa (demóadat)', 6 - COUNT(DISTINCT l.Minosites)
FROM dbo.Leolvasas l JOIN dbo.LeltarIdoszak i ON i.Id = l.LeltarIdoszakId WHERE i.Statusz = 'NYITOTT';

SELECT Sorszam, Ellenorzes, CASE WHEN Elteresek = 0 THEN N'MEGFELELT' ELSE N'HIBA' END AS Eredmeny, Elteresek
FROM @Kov ORDER BY Sorszam;
