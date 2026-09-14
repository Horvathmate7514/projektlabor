# Kliens–szerver architektúra – első elképzelés

## 1. Alapelvek

1. **Vékony kliens, okos szerver:** minden üzleti szabály (azonosítás, mennyiségkezelés, körzeteltérés, összehasonlítás,
   import-validáció, export) a szerveren fut. A kliens megjelenít, adatot gyűjt és a szerver szolgáltatásait hívja.
2. **Rétegzett szerver:** API ← alkalmazásszolgáltatások ← domain ← adatelérés. Egy új követelmény jellemzően egy új
   szolgáltatás + végpont, nem újratervezés.
3. **Szerződésalapú kommunikáció:** a kliens és a szerver közötti adatstruktúrák (DTO-k) dokumentáltak (OpenAPI).
4. **Semmi nem törlődik:** a módosítások eseményként/történetként tárolódnak.
5. **Bemutathatóság:** a szerver és az adatbázis egyszerűen, demóadatokkal indítható (a futtatási mód a 3. alkalomig dől el).

> **Választott technológia:** WPF kliens + ASP.NET Core szerver (oktatói jóváhagyásra vár).
> Az adatbázis és a kommunikációs protokoll még **nyitott részdöntés** (lásd `05_Technologiai_osszevetes.md`).

## 2. Komponensdiagram

```mermaid
flowchart LR
    subgraph Kliens[WPF kliens]
        direction TB
        V[Nézetek<br/>Leltározás, Eszközök, Összehasonlítás,<br/>Import, Riportok, Törzsadatok]
        VM[ViewModel réteg<br/>állapot, parancsok, validáció]
        SC[Beolvasás-kezelő<br/>billentyűzet-bemenet, demó mód]
        AC[API kliens<br/>HTTP + token kezelés]
        RT[Valós idejű kliens<br/>opcionális]
        LS[Helyi beállítások<br/>nézet, oszlopok]
        V --> VM
        SC --> VM
        VM --> AC
        VM --> RT
        VM --> LS
    end

    subgraph Szerver[ASP.NET Core szerver]
        direction TB
        API[API végpontok]
        HUB[Valós idejű értesítések<br/>opcionális]
        AUTH[Hitelesítés és jogosultság]
        subgraph Szolg[Alkalmazásszolgáltatások]
            S1[Eszközkezelés]
            S2[Import]
            S3[Leltározás / leolvasás]
            S4[Összehasonlítás]
            S5[Riport és export]
            S6[Törzsadatok]
            S7[Audit napló]
        end
        DOM[Domain modell és üzleti szabályok]
        DAL[Adatelérési réteg / ORM]
        API --> AUTH
        API --> Szolg
        HUB --> S3
        Szolg --> DOM
        Szolg --> DAL
    end

    DB[(Relációs adatbázis)]

    AC -- "HTTPS – protokoll: 3. alkalomig" --> API
    RT -- "valós idejű csatorna" --> HUB
    DAL --> DB
```

## 3. Felelősségek szétválasztása

| Feladat | Kliens | Szerver |
|---|---|---|
| Vonalkód-bemenet fogadása, demó mód | ✅ | |
| Beolvasott kód azonosítása, leolvasás minősítése | | ✅ |
| Visszajelzés megjelenítése (szín, hang) | ✅ | |
| Excel fájl kiválasztása és feltöltése | ✅ | |
| Excel feldolgozása, validálása, importálása | | ✅ |
| Riport paraméterek összeállítása | ✅ | |
| Riport előállítása (xlsx) | | ✅ |
| Riport mentése a felhasználó gépére | ✅ | |
| Szűrés, rendezés, lapozás nagy adatmennyiségen | kérés összeállítása | végrehajtás (adatbázisban) |
| Jogosultság ellenőrzése | csak a menük elrejtése | ✅ kötelező ellenőrzés |
| Audit napló | | ✅ |

## 4. Egy beolvasás útja

