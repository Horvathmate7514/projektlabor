# Kiegészítő funkció javaslat

**Csoport:** Bárkányi Máté, Horváth Máté, Kiss Barnabás
**Projekt:** Leltározó és leltárkezelő alkalmazás (WPF kliens + ASP.NET Core szerver)
**Állapot:** javaslat – oktatói jóváhagyásra vár (3. alkalom)

Két, egymást kiegészítő funkciót javaslunk, amelyek együtt a **teljes címkézési–beolvasási kört** lefedik:

> **A)** Webkamerás vonalkód-beolvasás · **B)** Címke- és leltárjegyzőkönyv-generálás
>
> A rendszer vonalkódcímkét nyomtat (B) → a címkét kamerával is be lehet olvasni (A) → a leltár végén hivatalos
> jegyzőkönyv készül (B).

---

## A) Webkamerás vonalkód-beolvasás

### Cél és probléma
A kiírás a vonalkódolvasót billentyűzetként működő eszközként kezeli. A gyakorlatban azonban:
- egy leltárban többen dolgoznak, de nem jut mindenkinek olvasó;
- sok egyszerű olvasó csak vonalkódot (1D) olvas, QR-kódot nem;
- a bemutatón és a fejlesztés közben is olvasó nélkül kell tudni dolgozni.

A funkció után **bármely kamerával rendelkező gép leltározóeszközzé válik**.

### Működés
1. A *Leltározás* képernyőn a felhasználó bekapcsolja a **Kamera** módot, és kiválasztja a kamerát.
2. A képernyő sarkában megjelenik az élőkép; a felismert kódot a rendszer keretezéssel jelzi.
3. A felismert kód **pontosan ugyanazon az úton** megy tovább, mint a billentyűzetes beolvasás: azonosítás,
   minősítés (megtalálva, más körzet, ismételt stb.) és a leolvasás rögzítése a szerveren.
4. Visszajelzés: hangjelzés és színes kiemelés, ugyanúgy, mint olvasóval.
5. **Ismétlésvédelem:** ugyanazt a kódot néhány másodpercen belül csak egyszer dolgozza fel, különben egy kamera
   elé tartott címke tucatnyi leolvasást okozna.
6. Ha a kép alapján nem sikerül a felismerés, a kézi bevitel továbbra is elérhető.

### Technikai tartalom
- **Kamerakép rögzítése WPF-ben:** **OpenCvSharp4** (a natív futtatókörnyezettel és a WPF-megjelenítést segítő kiegészítő csomaggal együtt).
- **Dekódolás: ZXing.Net.** Formátumok: Code 128, Code 39, EAN-13, QR, Data Matrix.
- **Képkockák feldolgozása háttérszálon**, ritkítással, hogy a felület ne lassuljon.
- **Megbízhatóság:** csak több egymást követő képkockán azonosan felismert kódot fogad el.
- **Adatvédelem:** a képfeldolgozás a kliensen történik, a szerverre **csak a kód szövege** kerül, kép nem.
- **Adatmodell:** a `Leolvasas.beviteli_mod` értékei közé bekerül a `KAMERA`, így a riportokban a kamerás
  beolvasások elkülöníthetők.

---

## B) Címke- és leltárjegyzőkönyv-generálás

### Cél és probléma
- Leltározáskor gyakori, hogy **egy eszközről hiányzik vagy olvashatatlan a címke**. Ilyenkor a helyszínen
  azonnal pótolható kellene legyen.
- A leltár eredményét a gyakorlatban **aláírt jegyzőkönyvvel** zárják le, amit ma kézzel kell összeállítani.

### Működés – címkék
1. Címkét lehet nyomtatni kijelölt eszközökre, egy helyiség összes eszközére vagy a „hiányzó címke” jelöléssel
   ellátott eszközökre.
2. **Helyiségcímke** is készíthető: a helyiség vonalkódjának beolvasásával kiválasztható a helyiség a leltározás elején.
3. A címkeív PDF-ben készül, **beállítható elrendezéssel** (sorok, oszlopok, margók mm-ben), a szabványos öntapadós etikettívekhez igazítva.
4. A címke tartalma: vonalkód vagy QR-kód, megnevezés, leltári szám, helyiség.
5. Ha az eszköznek nincs olvasható kódja, a rendszer **új, egyedi belső kódot** generál („belső vonalkód” kódtípus), és hozzárendeli az eszközhöz.
6. A címkenyomtatás **naplózódik**: ki, mikor, melyik eszközre.

### Működés – leltárjegyzőkönyv
1. **Csak lezárt leltáridőszakról** készíthető, így a tartalma már nem változhat.
2. **Tartalom:**
   - fejléc: szervezeti egység, leltárkörzet, időszak, a leltárban résztvevők;
   - összesítő: elvárt, megtalált, hiányzó, többlet, más körzetből előkerült;
   - eltéréslisták indoklással;
   - aláírási helyek.
3. A dokumentumon egyedi azonosító és generálási időpont szerepel, a generálás naplózódik.

