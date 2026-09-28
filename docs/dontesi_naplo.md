# Döntési és feltételezési napló

A kiírás előírja, hogy a dokumentációban megjelenjen: **milyen feltételezéseket tettünk, milyen alternatívák merültek
fel, milyen szempontok alapján döntöttünk, és milyen következményei vannak a döntéseknek.** Minden ilyen döntést itt,
a meghozatal pillanatában rögzítünk, hogy a dokumentáció írásakor ne kelljen utólag rekonstruálni.

Állapot: *javasolt* · *elfogadott* · *felülírva (→ D-XXX)*

---

## D-001 – Fizikai törlés helyett állapotalapú logikai törlés

- **Állapot:** elfogadott (a kiírás előírja) · **Terület:** adatmodell

**Kontextus.** Az eszközök megszűnhetnek (selejtezés, elvesztés, lopás), de a korábbi leltárak eredményének és az
eszköz történetének visszakereshetőnek kell maradnia.

**Alternatívák.** 1) Egyetlen állapotmező az eszköz táblában, felülírással. 2) Állapotmező + külön állapotváltozás-napló.
3) Teljes temporális tábla, minden mező verziózásával.

**Döntés.** **2.** – az aktuális állapot gyorsan lekérdezhető, a teljes történet mégis megmarad.

**Következmények.** Minden lekérdezésnél figyelni kell az állapot szerinti szűrésre (pl. a hiányzó eszközök listájában
ne szerepeljen a leltár előtt selejtezett tétel).

---

## D-002 – A leolvasás eseményként tárolódik

- **Állapot:** elfogadott · **Terület:** leltárlogika, adatmodell

**Kontextus.** Kezelni kell a mennyiségeket, a túlbeolvasást, a véletlen ismételt beolvasást és ezek javítását.

**Alternatívák.** 1) Eszközönként egy „megtalálva” jelző és egy darabszámláló. 2) Minden beolvasás külön esemény,
a darabszám ezekből számítva, javítás sztornóval. 3) Az 1. és 2. kombinációja gyorsítótárazott számlálóval.

**Döntés.** **2.**, szükség esetén később 3.-ra bővítve, ha a teljesítmény indokolja.

**Következmények.** Visszakereshető, ki mikor hol olvasta be az eszközt; a sztornó nem töröl. Az összesítő
lekérdezéseket indexekkel kell támogatni (lásd D-012).

---

## D-003 – Elvárt állomány pillanatképe a leltáridőszak nyitásakor

- **Állapot:** elfogadott · **Terület:** adatmodell, leltárlogika

**Kontextus.** A korábbi leltárak eredményét meg kell őrizni, de a forrásadatok időközben változnak. Egy több éve
lezárt leltár összevetése a *mai* forrásadatokkal hamis eredményt adna.

**Alternatívák.** 1) Mindig a mai forrásadatokkal hasonlítunk. 2) A forrásadatok történetéből rekonstruáljuk az akkori
állapotot. 3) Pillanatkép (`ElvartTetel`) a leltáridőszak megnyitásakor.

**Döntés.** **3.** – egyszerű, gyors, egyértelműen reprodukálható; a tárhelyigény elhanyagolható.

**Következmények.** A leltáridőszak alatt felvett új eszközöket külön kell kezelni: a pillanatkép kézzel bővíthető,
a bővítés naplózódik.

---

## D-004 – Kliens- és szervertechnológia: WPF + ASP.NET Core

- **Állapot:** elfogadott a csapatban – oktatói jóváhagyásra vár · **Terület:** architektúra

**Kontextus.** A kiírás asztali (nem webes) klienst és valódi kliens–szerver architektúrát ír elő.

**Alternatívák.** WPF + ASP.NET Core · Avalonia UI + ASP.NET Core · JavaFX + Spring Boot · Qt (C++) · PySide6 + FastAPI.

**Szempontok.** Csapat tapasztalata, GUI-képességek (adatrács, MVVM), kliens–szerver ökoszisztéma, Excel-kezelés,
bemutathatóság.

**Döntés.** **WPF + ASP.NET Core** – közös C#/.NET nyelv kliensen és szerveren, érett asztali keretrendszer és
szerveroldali ökoszisztéma.

**Következmények.** A kliens csak Windowson fut (F-32 feltételezés). Nem vállalunk többplatformos működést.

---

## D-005 – Adatbázis: Microsoft SQL Server

- **Állapot:** elfogadott · **Terület:** adatbázis

**Kontextus.** Relációs adatbázis kell, amely az elvárt állományt, a leltáreredményeket és a történeti adatokat is
tárolja, és jól illeszkedik a .NET-es szerverhez.

**Alternatívák.** MS SQL Server · PostgreSQL · MySQL/MariaDB · SQLite.

**Szempontok.** .NET- és EF Core-integráció, szűrt (feltételes) indexek támogatása, fejlesztői gépen való
telepíthetőség, a csapat korábbi tapasztalata.

