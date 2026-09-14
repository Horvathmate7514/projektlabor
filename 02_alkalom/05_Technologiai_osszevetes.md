# Technológiaválasztás

**Fontos:** a választott technológiát a **3. alkalomig az oktatóval egyeztetni és jóváhagyatni** kell.
A dokumentációban a döntést **indokolni** kell, és be kell mutatni a **megvizsgált alternatívákat** is.

## 1. A csapat döntése

| Réteg | Választás | Állapot |
|---|---|---|
| **Kliens** | **WPF** (.NET, C#) | Csapatdöntés – oktatói jóváhagyásra vár |
| **Szerver** | **ASP.NET Core** (.NET, C#) | Csapatdöntés – oktatói jóváhagyásra vár |

### Lehetséges indokok (a csapat pontosítsa, ezek kerülnek a dokumentációba)

- **Közös nyelv és platform:** kliensen és szerveren is C#/.NET → közös adatátviteli modellek (DTO-k) egy megosztott
  könyvtárban, egységes fejlesztőkörnyezet és hibakeresés.
- **WPF:** érett asztali keretrendszer; XAML alapú felületleírás, erős adatkötés, MVVM-minta támogatása,
  stílusok és sablonok az egységes megjelenéshez, nagy adatmennyiségnél is használható adatrács (virtualizációval).
- **ASP.NET Core:** beépített függőséginjektálás, hitelesítés és jogosultságkezelés, konfigurációkezelés,
  OpenAPI-támogatás; jól illeszkedik a .NET adatelérési könyvtáraihoz.
- **Célkörnyezet:** az egyetemi irodai munkaállomások jellemzően Windows rendszerűek (lásd F-32 feltételezés).
- **Csapat tapasztalata:** *kitöltendő – ki milyen .NET / WPF / ASP.NET tapasztalattal rendelkezik.*

### Következmények

- A kliens **csak Windows** rendszeren fut – a bemutatót Windows gépen kell tartani.
- A választás miatt valószínű, hogy más csoport is .NET-et választ → érdemes egyeztetni, és az eltérést
  a részdöntésekben (pl. adatbázis, kommunikáció, UI-könyvtár) megmutatni.

## 2. Megvizsgált alternatívák

A dokumentáció elvárja, hogy az alternatívák és a döntési szempontok is megjelenjenek.

| Alternatíva | Előnyök | Miért nem ezt választottuk? *(csapat pontosítsa)* |
|---|---|---|
| Avalonia UI + ASP.NET Core | Többplatformos, WPF-hez hasonló XAML | Kisebb közösség, kevesebb kész komponens; a többplatformosság nem követelmény |
| .NET MAUI + ASP.NET Core | Asztali és mobil célplatform egy kódbázisból | Asztali adatrács-/táblázatkezelés kevésbé érett |
| JavaFX + Spring Boot | Egységes Java stack, ipari szabvány szerver | Más nyelv és ökoszisztéma, telepítés körülményesebb |
| Qt (C++) | Natív teljesítmény, igényes GUI | Legnagyobb fejlesztési ráfordítás, szegényesebb szerveroldali ökoszisztéma |
| PySide6 + FastAPI | Gyors fejlesztés, egyszerű Excel-kezelés | Dinamikus típusosság nagyobb kódbázisnál, asztali csomagolás körülményes |

### Döntési szempontok

| Szempont | Magyarázat |
|---|---|
| Csapat tapasztalata | A félév rövid; ismeretlen technológia komoly kockázat |
| GUI-képességek | Adatrács, oszlopválasztás, szűrés, stílusozhatóság, MVVM |
| Kliens–szerver ökoszisztéma | Kommunikációs könyvtárak, adatelérés, hitelesítés |
| Excel-kezelés | Import és formázott export könyvtárai |
| Bemutathatóság | Egyszerű indítás a saját gépen |
| Dokumentáció, közösség | Hibák megoldhatósága |

## 3. Nyitott részdöntések – a 3. alkalomig eldöntendő

Ezekről a csapat közösen dönt; a döntéseket a `docs/dontesi_naplo.md` rögzíti.

| Terület | Lehetőségek | Mérlegelendő |
|---|---|---|
| **Adatbázis** | MS SQL Server · PostgreSQL · MySQL/MariaDB · SQLite | .NET-integráció, történetiség támogatása, telepítés a bemutatóhoz; az SQLite szerveres, többfelhasználós használatra gyenge |
| **Kommunikáció** | REST (HTTP + JSON) · gRPC · kiegészítésként valós idejű csatorna (SignalR) | Egyszerűség, tesztelhetőség, típusos szerződés, valós idejű értesítés igénye |
| **Adatelérés** | Entity Framework Core · Dapper · ADO.NET | Migrációk, lekérdezések átláthatósága, teljesítmény |
| **MVVM a kliensen** | CommunityToolkit.Mvvm · Prism · saját megvalósítás | Boilerplate mennyisége, navigáció, tanulási görbe |
| **WPF megjelenés** | Saját stílusok · MahApps.Metro · MaterialDesignInXamlToolkit · WPF-UI | Vizuális igényesség, egységesség, függőség mérete |
| **Excel** | ClosedXML · EPPlus (5-ös verziótól licencbeállítás kell, nem kereskedelmi célra ingyenes) · NPOI | Licenc, formázási lehetőségek, import és export támogatása |
| **Hitelesítés** | Saját felhasználókezelés + token · ASP.NET Core Identity · Windows/AD hitelesítés | Életszerűség, bemutathatóság, jogosultsági modell |
| **Futtatás a bemutatón** | Helyi telepítés · Docker (szerver + adatbázis) | Egy paranccsal indítható-e, mennyire függ a géptől |

## 4. Kiegészítő eszközök (stacktől függetlenül)

| Terület | Eszköz |
|---|---|
| Verziókezelés, feladatkövetés | Git, GitHub (Issues, Projects, Pull Requests) |
| CI | GitHub Actions (fordítás + tesztek minden PR-ra) |
| Fejlesztőkörnyezet | Visual Studio / JetBrains Rider |
| API tesztelés, dokumentálás | OpenAPI/Swagger, Postman vagy Bruno |
| Tervezés | draw.io / diagrams.net, Mermaid, Figma (képernyőtervek) |
| Dokumentáció | Kari Word vagy LaTeX sablon |
