# Leltár-összehasonlítás – szerveroldal

**Felelős:** Horváth Máté · **Állapot:** a 9. alkalomra elkészült, tesztelt változat

A 9. alkalom elvárása: a leltáridőszakok elkülönítése, a megtalált, hiányzó, többlet, más körzetben talált és eltérő
mennyiségű tételek nézetei, a korábbi időszakok megőrzése. Az időszakon belüli nézetek a 7. alkalom tétellistájára
épülnek (állapotszűrő, D-037); most az **időszakok összevetése** és az **Excel-export** került mellé. A nézeteket a
kliensen Bárkányi Máté készíti.

## 1. Mi készült el?

| Elvárás (kiírás, 9. alkalom) | Megvalósítás |
|---|---|
| Forrásadatok és leolvasások elkülönítése | Változatlanul: elvárt tételek pillanatképe (D-030) és leolvasások időszakonként |
| Megtalált / hiányzó / többlet / más körzet / eltérő mennyiség | `GET .../tetelek?allapot=` – `Rendben`, `Hianyzik`, `Tobblet`, `MasKorzetben`, `Reszleges`, `NemElvart` |
| Korábbi időszakok megőrzése és összevetése | `GET /api/leltaridoszakok/{id}/osszevetes?korabbiIdoszakId=` – eszközönkénti változás (D-048) |
| Export | `GET /api/leltaridoszakok/{id}/tetelek.xlsx` – körzetösszesítő és szűrt tétellista egy munkafüzetben |

## 2. Időszakok összevetése (D-048)

Mindkét időszak tétellistája a 7. alkalom szabályaival számolódik, majd eszközönként párba állnak. A változás:

| Korábbi állapot | Aktuális állapot | Változás |
|---|---|---|
| – (nem szerepelt) | bármi | `UjTetel` |
| bármi | – (nem szerepel) | `KikerultTetel` |
| `Hianyzik` | `Hianyzik` | `TartosHiany` |
| `Hianyzik` | más | `Megkerult` |
| más | `Hianyzik` | `UjHiany` |
| azonos | azonos | `Valtozatlan` |
| eltérő | eltérő | `AllapotValtozott` |

A válasz a változásonkénti darabszámokat és a lapozott tétellistát adja; szűrhető `valtozas=` és
`csakValtozasok=true` szerint. A láthatóság ugyanaz, mint az összesítésnél (`KorzetJogosultsag`). Hibás esetek: önmagával
összevetés 400, nem létező időszak 404, ismeretlen változástípus 400.

## 3. Megszűnt eszköz a nyitott leltárban (D-049)

A 10. alkalom nyitott kérdésére (Kiss Barnabás): ha egy eszköz a leltár alatt megszűnt állapotba kerül, a
pillanatkép miatt továbbra is elvárt tétel marad, de a tétel `megszunt: true` jelzést kap, az Excelben külön oszlopban.
Így a kliens jelölheti, és nem tűnik el nyomtalanul a hiányzók közül.

## 4. Excel-export

A `tetelek.xlsx` ugyanazokat a szűrőket fogadja, mint a tétellista (lapozás nélkül). Két munkalap:
**Összesítés** (körzetenként, összesen sorral és készültség-százalékkal) és **Tételek** (magyar állapotnevekkel,
autoszűrővel, rögzített fejléccel). A formázás az importhibák exportját követi.

## 5. Tesztek

`tests/Szerver.Tesztek/OsszevetesTesztek.cs` – 12 új eset: a változás minősítése mind a hét esetre (egységteszt),
két időszak összevetése, szűrés változásra, 400/404, az Excel tartalmának ellenőrzése ClosedXML-lel és a megszűnt
eszköz jelölése. A teljes csomag: 263 teszt, SQL Server nélkül 254 sikeres és 9 kihagyott.

## 6. Ismert korlátok

| Téma | Állapot | Terv |
|---|---|---|
| Teljesítmény | Két időszak tétellistája a memóriában párosul | Ha lassú, lezáráskor eltárolt eredmény |
| Riportok | Csak a tétellista exportja | 11. alkalom: típus, helyiség, felelős szerinti riportok (C, A) |