*REST-stílusú hívással szemléltetve – a végleges protokoll a 3. alkalomig dől el.*

```mermaid
sequenceDiagram
    actor L as Leltározó
    participant K as Kliens
    participant S as Szerver
    participant D as Adatbázis

    L->>K: Vonalkód beolvasása (billentyűzet + Enter)
    K->>S: POST /api/leltaridoszakok/{id}/leolvasasok<br/>{kod, korzetId, helyisegId, mennyiseg}
    S->>S: Token és jogosultság ellenőrzése
    S->>D: Kód keresése, eszköz, elvárt állomány, eddigi leolvasások
    D-->>S: Találat
    S->>S: Minősítés (OK / MAS_KORZET / ISMETELT / TOBBLET / ISMERETLEN / NEM_AKTIV)
    S->>D: Leolvasás + helytörténet + kiegészítők mentése (egy tranzakcióban)
    S-->>K: 201 Created {eredmeny, eszkoz, elvart, beolvasott, uzenet}
    S--)K: Értesítés: előrehaladás frissült (ha lesz valós idejű csatorna)
    K->>L: Színes visszajelzés, hang, lista frissítése
```

## 5. Első API-vázlat (erőforrások)

*REST-stílusban leírva; gRPC választása esetén ugyanezek a műveletek szolgáltatásmetódusokként jelennek meg.*

| Módszer | Végpont | Leírás |
|---|---|---|
| POST | `/api/auth/login` | Bejelentkezés, token |
| GET | `/api/eszkozok?szuro…&oldal…` | Eszközlista szűréssel, lapozással |
| GET | `/api/eszkozok/{id}` | Eszköz részletei (kódok, kiegészítők, aktuális hely, felelős) |
| POST / PUT | `/api/eszkozok`, `/api/eszkozok/{id}` | Felvitel, módosítás |
| POST | `/api/eszkozok/{id}/allapotvaltozasok` | Állapotváltozás (logikai törlés is) |
| GET | `/api/eszkozok/{id}/tortenet` | Állapot-, hely-, felelős- és adattörténet |
| POST | `/api/eszkozok/{id}/athelyezesek` | Kézi áthelyezés |
| POST | `/api/importok` | Excel feltöltés és import indítása |
| GET | `/api/importok/{id}` | Import eredménye, hibák |
| GET / POST | `/api/leltarkorzetek`, `/api/helyisegek`, `/api/eszkoztipusok`, `/api/felelosok`, `/api/kodtipusok` | Törzsadatok |
| POST | `/api/leltaridoszakok` | Új leltáridőszak (elvárt állomány pillanatképpel) |
| POST | `/api/leltaridoszakok/{id}/lezaras` | Lezárás |
| POST | `/api/leltaridoszakok/{id}/leolvasasok` | Beolvasás |
| POST | `/api/leolvasasok/{id}/sztorno` | Sztornó indoklással |
| GET | `/api/leltaridoszakok/{id}/osszehasonlitas?kategoria…` | Összehasonlítás |
| GET | `/api/riportok/{tipus}.xlsx?…` | Excel riport |

## 6. Nyitott architekturális kérdések (3. alkalomig)

- Kommunikációs protokoll (REST / gRPC) és kell-e valós idejű csatorna (SignalR)?
- Adatbázis-kezelő kiválasztása.
- Kell-e kliensoldali várakozási sor gyenge hálózat esetére?
- Egy vagy több kliensalkalmazás (pl. külön egyszerűsített „leltározó” és „admin” felület)? – *Előzetesen: egy alkalmazás, szerepkör szerint eltérő menüvel.*
- Hitelesítés: saját felhasználókezelés vagy egyetemi címtár szimulálása?
- Hol készüljön a riport – a szerveren (javasolt) vagy a kliensen a letöltött adatokból?
- Közös C# DTO-könyvtár a kliens és a szerver között, vagy OpenAPI-ból generált kliens?
