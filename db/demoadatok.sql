/*
================================================================================
  Leltárkezelő – demó- és tesztadatok
  Felelős: Kiss Barnabás · 4. alkalom
--------------------------------------------------------------------------------
  - Determinisztikus: nincs benne véletlenszám, minden érték az eszköz sorszámából
    számolódik, így minden futtatás ugyanazt az adatot adja (bemutató, tesztek).
  - CSAK fejlesztői vagy demó adatbázisba! Üres, frissen migrált adatbázist vár
    (`dotnet ef database update`), különben hibával leáll, és nem ír semmit.
  - Egyetlen tranzakcióban fut: hiba esetén semmi sem marad félkészen.

  Tartalom: 5 felhasználó, 6 leltárkörzet, 20 eszköztípus, 38 helyiség,
  14 felelős személy, 240 eszköz kódokkal, kiegészítők, hely- és felelőstörténet,
  állapotváltozások, egy lezárt (2025) és egy nyitott (2026) leltáridőszak
  leolvasásokkal, egy importfutás hibákkal.

  Demó felhasználók (szerepkör): admin (Admin), leltarfelelos (Leltarfelelos),
  leltarozo1, leltarozo2 (Leltarozo), megtekinto (Megtekinto).
  Közös demójelszó: Demo-Jelszo-2026 – kizárólag demó adatbázisban használható!
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
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Eszkoz') IS NULL
    THROW 50001, N'Az adatbázis nincs migrálva: előbb futtasd a "dotnet ef database update" parancsot.', 1;
IF EXISTS (SELECT 1 FROM dbo.Eszkoz) OR EXISTS (SELECT 1 FROM dbo.Felhasznalo) OR EXISTS (SELECT 1 FROM dbo.Leltarkorzet)
    THROW 50002, N'Az adatbázis már tartalmaz adatot. A demószkript csak üres, frissen migrált adatbázisba tölthető.', 1;

BEGIN TRANSACTION;

-- A .NET PasswordHasher (PBKDF2) által előállított hash a közös demójelszóhoz.
DECLARE @JelszoHash nvarchar(200) = N'AQAAAAIAAYagAAAAED9SbswAAJppZ4t/RDN6Wzz2E8u8/mCOhwi1rjSWg02/nZg46bWX9b3aZdjfqLTFGg==';

-- Segédtábla: számok 1-től 1000-ig
CREATE TABLE #N (n int PRIMARY KEY);
INSERT #N (n)
SELECT a.v * 100 + b.v * 10 + c.v + 1
FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) a(v)
CROSS JOIN (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) b(v)
CROSS JOIN (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) c(v);

--------------------------------------------------------------------------------
-- 1. Leltárkörzetek, felhasználók, jogosultságok
--------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.Leltarkorzet ON;
INSERT dbo.Leltarkorzet (Id, Kod, Nev, SzervezetiEgyseg, Aktiv) VALUES
 (1, N'MIK-A', N'Mérnöki Kar – A épület',             N'Mérnöki Kar',           1),
 (2, N'MIK-B', N'Mérnöki Kar – B épület',             N'Mérnöki Kar',           1),
 (3, N'MIK-I', N'Mérnöki Kar – I épület (laborok)',   N'Mérnöki Kar',           1),
 (4, N'GTK-G', N'Gazdaságtudományi Kar – G épület',   N'Gazdaságtudományi Kar', 1),
 (5, N'KONYV', N'Egyetemi Könyvtár',                  N'Egyetemi Könyvtár',     1),
 (6, N'KOZP',  N'Központi Igazgatás',                 N'Gazdasági Igazgatóság', 1);
SET IDENTITY_INSERT dbo.Leltarkorzet OFF;

SET IDENTITY_INSERT dbo.Felhasznalo ON;
INSERT dbo.Felhasznalo (Id, Felhasznalonev, Nev, Email, JelszoHash, SzervezetiEgyseg, Aktiv) VALUES
 (1, N'admin',         N'Rendszeradminisztrátor', N'admin@demo.local',         @JelszoHash, N'Informatikai Igazgatóság', 1),
 (2, N'leltarfelelos', N'Demó Leltárfelelős',     N'leltarfelelos@demo.local', @JelszoHash, N'Gazdasági Igazgatóság',    1),
 (3, N'leltarozo1',    N'Demó Leltározó 1',       N'leltarozo1@demo.local',    @JelszoHash, N'Mérnöki Kar',              1),
 (4, N'leltarozo2',    N'Demó Leltározó 2',       N'leltarozo2@demo.local',    @JelszoHash, N'Gazdaságtudományi Kar',    1),
 (5, N'megtekinto',    N'Demó Megtekintő',        N'megtekinto@demo.local',    @JelszoHash, N'Mérnöki Kar',              1);
SET IDENTITY_INSERT dbo.Felhasznalo OFF;

INSERT dbo.FelhasznaloSzerepkor (FelhasznalokId, SzerepkorokId)
SELECT v.fid, s.Id
FROM (VALUES (1, N'Admin'), (2, N'Leltarfelelos'), (3, N'Leltarozo'), (4, N'Leltarozo'), (5, N'Megtekinto')) v(fid, nev)
JOIN dbo.Szerepkor s ON s.Nev = v.nev;

-- leltarozo1 a Mérnöki Kar körzeteiben, leltarozo2 a többiben leltározhat
INSERT dbo.FelhasznaloLeltarkorzet (FelhasznaloId, JogosultKorzetekId) VALUES
 (3, 1), (3, 2), (3, 3), (4, 4), (4, 5), (4, 6);

--------------------------------------------------------------------------------
-- 2. Eszköztípusok (bővíthető hierarchia)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.EszkozTipus ON;
INSERT dbo.EszkozTipus (Id, Nev, SzuloId, Aktiv) VALUES
 (1, N'Bútor', NULL, 1), (2, N'Informatika', NULL, 1), (3, N'Mérőműszer', NULL, 1),
 (4, N'Oktatástechnika', NULL, 1), (5, N'Egyéb', NULL, 1),
 (6, N'Szék', 1, 1), (7, N'Asztal', 1, 1), (8, N'Szekrény, polc', 1, 1),
 (9, N'Asztali számítógép', 2, 1), (10, N'Laptop', 2, 1), (11, N'Monitor', 2, 1), (12, N'Nyomtató', 2, 1),
 (13, N'Hálózati eszköz', 2, 1), (14, N'Számítógépes tartozék', 2, 1),
 (15, N'Oszcilloszkóp', 3, 1), (16, N'Multiméter', 3, 1), (17, N'Labortápegység', 3, 1),
 (18, N'Projektor', 4, 1), (19, N'Interaktív tábla', 4, 1), (20, N'Vetítővászon', 4, 1);
SET IDENTITY_INSERT dbo.EszkozTipus OFF;

--------------------------------------------------------------------------------
-- 3. Helyiségek (38 db, körzetenként egy épület)
--------------------------------------------------------------------------------
DECLARE @Epulet TABLE (KorzetId bigint PRIMARY KEY, Epulet nvarchar(50), Elotag nvarchar(5), Darab int, EmeletKezd int);
INSERT @Epulet VALUES
 (1, N'A épület', N'A', 10, 1), (2, N'B épület', N'B', 8, 1), (3, N'I épület', N'I', 6, 0),
 (4, N'G épület', N'G', 6, 1), (5, N'Könyvtár', N'K', 4, 0), (6, N'Központi épület', N'KP', 4, 1);

CREATE TABLE #H (Id bigint PRIMARY KEY, KorzetId bigint, Idx int, Epulet nvarchar(50), Emelet nvarchar(20),
                 Szobaszam nvarchar(20), Megnevezes nvarchar(200));
INSERT #H
SELECT ROW_NUMBER() OVER (ORDER BY e.KorzetId, n.n), e.KorzetId, n.n, e.Epulet,
       CAST(e.EmeletKezd + (n.n - 1) / 5 AS nvarchar(20)),
       e.Elotag + N'-' + CAST(e.EmeletKezd + (n.n - 1) / 5 AS nvarchar(5)) + RIGHT(N'0' + CAST((n.n - 1) % 5 + 1 AS nvarchar(2)), 2),
       CASE e.KorzetId
            WHEN 3 THEN N'Labor'
            WHEN 5 THEN CASE WHEN n.n % 2 = 0 THEN N'Raktár' ELSE N'Olvasóterem' END
            WHEN 6 THEN N'Iroda'
            ELSE CASE WHEN n.n % 3 = 0 THEN N'Iroda' ELSE N'Tanterem' END
       END
FROM @Epulet e JOIN #N n ON n.n <= e.Darab;

SET IDENTITY_INSERT dbo.Helyiseg ON;
INSERT dbo.Helyiseg (Id, Epulet, Emelet, Szobaszam, Megnevezes, Vonalkod, Aktiv)
SELECT Id, Epulet, Emelet, Szobaszam, Megnevezes, N'H-' + Szobaszam, 1 FROM #H;
SET IDENTITY_INSERT dbo.Helyiseg OFF;

--------------------------------------------------------------------------------
-- 4. Felelős személyek (kitalált nevek)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.FelelosSzemely ON;
INSERT dbo.FelelosSzemely (Id, Nev, Azonosito, Email, SzervezetiEgyseg, FelhasznaloId, Aktiv) VALUES
 (1,  N'Dr. Kovács Anna',   N'D0001', N'kovacs.anna@demo.local',   N'Mérnöki Kar',           5,    1),
 (2,  N'Nagy Péter',        N'D0002', N'nagy.peter@demo.local',    N'Mérnöki Kar',           NULL, 1),
 (3,  N'Szabó Eszter',      N'D0003', N'szabo.eszter@demo.local',  N'Mérnöki Kar',           NULL, 1),
 (4,  N'Tóth Gábor',        N'D0004', N'toth.gabor@demo.local',    N'Mérnöki Kar',           NULL, 1),
 (5,  N'Varga László',      N'D0005', N'varga.laszlo@demo.local',  N'Mérnöki Kar',           NULL, 1),
 (6,  N'Molnár Judit',      N'D0006', N'molnar.judit@demo.local',  N'Gazdaságtudományi Kar', NULL, 1),
 (7,  N'Farkas Tamás',      N'D0007', N'farkas.tamas@demo.local',  N'Gazdaságtudományi Kar', NULL, 1),
 (8,  N'Balogh Réka',       N'D0008', N'balogh.reka@demo.local',   N'Gazdaságtudományi Kar', NULL, 1),
 (9,  N'Papp Zoltán',       N'D0009', N'papp.zoltan@demo.local',   N'Egyetemi Könyvtár',     NULL, 1),
 (10, N'Takács Ildikó',     N'D0010', N'takacs.ildiko@demo.local', N'Egyetemi Könyvtár',     NULL, 1),
 (11, N'Juhász Ádám',       N'D0011', N'juhasz.adam@demo.local',   N'Gazdasági Igazgatóság', NULL, 1),
 (12, N'Lakatos Nóra',      N'D0012', N'lakatos.nora@demo.local',  N'Gazdasági Igazgatóság', NULL, 1),
 (13, N'Mészáros Béla',     N'D0013', N'meszaros.bela@demo.local', N'Informatikai Igazgatóság', NULL, 1),
 (14, N'Simon Krisztina',   N'D0014', N'simon.krisztina@demo.local', N'Mérnöki Kar',         NULL, 0);
SET IDENTITY_INSERT dbo.FelelosSzemely OFF;

--------------------------------------------------------------------------------
-- 5. Eszközök (240 db, 24 minta × 10), 24-es blokkonként azonos körzetben
--------------------------------------------------------------------------------
DECLARE @Minta TABLE (T int PRIMARY KEY, Megnevezes nvarchar(200), TipusId bigint, Elvart int, Ertek decimal(18,2), GyartasiSzam bit);
INSERT @Minta VALUES
 (1,  N'Asztali számítógép Dell OptiPlex 7010',   9,  1, 289000, 1),
 (2,  N'Monitor 24" Dell P2422H',                 11, 1,  68000, 1),
 (3,  N'Laptop Lenovo ThinkPad T14',              10, 1, 412000, 1),
 (4,  N'Irodai forgószék',                        6,  1,  45000, 0),
 (5,  N'Tantermi székkészlet',                    6, 30, 390000, 0),
 (6,  N'Oktatói asztal',                          7,  1,  72000, 0),
 (7,  N'Tantermi asztalkészlet',                  7, 15, 540000, 0),
 (8,  N'Iratszekrény, kétajtós',                  8,  1,  58000, 0),
 (9,  N'Lézernyomtató HP LaserJet Pro',           12, 1,  96000, 1),
 (10, N'Hálózati switch, 24 portos',              13, 1, 145000, 1),
 (11, N'Digitális oszcilloszkóp Rigol DS1054Z',   15, 1, 185000, 1),
 (12, N'Digitális multiméter Fluke 117',          16, 1,  92000, 1),
 (13, N'Multiméter-készlet (hallgatói labor)',    16, 10, 180000, 0),
 (14, N'Labortápegység 0–30 V',                   17, 1,  64000, 1),
 (15, N'Projektor Epson EB-W51',                  18, 1, 198000, 1),
 (16, N'Interaktív tábla',                        19, 1, 890000, 1),
 (17, N'Könyvespolc',                             8,  1,  36000, 0),
 (18, N'Tárgyalóasztal',                          7,  1, 124000, 0),
 (19, N'Monitor 27" LG 27UL500',                  11, 1,  95000, 1),
 (20, N'Asztali számítógép HP ProDesk 400',       9,  1, 265000, 1),
 (21, N'Tárgyalószék-készlet',                    6,  6, 132000, 0),
 (22, N'Wifi hozzáférési pont',                   13, 1,  58000, 1),
 (23, N'USB-C dokkolóállomás',                    14, 1,  42000, 1),
 (24, N'Vetítővászon, falra szerelhető',          20, 1,  48000, 0);

-- Állapotok: 5 = selejtezett, 6 = elveszett, 2 = javítás alatt, 7 = ellopott, 1 = aktív
CREATE TABLE #E (Id bigint PRIMARY KEY, T int, KorzetId bigint, Beszerzes date, Letrehozva datetime2(3), AllapotId bigint, Elvart int);
INSERT #E (Id, T, KorzetId, Beszerzes, AllapotId, Elvart)
SELECT n.n, (n.n - 1) % 24 + 1, ((n.n - 1) / 24) % 6 + 1,
       CASE WHEN n.n > 228 THEN DATEADD(day, n.n - 228, CAST('2026-02-02' AS date))
            ELSE DATEADD(day, -((n.n * 53) % 2100), CAST('2025-09-01' AS date)) END,
       CASE WHEN n.n % 37 = 0 THEN 5 WHEN n.n % 53 = 0 THEN 6 WHEN n.n % 41 = 0 THEN 2 WHEN n.n = 199 THEN 7 ELSE 1 END,
       m.Elvart
FROM #N n JOIN @Minta m ON m.T = (n.n - 1) % 24 + 1
WHERE n.n <= 240;

-- A kezdeti import 2024. január 15-én történt; a később beszerzett eszközöket kézzel vették fel.
UPDATE #E SET Letrehozva = DATEADD(hour, 9, CAST(CASE WHEN Beszerzes < '2024-01-15' THEN CAST('2024-01-15' AS date) ELSE Beszerzes END AS datetime2(3)));

SET IDENTITY_INSERT dbo.Eszkoz ON;
INSERT dbo.Eszkoz (Id, Megnevezes, EszkozTipusId, LeltarkorzetId, ElvartMennyiseg, MennyisegiEgyseg, Ertek,
                   BeszerzesDatuma, EszkozAllapotId, Megjegyzes, Letrehozva, LetrehoztaId)
SELECT e.Id, m.Megnevezes, m.TipusId, e.KorzetId, e.Elvart, N'db', m.Ertek + ((e.Id * 37) % 11) * 1000,
       e.Beszerzes, e.AllapotId,
       CASE WHEN e.Elvart > 1 THEN N'Készlet: ' + CAST(e.Elvart AS nvarchar(5)) + N' db egy leltári tételen.' END,
       e.Letrehozva, 2
FROM #E e JOIN @Minta m ON m.T = e.T;
SET IDENTITY_INSERT dbo.Eszkoz OFF;

--------------------------------------------------------------------------------
-- 6. Eszközkódok: minden eszköznek több, típusos kódja lehet (D-012)
--    1 SAP szám · 2 Leltári szám 1 · 3 Leltári szám 2 (vezető nullákkal) · 4 Gyártási szám · 5 Belső vonalkód
--------------------------------------------------------------------------------
INSERT dbo.EszkozKod (EszkozId, KodTipusId, Ertek, Aktiv)
SELECT Id, 1, N'40' + RIGHT(N'000000' + CAST(Id AS nvarchar(6)), 6), 1 FROM #E
UNION ALL
SELECT Id, 2, N'L-' + CAST(YEAR(Beszerzes) AS nvarchar(4)) + N'-' + RIGHT(N'0000' + CAST(Id AS nvarchar(4)), 4), 1 FROM #E WHERE Id % 5 <> 0
UNION ALL
SELECT Id, 3, RIGHT(N'000000' + CAST(Id * 7 + 100 AS nvarchar(6)), 6), 1 FROM #E WHERE Id % 4 = 0
UNION ALL
SELECT e.Id, 4, N'SN' + RIGHT(N'00000000' + CAST((e.Id * 7919) % 100000000 AS nvarchar(8)), 8), 1
FROM #E e JOIN @Minta m ON m.T = e.T WHERE m.GyartasiSzam = 1
UNION ALL
SELECT Id, 5, N'BV' + RIGHT(N'00000' + CAST(Id AS nvarchar(5)), 5), 1 FROM #E WHERE Id % 5 = 0
UNION ALL
-- lecserélt, már nem érvényes régi címkék (Aktiv = 0)
SELECT Id, 2, N'L-REGI-' + CAST(Id AS nvarchar(5)), 0 FROM #E WHERE Id % 25 = 0;

--------------------------------------------------------------------------------
-- 7. Kiegészítők: önálló eszközként vagy leírt tartozékként (F-19)
--------------------------------------------------------------------------------
INSERT dbo.Kiegeszito (FoEszkozId, KiegeszitoEszkozId, Leiras, Mennyiseg, ErvenyesTol)
SELECT Id, Id + 1,  NULL,                     1, Letrehozva FROM #E WHERE T = 1           -- számítógép + monitor
UNION ALL
SELECT Id, NULL,    N'Billentyűzet és egér',  1, Letrehozva FROM #E WHERE T IN (1, 20)
UNION ALL
SELECT Id, Id + 20, NULL,                     1, Letrehozva FROM #E WHERE T = 3           -- laptop + dokkoló
UNION ALL
SELECT Id, NULL,    N'Hálózati töltő',        1, Letrehozva FROM #E WHERE T = 3
UNION ALL
SELECT Id, NULL,    N'Mérőzsinór-készlet',    2, Letrehozva FROM #E WHERE T = 12;

--------------------------------------------------------------------------------
-- 8. Elhelyezés (helytörténet): az eszközök kb. 5/6-ának ismert a helye,
--    minden 15. eszköz a 2025. évi leltárkor más helyiségben került elő (áthelyezés)
--------------------------------------------------------------------------------
CREATE TABLE #Hely (EszkozId bigint PRIMARY KEY, HelyisegId bigint, RegiHelyisegId bigint NULL);
INSERT #Hely
SELECT e.Id,
       (SELECT h.Id FROM #H h WHERE h.KorzetId = e.KorzetId AND h.Idx = (e.Id * 11) % ep.Darab + 1),
       CASE WHEN e.Id % 15 = 0 AND e.Id <= 228
            THEN (SELECT h.Id FROM #H h WHERE h.KorzetId = e.KorzetId AND h.Idx = (e.Id * 11 + 1) % ep.Darab + 1) END
FROM #E e JOIN @Epulet ep ON ep.KorzetId = e.KorzetId
WHERE e.Id % 6 <> 5 AND e.AllapotId NOT IN (5, 6, 7);

INSERT dbo.Elhelyezes (EszkozId, HelyisegId, ErvenyesTol, ErvenyesIg, Forras)
SELECT h.EszkozId, h.RegiHelyisegId, DATEADD(day, 1, e.Letrehozva), CAST('2025-11-04T10:00:00' AS datetime2(3)), 'KEZI'
FROM #Hely h JOIN #E e ON e.Id = h.EszkozId WHERE h.RegiHelyisegId IS NOT NULL
UNION ALL
SELECT h.EszkozId, h.HelyisegId,
       CASE WHEN h.RegiHelyisegId IS NOT NULL THEN CAST('2025-11-04T10:00:00' AS datetime2(3)) ELSE DATEADD(day, 1, e.Letrehozva) END,
       NULL,
       CASE WHEN h.RegiHelyisegId IS NOT NULL THEN 'LELTAR' ELSE 'KEZI' END
FROM #Hely h JOIN #E e ON e.Id = h.EszkozId;

--------------------------------------------------------------------------------
-- 9. Felelős-hozzárendelés: minden 20. régi eszköznél 2025. március 1-jén felelősváltás történt
--------------------------------------------------------------------------------
INSERT dbo.FelelosHozzarendeles (EszkozId, FelelosId, ErvenyesTol, ErvenyesIg)
SELECT Id, (Id * 5) % 13 + 1, DATEADD(day, 1, Letrehozva), NULL
FROM #E WHERE Id % 4 <> 3 AND AllapotId NOT IN (5, 6, 7) AND NOT (Id % 20 = 0 AND Letrehozva < '2025-02-01')
UNION ALL
SELECT Id, (Id * 3) % 13 + 1, DATEADD(day, 1, Letrehozva), CAST('2025-03-01T08:00:00' AS datetime2(3))
FROM #E WHERE Id % 4 <> 3 AND AllapotId NOT IN (5, 6, 7) AND Id % 20 = 0 AND Letrehozva < '2025-02-01'
UNION ALL
SELECT Id, (Id * 5) % 13 + 1, CAST('2025-03-01T08:00:00' AS datetime2(3)), NULL
FROM #E WHERE Id % 4 <> 3 AND AllapotId NOT IN (5, 6, 7) AND Id % 20 = 0 AND Letrehozva < '2025-02-01';

--------------------------------------------------------------------------------
-- 10. Állapotváltozások (logikai törlés időbélyeggel és indoklással, F-26)
--------------------------------------------------------------------------------
INSERT dbo.AllapotValtozas (EszkozId, RegiAllapotId, UjAllapotId, Indoklas, Ugyiratszam, FelhasznaloId, Idopont)
SELECT Id, 1, 4, N'Elhasználódott, javítása gazdaságtalan; selejtezésre javasolva.', NULL, 2,
       DATEADD(minute, Id, CAST('2025-12-01T09:00:00' AS datetime2(3)))
FROM #E WHERE AllapotId = 5
UNION ALL
SELECT Id, 4, 5, N'A selejtezési bizottság döntése alapján selejtezve.', N'SEL-2025/' + CAST(Id AS nvarchar(5)), 2,
       DATEADD(minute, Id, CAST('2025-12-10T09:00:00' AS datetime2(3)))
FROM #E WHERE AllapotId = 5
UNION ALL
SELECT Id, 1, 6, N'A 2025. évi leltárban nem került elő, a keresés eredménytelen volt.', NULL, 2,
       DATEADD(minute, Id, CAST('2025-12-12T09:00:00' AS datetime2(3)))
FROM #E WHERE AllapotId = 6
UNION ALL
SELECT Id, 1, 2, N'Meghibásodás miatt szervizbe szállítva.', NULL, 2,
       DATEADD(minute, Id, CAST('2026-03-02T10:00:00' AS datetime2(3)))
FROM #E WHERE AllapotId = 2
UNION ALL
SELECT Id, 1, 7, N'Eltulajdonítás, rendőrségi feljelentés alapján.', N'RK-2025/0412', 2,
       CAST('2025-12-15T11:00:00' AS datetime2(3))
FROM #E WHERE AllapotId = 7;

--------------------------------------------------------------------------------
-- 11. Leltáridőszakok és elvárt állomány (pillanatkép, D-003)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.LeltarIdoszak ON;
INSERT dbo.LeltarIdoszak (Id, Megnevezes, Tipus, Kezdete, Vege, Statusz, Lezarva, LezartaId) VALUES
 (1, N'2025. évi leltár', 'EVES', '2025-11-03', '2025-11-28', 'LEZART', '2025-12-05T14:00:00', 2),
 (2, N'2026. évi leltár', 'EVES', '2026-09-21', NULL,         'NYITOTT', NULL,              NULL);
SET IDENTITY_INSERT dbo.LeltarIdoszak OFF;

INSERT dbo.ElvartTetel (LeltarIdoszakId, EszkozId, LeltarkorzetId, ElvartMennyiseg, FelvitelOka)
SELECT 1, Id, KorzetId, Elvart, 'PILLANATKEP' FROM #E WHERE Beszerzes <= '2025-11-03'
UNION ALL
SELECT 2, Id, KorzetId, Elvart, 'PILLANATKEP' FROM #E WHERE AllapotId NOT IN (5, 6, 7);

--------------------------------------------------------------------------------
-- 12. Leolvasások
--   2025 (lezárt): hiányzó, részben megtalált, többlet, más körzetből előkerült, ismételt,
--                  sztornózott és ismeretlen kódú beolvasások is vannak.
--   2026 (nyitott): a Mérnöki Kar A és B épülete nagyrészt kész, a többi körzet még nem kezdődött el.
--   Az azonosítók: 2025 alap = eszköz Id, ismételt = 1000+, többlet = 2000+, sztornó = 3000+,
--                  ismeretlen = 3901+, 2026 alap = 5000+, 2026 egyedi esetek = 6000+
--------------------------------------------------------------------------------
CREATE TABLE #L (Id bigint PRIMARY KEY, IdoszakId bigint, EszkozId bigint NULL, KodId bigint NULL, Kod nvarchar(64),
                 KorzetId bigint, HelyisegId bigint NULL, Mennyiseg int, Minosites varchar(20), Mod varchar(20),
                 FelhasznaloId bigint, Idopont datetime2(3), Sztornozva bit,
                 SztornoIndoklas nvarchar(500) NULL, SztornoIdopont datetime2(3) NULL, SztornoztaId bigint NULL);

-- 2025 – alap beolvasások (minden 10. eszköz és az elveszettek hiányoznak)
INSERT #L
SELECT e.Id, 1, e.Id, k.Id, k.Ertek,
       CASE WHEN e.Id % 17 = 0 THEN e.KorzetId % 6 + 1 ELSE e.KorzetId END,
       CASE WHEN e.Id % 17 = 0 THEN NULL ELSE h.HelyisegId END,
       CASE WHEN e.Elvart > 1 AND e.Id % 5 <> 0 THEN e.Elvart - e.Id % 3 ELSE e.Elvart END,
       CASE WHEN e.Id % 17 = 0 THEN 'MAS_KORZET' ELSE 'OK' END,
       CASE WHEN e.Id % 9 = 0 THEN 'KEZI' WHEN e.Id % 13 = 0 THEN 'BEILLESZTES' ELSE 'OLVASO' END,
       CASE WHEN (CASE WHEN e.Id % 17 = 0 THEN e.KorzetId % 6 + 1 ELSE e.KorzetId END) <= 3 THEN 3 ELSE 4 END,
       DATEADD(minute, e.Id * 2, DATEADD(day, e.KorzetId - 1, CAST('2025-11-03T08:00:00' AS datetime2(3)))),
       0, NULL, NULL, NULL
FROM #E e
JOIN dbo.ElvartTetel t ON t.LeltarIdoszakId = 1 AND t.EszkozId = e.Id
CROSS APPLY (SELECT TOP (1) ek.Id, ek.Ertek FROM dbo.EszkozKod ek
             WHERE ek.EszkozId = e.Id AND ek.Aktiv = 1
             ORDER BY CASE WHEN e.Id % 3 = 0 AND ek.KodTipusId = 2 THEN 0 ELSE ek.KodTipusId END) k
LEFT JOIN #Hely h ON h.EszkozId = e.Id
WHERE e.Id % 10 <> 3 AND e.Id % 53 <> 0;

-- 2025 – véletlen ismételt beolvasás egyedi eszköznél (a darabszámba nem számít bele)
INSERT #L
SELECT 1000 + l.Id, 1, l.EszkozId, l.KodId, l.Kod, l.KorzetId, l.HelyisegId, 1, 'ISMETELT', l.Mod, l.FelhasznaloId,
       DATEADD(minute, 1, l.Idopont), 0, NULL, NULL, NULL
FROM #L l JOIN #E e ON e.Id = l.EszkozId
WHERE l.IdoszakId = 1 AND l.Id < 1000 AND e.Elvart = 1 AND e.Id % 29 = 0;

-- 2025 – többlet mennyiséges tételeknél (az elvártnál több darab került elő)
INSERT #L
SELECT 2000 + l.Id, 1, l.EszkozId, l.KodId, l.Kod, l.KorzetId, l.HelyisegId, 2, 'TOBBLET', l.Mod, l.FelhasznaloId,
       DATEADD(minute, 1, l.Idopont), 0, NULL, NULL, NULL
FROM #L l JOIN #E e ON e.Id = l.EszkozId
WHERE l.IdoszakId = 1 AND l.Id < 1000 AND e.Elvart > 1 AND e.Id % 5 = 0;

-- 2025 – hibás beolvasás, amelyet a leltározó indoklással sztornózott (UC-08)
INSERT #L
SELECT 3000 + l.Id, 1, l.EszkozId, l.KodId, l.Kod, l.KorzetId, l.HelyisegId, l.Mennyiseg, l.Minosites, l.Mod, l.FelhasznaloId,
       DATEADD(minute, 1, l.Idopont), 1, N'Véletlen dupla beolvasás.', DATEADD(minute, 3, l.Idopont), l.FelhasznaloId
FROM #L l
WHERE l.IdoszakId = 1 AND l.Id < 1000 AND l.Id % 31 = 0;

-- 2025 – ismeretlen kódok (nyilvántartáson kívüli vagy sérült címkék)
INSERT #L VALUES
 (3901, 1, NULL, NULL, N'X-0000123',     1, NULL, 1, 'ISMERETLEN_KOD', 'OLVASO', 3, '2025-11-03T10:15:00', 0, NULL, NULL, NULL),
 (3902, 1, NULL, NULL, N'40999001',      2, NULL, 1, 'ISMERETLEN_KOD', 'OLVASO', 3, '2025-11-04T09:40:00', 0, NULL, NULL, NULL),
 (3903, 1, NULL, NULL, N'L-2018-9999',   3, NULL, 1, 'ISMERETLEN_KOD', 'KEZI',   3, '2025-11-05T13:05:00', 0, NULL, NULL, NULL),
 (3904, 1, NULL, NULL, N'SN00000000',    4, NULL, 1, 'ISMERETLEN_KOD', 'OLVASO', 4, '2025-11-06T11:20:00', 0, NULL, NULL, NULL),
 (3905, 1, NULL, NULL, N'5998000123457', 5, NULL, 1, 'ISMERETLEN_KOD', 'OLVASO', 4, '2025-11-07T08:55:00', 0, NULL, NULL, NULL);

-- 2026 – folyamatban lévő leltár: MIK-A és MIK-B körzet, minden 7. eszköz még hiányzik
INSERT #L
SELECT 5000 + e.Id, 2, e.Id, k.Id, k.Ertek, e.KorzetId, h.HelyisegId, e.Elvart, 'OK', 'OLVASO', 3,
       DATEADD(minute, e.Id * 2, DATEADD(day, e.KorzetId - 1, CAST('2026-09-22T08:00:00' AS datetime2(3)))),
       0, NULL, NULL, NULL
FROM #E e
JOIN dbo.ElvartTetel t ON t.LeltarIdoszakId = 2 AND t.EszkozId = e.Id
CROSS APPLY (SELECT TOP (1) ek.Id, ek.Ertek FROM dbo.EszkozKod ek
             WHERE ek.EszkozId = e.Id AND ek.Aktiv = 1 ORDER BY ek.KodTipusId) k
LEFT JOIN #Hely h ON h.EszkozId = e.Id
WHERE e.KorzetId IN (1, 2) AND e.Id % 7 <> 0;

-- 2026 – más körzetből előkerült eszköz és egy selejtezett eszköz beolvasása
INSERT #L
SELECT 6000 + e.Id, 2, e.Id, k.Id, k.Ertek, v.Korzet, NULL, 1, v.Minosites, 'OLVASO', v.Felhasznalo, v.Idopont, 0, NULL, NULL, NULL
FROM (VALUES (50, 1, 'MAS_KORZET', 3, CAST('2026-09-22T11:30:00' AS datetime2(3))),
             (74, 4, 'NEM_AKTIV',  4, CAST('2026-09-23T09:10:00' AS datetime2(3)))) v(EszkozId, Korzet, Minosites, Felhasznalo, Idopont)
JOIN #E e ON e.Id = v.EszkozId
CROSS APPLY (SELECT TOP (1) ek.Id, ek.Ertek FROM dbo.EszkozKod ek WHERE ek.EszkozId = e.Id AND ek.Aktiv = 1 ORDER BY ek.KodTipusId) k;

SET IDENTITY_INSERT dbo.Leolvasas ON;
INSERT dbo.Leolvasas (Id, LeltarIdoszakId, BeolvasottKod, EszkozKodId, EszkozId, AktivKorzetId, HelyisegId, Mennyiseg,
                      Minosites, BeviteliMod, FelhasznaloId, Idopont, Sztornozva, SztornoIndoklas, SztornoIdopont, SztornoztaId)
SELECT Id, IdoszakId, Kod, KodId, EszkozId, KorzetId, HelyisegId, Mennyiseg,
       Minosites, Mod, FelhasznaloId, Idopont, Sztornozva, SztornoIndoklas, SztornoIdopont, SztornoztaId
FROM #L;
SET IDENTITY_INSERT dbo.Leolvasas OFF;

-- Kiegészítők ellenőrzése: közvetlen, ha a kiegészítő eszközt ugyanabban a leltárban maga is beolvasták (F-20)
INSERT dbo.KiegeszitoEllenorzes (LeolvasasId, KiegeszitoId, Mod)
SELECT l.Id, k.Id,
       CASE WHEN k.KiegeszitoEszkozId IS NOT NULL AND EXISTS (
                SELECT 1 FROM #L l2
                WHERE l2.IdoszakId = l.IdoszakId AND l2.EszkozId = k.KiegeszitoEszkozId
                  AND l2.Minosites IN ('OK', 'MAS_KORZET') AND l2.Sztornozva = 0)
            THEN 'KOZVETLEN' ELSE 'FELTETELEZETT' END
FROM #L l JOIN dbo.Kiegeszito k ON k.FoEszkozId = l.EszkozId
WHERE l.Id < 1000 OR (l.Id >= 5000 AND l.Id < 6000);

--------------------------------------------------------------------------------
-- 13. A kezdeti Excel-import naplója (részben sikeres, 4 hibás sorral)
--------------------------------------------------------------------------------
SET IDENTITY_INSERT dbo.ImportFutas ON;
INSERT dbo.ImportFutas (Id, Fajlnev, Idopont, FelhasznaloId, SorokSzama, Sikeres, Hibas, Statusz) VALUES
 (1, N'leltari_forrasadatok_2024.xlsx', '2024-01-15T09:00:00', 2, 170, 166, 4, 'RESZLEGES');
SET IDENTITY_INSERT dbo.ImportFutas OFF;

INSERT dbo.ImportHiba (ImportFutasId, Sor, Oszlop, Uzenet, Ertek) VALUES
 (1, 17,  N'Megnevezés',       N'Hiányzó kötelező mező: megnevezés.',                   NULL),
 (1, 58,  N'Leltári szám 1',   N'Ütköző kód: az érték már szerepel az 57. sorban.',     N'L-2021-0057'),
 (1, 120, N'Érték',            N'Nem értelmezhető szám.',                                N'12.500,-Ft'),
 (1, 141, N'Beszerzés dátuma', N'Érvénytelen dátum.',                                    N'2021.13.45');

COMMIT TRANSACTION;

--------------------------------------------------------------------------------
-- Összesítés
--------------------------------------------------------------------------------
SELECT N'Felhasználó' AS Tabla, COUNT(*) AS Sorok FROM dbo.Felhasznalo
UNION ALL SELECT N'Leltárkörzet', COUNT(*) FROM dbo.Leltarkorzet
UNION ALL SELECT N'Eszköztípus', COUNT(*) FROM dbo.EszkozTipus
UNION ALL SELECT N'Helyiség', COUNT(*) FROM dbo.Helyiseg
UNION ALL SELECT N'Felelős személy', COUNT(*) FROM dbo.FelelosSzemely
UNION ALL SELECT N'Eszköz', COUNT(*) FROM dbo.Eszkoz
UNION ALL SELECT N'Eszközkód', COUNT(*) FROM dbo.EszkozKod
UNION ALL SELECT N'Kiegészítő', COUNT(*) FROM dbo.Kiegeszito
UNION ALL SELECT N'Elhelyezés', COUNT(*) FROM dbo.Elhelyezes
UNION ALL SELECT N'Felelős-hozzárendelés', COUNT(*) FROM dbo.FelelosHozzarendeles
UNION ALL SELECT N'Állapotváltozás', COUNT(*) FROM dbo.AllapotValtozas
UNION ALL SELECT N'Elvárt tétel', COUNT(*) FROM dbo.ElvartTetel
UNION ALL SELECT N'Leolvasás', COUNT(*) FROM dbo.Leolvasas
UNION ALL SELECT N'Kiegészítő-ellenőrzés', COUNT(*) FROM dbo.KiegeszitoEllenorzes
UNION ALL SELECT N'Importhiba', COUNT(*) FROM dbo.ImportHiba;
