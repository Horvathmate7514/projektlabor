# Adatmodell – első elképzelés

Az adatmodell négy logikai területre bontható. A kiírás legfontosabb elvárása: **az elvárt állomány (forrásadatok)
és a leltározási eredmények elkülönítése**, valamint **a hely, a felelős és az állapot történetének megőrzése**.

| Terület | Entitások | Jelleg |
|---|---|---|
| **A – Törzs- és forrásadatok** | Eszkoz, EszkozKod, KodTipus, EszkozTipus, Leltarkorzet, Kiegeszito | Az elvárt állomány; ritkán változik |
| **B – Leltározás** | LeltarIdoszak, ElvartTetel, Leolvasas, KiegeszitoEllenorzes | Időszakonként elkülönülő eredmények |
| **C – Történeti adatok** | Helyiseg, Elhelyezes, FelelosSzemely, FelelosHozzarendeles, AllapotValtozas | Érvényességi időszakos / eseményalapú |
| **D – Rendszer** | Felhasznalo, Szerepkor, ImportFutas, ImportHiba, AuditNaplo | Hozzáférés, követhetőség |

## 1. ER-vázlat

```mermaid
erDiagram
    ESZKOZTIPUS ||--o{ ESZKOZTIPUS : "szülője"
    ESZKOZTIPUS ||--o{ ESZKOZ : "besorolja"
    LELTARKORZET ||--o{ ESZKOZ : "tartalmazza"
    ESZKOZ ||--|{ ESZKOZKOD : "azonosítja"
    KODTIPUS ||--o{ ESZKOZKOD : "típusa"
    ESZKOZ ||--o{ KIEGESZITO : "fő eszköz"
    ESZKOZ |o--o{ KIEGESZITO : "kiegészítő eszköz"
    ESZKOZ ||--o{ ALLAPOTVALTOZAS : "története"
    ESZKOZ ||--o{ ELHELYEZES : "helytörténete"
    HELYISEG ||--o{ ELHELYEZES : "hely"
    ESZKOZ ||--o{ FELELOS_HOZZARENDELES : "felelőse"
    FELELOS_SZEMELY ||--o{ FELELOS_HOZZARENDELES : "felel"

    LELTARIDOSZAK ||--|{ ELVART_TETEL : "pillanatkép"
    ESZKOZ ||--o{ ELVART_TETEL : "elvárt"
    LELTARIDOSZAK ||--o{ LEOLVASAS : "tartalmazza"
    ESZKOZ |o--o{ LEOLVASAS : "beolvasva"
    ESZKOZKOD |o--o{ LEOLVASAS : "ezzel a kóddal"
    HELYISEG |o--o{ LEOLVASAS : "itt"
    LELTARKORZET ||--o{ LEOLVASAS : "aktív körzet"
    FELHASZNALO ||--o{ LEOLVASAS : "rögzítette"
    LEOLVASAS ||--o{ KIEGESZITO_ELLENORZES : "kiegészítői"
    KIEGESZITO ||--o{ KIEGESZITO_ELLENORZES : "ellenőrzött"

    FELHASZNALO }o--o{ SZEREPKOR : "rendelkezik"
    FELHASZNALO ||--o{ IMPORT_FUTAS : "indította"
    IMPORT_FUTAS ||--o{ IMPORT_HIBA : "hibái"
    FELHASZNALO ||--o{ AUDIT_NAPLO : "végezte"

    ESZKOZ {
        bigint id PK
        string megnevezes
        bigint eszkoztipus_id FK
        bigint leltarkorzet_id FK
        int elvart_mennyiseg
        string mennyisegi_egyseg
        decimal ertek
        date beszerzes_datuma
        string aktualis_allapot
        bool megszunt
        string megjegyzes
        timestamp letrehozva
        timestamp modositva
    }
    ESZKOZKOD {
        bigint id PK
        bigint eszkoz_id FK
        bigint kodtipus_id FK
        string ertek
        bool aktiv
    }
    KODTIPUS {
        bigint id PK
        string nev
        bool vonalkodkent_hasznalhato
    }
    ESZKOZTIPUS {
        bigint id PK
        string nev
        bigint szulo_id FK
        bool aktiv
    }
    LELTARKORZET {
        bigint id PK
        string kod
        string nev
        string szervezeti_egyseg
        bool aktiv
    }
    KIEGESZITO {
        bigint id PK
        bigint fo_eszkoz_id FK
        bigint kiegeszito_eszkoz_id FK
        string leiras
        int mennyiseg
        timestamp ervenyes_tol
        timestamp ervenyes_ig
    }
    ALLAPOTVALTOZAS {
        bigint id PK
        bigint eszkoz_id FK
        string regi_allapot
        string uj_allapot
        string indoklas
        string ugyiratszam
        bigint felhasznalo_id FK
        timestamp idopont
    }
    HELYISEG {
        bigint id PK
        string epulet
        string emelet
        string szobaszam
        string megnevezes
        string vonalkod
        bool aktiv
    }
    ELHELYEZES {
        bigint id PK
        bigint eszkoz_id FK
        bigint helyiseg_id FK
        timestamp ervenyes_tol
        timestamp ervenyes_ig
        string forras
        bigint leolvasas_id FK
    }
    FELELOS_SZEMELY {
        bigint id PK
        string nev
        string azonosito
        string email
        string szervezeti_egyseg
        bigint felhasznalo_id FK
        bool aktiv
    }
    FELELOS_HOZZARENDELES {
        bigint id PK
        bigint eszkoz_id FK
        bigint felelos_id FK
        timestamp ervenyes_tol
        timestamp ervenyes_ig
    }
    LELTARIDOSZAK {
        bigint id PK
        string megnevezes
        string tipus
        date kezdete
        date vege
        string statusz
        timestamp lezarva
        bigint lezarta_id FK
    }
    ELVART_TETEL {
        bigint id PK
        bigint leltaridoszak_id FK
        bigint eszkoz_id FK
        bigint leltarkorzet_id FK
        int elvart_mennyiseg
    }
    LEOLVASAS {
        bigint id PK
        bigint leltaridoszak_id FK
        string beolvasott_kod
        bigint eszkozkod_id FK
        bigint eszkoz_id FK
        bigint aktiv_korzet_id FK
        bigint helyiseg_id FK
        int mennyiseg
        string minosites
        string beviteli_mod
        bigint felhasznalo_id FK
        timestamp idopont
        bool sztornozva
        string sztorno_indoklas
        timestamp sztorno_idopont
    }
    KIEGESZITO_ELLENORZES {
        bigint id PK
        bigint leolvasas_id FK
        bigint kiegeszito_id FK
        string mod
    }
    FELHASZNALO {
        bigint id PK
        string felhasznalonev
        string nev
        string email
        string jelszo_hash
        string szervezeti_egyseg
        bool aktiv
        timestamp utolso_belepes
    }
    SZEREPKOR {
        bigint id PK
        string nev
    }
    IMPORT_FUTAS {
        bigint id PK
        string fajlnev
        timestamp idopont
        bigint felhasznalo_id FK
        int sorok_szama
        int sikeres
        int hibas
    }
    IMPORT_HIBA {
        bigint id PK
        bigint import_futas_id FK
        int sor
        string oszlop
        string uzenet
    }
    AUDIT_NAPLO {
        bigint id PK
        string entitas
        bigint entitas_id
        string muvelet
        string regi_ertek_json
        string uj_ertek_json
        bigint felhasznalo_id FK
        timestamp idopont
    }
```