### Technikai tartalom
- **Generálás a szerveren** (üzleti logika és adatok ott vannak); a kliens letölti, menti vagy nyomtatja.
- **PDF-előállítás: QuestPDF.** Az elrendezés C# kódban, olvashatóan írható le, és milliméteres méretmegadást is támogat.
- **Vonalkód-rajzolás:** a felismeréshez is használt **ZXing.Net** rajzoló funkciójával; a kapott kép a PDF-be ágyazódik.
- **Pontos nyomdai elrendezés:** a méretek mm-ben megadva, próbanyomtatással ellenőrizve.

---

## Miért jelent érdemi többletet?

| Szempont | A) Kamerás beolvasás | B) Címke és jegyzőkönyv |
|---|---|---|
| Valós probléma | Kevés olvasó, QR-kódok, bemutatás olvasó nélkül | Hiányzó címkék, kézzel írt jegyzőkönyv |
| Szakmai tartalom | Képfeldolgozás, párhuzamos feldolgozás, megbízható felismerés | Dokumentumgenerálás, vonalkód-szabványok, nyomdai elrendezés |
| Kapcsolat a kötelező részekkel | A meglévő beolvasási folyamatba illeszkedik | A leltáridőszak lezárására és a kódkezelésre épül |
| Bemutathatóság | Élőben, egy kinyomtatott címkével | A B)-vel nyomtatott címke az A)-val beolvasható |

A két funkció **egymás tesztanyagát is adja**: a generált címkékkel mérhető a kamerás felismerés sikeraránya és sebessége.

## Becsült ráfordítás és ütemezés

| Rész | Becsült ráfordítás | Tervezett felelős |
|---|---|---|
| A) Kamerakép, felismerés, beépítés a leltározó képernyőbe | ~2 hét | Bárkányi Máté (kliens) |
| B) Címkeív-generálás, belső kód | ~1,5 hét | Kiss Barnabás (export) |
| B) Leltárjegyzőkönyv | ~1 hét | Kiss Barnabás, Horváth Máté (szerver) |
| Tesztelés (fényviszonyok, távolság, sérült címke, nyomtatási pontosság) | ~0,5 hét | Mindenki |

A felelősök kijelölése a jelenlegi terv; a csoport fenntartja a lehetőséget, hogy a munkaterhelés alakulása szerint
módosítsa. Az esetleges változásokat a dokumentáció munkamegosztási fejezetében rögzítjük.

A kiírás szerint a kiegészítő funkcióknak a **12. alkalomra** kell elkészülniük. A kamerás beolvasás alapjai már
a 6. alkalom körül (vonalkódos leltározás) beépíthetők.

## Választott könyvtárak

| Feladat | Könyvtár | Licenc | Miért ezt választottuk | Elvetett alternatívák |
|---|---|---|---|---|
| Kamerakép a kliensen | **OpenCvSharp4** | Apache 2.0 | Kevés kóddal indítható kamera és képkocka-feldolgozás, WPF-megjelenítéshez kész kiegészítővel | Windows.Media.Capture (bonyolultabb, WPF-ben nincs kész előnézet); AForge.NET (elavult); Emgu CV (GPL) |
| Vonalkód felismerése és rajzolása | **ZXing.Net** | Apache 2.0 | Egyetlen könyvtár olvasásra és címkerajzolásra; 1D, QR és Data Matrix formátumokat is kezel | Csak QR-re szorítkozó megoldás (a meglévő vonalkódos címkéket nem olvasná) |
| PDF előállítása a szerveren | **QuestPDF** | Community licenc (a kódban beállítandó) | Táblázatos jegyzőkönyv és milliméterre pontos címkeív is jól leírható vele | PdfSharp/MigraDoc (hosszabb, alacsonyabb szintű kód); iText (AGPL) |

**Ellenőrizendő a bevezetés előtt:**
- a QuestPDF Community licenc feltételei (a licenctípust a program indulásakor be kell állítani);
- a ZXing.Net és az OpenCvSharp közötti képátadás módja: van hozzá kiegészítő csomag, enélkül néhány soros átalakítás szükséges.

## Kockázatok

| Kockázat | Kezelés |
|---|---|
| A laptopok beépített kamerája fix fókuszú, a kis 1D vonalkódokat nehezen olvassa | QR-kód a saját címkéken, nagyobb címkeméret, külső USB-kamera lehetősége |
| Rossz fényviszonyok | Tesztelés többféle megvilágításban, visszajelzés a felhasználónak („közelebb / több fény”) |
| A QuestPDF Community licenc feltételei megváltoznak vagy nem illenek a felhasználásunkhoz | A feltételek ellenőrzése a bevezetés előtt; szükség esetén váltás a PdfSharp/MigraDoc könyvtárra |
| Nyomtatási eltérések különböző nyomtatókon | Beállítható margók, próbanyomtatási oldal |

## Kérdések az oktatóhoz

1. Elfogadható-e a kiegészítő funkció két, egymásra épülő részből álló kombinációként?
2. Van-e az egyetemen előírt leltárjegyzőkönyv-minta vagy kötelező tartalom, amelyhez igazodnunk kellene?