**Döntés.** **MS SQL Server** (Developer vagy Express kiadás). Szűrt egyedi indexeket használunk az aktív kódokra és
a lezáratlan történeti sorokra, amit ez az adatbázis natívan támogat.

**Következmények.** A fejlesztéshez mindenkinek telepítenie kell egy SQL Server példányt (vagy konténerben futtatnia).
Az SQLite legfeljebb automatizált tesztekhez jöhet szóba. A bemutatóhoz elő kell készíteni egy feltöltött adatbázist.

---

## D-006 – Adatelérés: Entity Framework Core

- **Állapot:** elfogadott · **Terület:** szerver, adatelérés

**Alternatívák.** EF Core · Dapper · a kettő vegyesen.

**Szempontok.** Migrációk kezelése, fejlesztési sebesség, tanulási görbe, a bonyolult összesítő lekérdezések
teljesítménye.

**Döntés.** **EF Core**, kód alapú migrációkkal. Ha egy összesítő lekérdezés (például a leltár-összehasonlítás)
lassúnak bizonyul, azt egyedi SQL-lel vagy nézettel váltjuk ki – ezt a döntést akkor külön rögzítjük.

**Következmények.** Az adatbázis-séma a C# osztályokból és a Fluent API konfigurációból áll elő, a séma változásait
migrációk követik, így a repóból bárki fel tudja építeni ugyanazt az adatbázist.

---

## D-007 – Kommunikáció: REST (HTTP + JSON)

- **Állapot:** elfogadott · **Terület:** kommunikáció

**Alternatívák.** REST · REST + SignalR · gRPC.

**Szempontok.** Egyszerűség, tesztelhetőség (Swagger, Postman), a csapat tapasztalata, a valós idejű értesítés igénye.

**Döntés.** **REST**, HTTPS felett, JSON adatokkal. Valós idejű értesítést egyelőre nem valósítunk meg; a leltározás
előrehaladása kérésre frissül.

**Következmények.** Ha később mégis kell élő frissítés (például párhuzamosan dolgozó leltározóknál), az SignalR-rel
külön kiegészítésként építhető be, a REST-végpontok érintése nélkül.

---

## D-008 – Excel-kezelés: ClosedXML

- **Állapot:** elfogadott · **Terület:** import, export

**Alternatívák.** ClosedXML · EPPlus · NPOI.

**Szempontok.** Licenc, API egyszerűsége, formázott riportok készítése.

**Döntés.** **ClosedXML** (MIT licenc, korlátozás nélkül használható), a forrásadatok importálásához és a riportok
exportálásához egyaránt.

**Következmények.** Csak `.xlsx` formátumot kezelünk. Ha a forrásfájl régi `.xls`, azt előbb át kell alakítani –
ezt az importáláskor egyértelmű hibaüzenettel jelezzük.

---

## D-009 – Kiegészítő funkció és könyvtárai

- **Állapot:** javasolt – oktatói jóváhagyásra vár · **Terület:** kiegészítő funkció

**Döntés.** Webkamerás vonalkód-beolvasás (**OpenCvSharp4** + **ZXing.Net**), valamint címke- és
leltárjegyzőkönyv-generálás (**QuestPDF** + ZXing.Net rajzolás). Részletek:
[`03_alkalom/01_Kiegeszito_funkcio_javaslat.md`](../03_alkalom/01_Kiegeszito_funkcio_javaslat.md).

**Következmények.** A QuestPDF Community licencét a kódban be kell állítani, a feltételeit a bevezetés előtt
ellenőrizni kell.

---

## D-010 – Elsődleges kulcsok: `bigint` azonosító

- **Állapot:** elfogadott · **Terület:** adatmodell

**Alternatívák.** 1) Növekményes `bigint` azonosító. 2) `uniqueidentifier` (GUID). 3) Természetes kulcs (például SAP szám).

**Szempontok.** Index- és tárhelyigény, olvashatóság hibakereséskor, a természetes azonosítók változékonysága.

**Döntés.** **1.** – minden táblának saját, növekményes `bigint` azonosítója van. Az üzleti azonosítókra (SAP szám,
leltári szám) egyediségi megszorítás kerül, de nem ezek a kulcsok.

**Következmények.** Az SAP szám utólag javítható anélkül, hogy a kapcsolódó sorokat módosítani kellene.

---

## D-011 – Történetiség: saját történeti táblák

- **Állapot:** elfogadott · **Terület:** adatmodell

**Kontextus.** Az eszközök helyének, felelősének és állapotának változását követni kell.

**Alternatívák.** 1) Saját történeti táblák érvényességi időszakkal (`ErvenyesTol` / `ErvenyesIg`).
2) Az SQL Server rendszerverziózott (temporális) táblái. 3) Csak alkalmazásszintű naplózás.

