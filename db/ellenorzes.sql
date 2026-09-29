/*
================================================================================
  Leltárkezelő – az adatbázis-megszorítások ellenőrzése és leltár-összehasonlítás
  Felelős: Kiss Barnabás · 4. alkalom
--------------------------------------------------------------------------------
  1. rész: szándékosan hibás műveleteket próbál végrehajtani, és ellenőrzi, hogy
     az adatbázis elutasítja-e őket. Minden egy tranzakcióban fut, amelyet a végén
     visszagörget – az adatbázis tartalma NEM változik.
  2. rész: a 2025. évi (lezárt) leltár összehasonlítása az elvárt állománnyal.

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

ROLLBACK TRANSACTION;   -- semmi nem marad meg a próbákból

SELECT Sorszam, Ellenorzes,
       CASE WHEN Vart = Kapott THEN N'MEGFELELT' ELSE N'HIBA' END AS Eredmeny,
       Vart, Kapott, LEFT(Uzenet, 120) AS Uzenet
FROM @Eredmeny ORDER BY Sorszam;

--------------------------------------------------------------------------------
-- 2. rész: a 2025. évi leltár összehasonlítása az elvárt állománnyal
--    Az ismételt beolvasás nem számít bele a darabszámba, a sztornózott sor sem (F-11, F-12).
--------------------------------------------------------------------------------
;WITH Osszesites AS (
    SELECT t.EszkozId, t.ElvartMennyiseg,
           ISNULL(SUM(CASE WHEN l.Sztornozva = 0 AND l.Minosites <> 'ISMETELT' THEN l.Mennyiseg END), 0) AS Beolvasva,
           MAX(CASE WHEN l.Sztornozva = 0 AND l.Minosites = 'MAS_KORZET' THEN 1 ELSE 0 END) AS MasKorzetbol
    FROM dbo.ElvartTetel t
    LEFT JOIN dbo.Leolvasas l ON l.LeltarIdoszakId = t.LeltarIdoszakId AND l.EszkozId = t.EszkozId
    WHERE t.LeltarIdoszakId = 1
    GROUP BY t.EszkozId, t.ElvartMennyiseg
)
SELECT Kategoria, Tetelek FROM (
    SELECT 1 AS S, N'Elvárt tétel összesen' AS Kategoria, COUNT(*) AS Tetelek FROM Osszesites
    UNION ALL SELECT 2, N'Megtalált (elvárt = beolvasott)', COUNT(*) FROM Osszesites WHERE Beolvasva = ElvartMennyiseg
    UNION ALL SELECT 3, N'Részben megtalált (mennyiségi hiány)', COUNT(*) FROM Osszesites WHERE Beolvasva > 0 AND Beolvasva < ElvartMennyiseg
    UNION ALL SELECT 4, N'Hiányzó', COUNT(*) FROM Osszesites WHERE Beolvasva = 0
    UNION ALL SELECT 5, N'Többlet', COUNT(*) FROM Osszesites WHERE Beolvasva > ElvartMennyiseg
    UNION ALL SELECT 6, N'Más körzetből előkerült', COUNT(*) FROM Osszesites WHERE MasKorzetbol = 1
    UNION ALL SELECT 7, N'Ismeretlen kódú beolvasás', COUNT(*) FROM dbo.Leolvasas WHERE LeltarIdoszakId = 1 AND Minosites = 'ISMERETLEN_KOD'
    UNION ALL SELECT 8, N'Ismételt beolvasás (nem számít bele)', COUNT(*) FROM dbo.Leolvasas WHERE LeltarIdoszakId = 1 AND Minosites = 'ISMETELT'
    UNION ALL SELECT 9, N'Sztornózott beolvasás', COUNT(*) FROM dbo.Leolvasas WHERE LeltarIdoszakId = 1 AND Sztornozva = 1
) x ORDER BY S;