## 2. Kulcs tervezési szabályok

| # | Szabály | Kiírás pont |
|---|---|---|
| 1 | Az `ESZKOZ` nem tartalmaz helyet; a hely az `ELHELYEZES` táblában van érvényességi időszakkal. | 10 |
| 2 | Egy eszköznek bármennyi kódja lehet (`ESZKOZKOD`); az aktív kódok értéke egyedi (részleges egyedi index). | 5 |
| 3 | A leolvasás **esemény**; a beolvasott darabszám = a nem sztornózott `LEOLVASAS.mennyiseg` összege. | 6, 8 |
| 4 | A `LEOLVASAS` eltárolja a nyers beolvasott kódot is → ismeretlen kódnál az `eszkoz_id` üres. | 3 |
| 5 | `minosites`: `OK`, `MAS_KORZET`, `ISMETELT`, `TOBBLET`, `ISMERETLEN_KOD`, `NEM_AKTIV`. | 4, 6 |
| 6 | `beviteli_mod`: `OLVASO`, `BEILLESZTES`, `KEZI`, `DEMO` – a demó beolvasások kiszűrhetők. | 3 |
| 7 | Az `ELVART_TETEL` a leltáridőszak nyitásakor készül → a régi leltárak a forrásadatok későbbi változása után is reprodukálhatók. | 8 |
| 8 | `KIEGESZITO_ELLENORZES.mod`: `KOZVETLEN` vagy `FELTETELEZETT`. | 7 |
| 9 | Nincs fizikai törlés: `megszunt`/`aktiv` jelzők, `ALLAPOTVALTOZAS` időbélyeggel. | 9 |
| 10 | Az `ESZKOZTIPUS` és a `KODTIPUS` adatbázisban tárolt, bővíthető lista – nem beégetett felsorolás. | 12, 5 |
| 11 | Minden adatmódosítás az `AUDIT_NAPLO`-ba is bekerül (régi és új érték). | 9, 19 |

## 3. Nyitott adatmodell-kérdések (3. alkalomig)

- Az `aktualis_allapot` az `ESZKOZ` táblában tárolt (gyors lekérdezés) vagy mindig az `ALLAPOTVALTOZAS`-ból számolt? – *Javaslat: tárolt, tranzakcióban frissítve, a történet a külön táblában.*
- Kell-e a leltárkörzet-hozzárendelésnek is történet (eszköz átkerülhet másik körzetbe)?
- A forrás Excel többi oszlopa (amit most nem ismerünk) hogyan tárolható rugalmasan? – *Lehetőség: egyedi mezők táblája vagy JSON oszlop.*
- Kell-e külön `LeltarBizottsag` entitás (ki vett részt az adott leltárban)?
- Mennyiséges eszközöknél indokolt-e darabszintű sorozatszámkezelés?