**Szempontok.** Az adatbázis-független megoldás értéke, a lekérdezések egyszerűsége, az EF Core-támogatás,
az, hogy a hely és a felelős **üzleti** adat (nem csak technikai módosítási napló).

**Döntés.** **1.** – a hely, a felelős és az állapot változása önálló, üzleti jelentésű táblákban tárolódik.
Emellett minden adatmódosításról technikai `AuditNaplo` bejegyzés készül.

**Következmények.** Az „aktuális” sort szűrt egyedi index védi (`ErvenyesIg IS NULL`), így egy eszköznek egyszerre
csak egy érvényes helye és felelőse lehet.

---

## D-012 – Kódok normalizálása és egyedisége

- **Állapot:** elfogadott · **Terület:** adatmodell, leltárlogika

**Kontextus.** Ugyanaz a kód eltérő írásmóddal kerülhet be (szóközök, kis- és nagybetűk), a beolvasásnak mégis
biztosan találnia kell.

**Döntés.** Az `EszkozKod` táblában a nyers érték mellett tárolunk egy **normalizált** értéket (levágott szóközök,
nagybetűs alak), amelyet az adatbázis automatikusan számol. A keresés ezen történik, és erre kerül a **szűrt egyedi
index**: az aktív kódok normalizált értéke egyedi. A vezető nullákat jelentősnek tekintjük, tehát nem vágjuk le.

**Következmények.** Importáláskor az ütköző kódok hibaként jelennek meg az importnaplóban, és kézi rendezést igényelnek.

---

## D-013 – Megnevezések nyelve az adatbázisban és a kódban

- **Állapot:** javasolt · **Terület:** adatmodell, forráskód

**Kontextus.** A tervezési anyagok magyar fogalomneveket használnak (Eszköz, Leltárkörzet, Leolvasás).

**Alternatívák.** 1) Magyar, ékezet nélküli tábla- és mezőnevek. 2) Angol nevek. 3) Vegyes (angol kód, magyar adatbázis).

**Döntés.** **1.** – a tervezési dokumentumokkal és a feladatkiírás fogalmaival egyező, magyar, ékezet nélküli nevek.
A csoport a 3. alkalmon megerősíti; áttérés angolra csak most, a fejlesztés megkezdése előtt olcsó.

**Következmények.** A kód és a dokumentáció fogalmai egyeznek, ami a bemutatón és a védésen is előny.

---

## D-014 – MVVM a kliensen: CommunityToolkit.Mvvm

- **Állapot:** elfogadott · **Terület:** kliens

**Alternatívák.** CommunityToolkit.Mvvm · Prism · saját `ViewModelBase` és `RelayCommand`.

**Szempontok.** Ismétlődő kód mennyisége, tanulási görbe, függőség mérete, magyarázhatóság a bemutatón.

**Döntés.** **CommunityToolkit.Mvvm** – a Microsoft könnyű MVVM-csomagja; kódgenerálással (`[ObservableProperty]`,
`[RelayCommand]`) jelentősen kevesebb kézzel írt kód kell, és nem kényszerít ránk keretrendszert.

**Következmények.** A navigációt és a függőséginjektálást a kliensen magunknak kell megoldani (pl. a .NET beépített
DI-tárolójával). A generált kód működését mindenkinek értenie kell, hogy a bemutatón meg tudja magyarázni.

---

## D-015 – Hitelesítés: saját felhasználókezelés + JWT

- **Állapot:** elfogadott · **Terület:** szerver, biztonság

**Alternatívák.** Saját felhasználótábla + JWT · ASP.NET Core Identity + JWT · Windows-hitelesítés.

**Szempontok.** Illeszkedés az adatmodellhez (a `Felhasznalo` tábla több adatot tárol, mint név és jelszó),
bemutathatóság tartomány nélkül, átláthatóság a dokumentációban.

**Döntés.** **Saját felhasználókezelés.** A jelszó csak biztonságos, sózott hash-ként tárolódik (erre a .NET beépített
jelszó-hash eszközét használjuk, nem saját algoritmust). Belépéskor a szerver korlátozott érvényességű JWT tokent ad,
amely a felhasználó azonosítóját és szerepköreit tartalmazza; minden végpont ez alapján ellenőrzi a jogosultságot.

**Következmények.** A token aláíró kulcsa titok: nem kerülhet a repóba. A token lejárata és a kijelentkezés kezelése
a mi feladatunk. Ha később egyetemi címtárhoz kellene kapcsolódni, a hitelesítési réteg cserélhető.

---

## D-016 – WPF megjelenés és stíluskönyvtár

- **Állapot:** nyitott – döntés: Bárkányi Máté, a képernyőtervekkel együtt · **Terület:** kliens, GUI

**Alternatívák.** Saját stílusok · WPF-UI (Fluent) · MaterialDesignInXamlToolkit · MahApps.Metro.

**Szempontok.** Vizuális igényesség, egységesség, a leltározó képernyő visszajelzési színeinek kezelése, függőség mérete.
