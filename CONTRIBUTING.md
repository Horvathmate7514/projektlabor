# Közreműködési szabályok

A cél, hogy a projekt fejlődése és **minden tag egyéni hozzájárulása** a Git-előzményekből visszakövethető legyen –
a kiírás ezt külön értékeli.

## Ágak

| Ág | Szerepe |
|---|---|
| `main` | Mindig forduló, a tesztekkel együtt működő állapot. Közvetlenül nem commitolunk rá. |
| `200` | A jelenlegi közös fejlesztési ág, innen megy pull request a `main`-re. |
| `feature/<issue>-rovid-leiras` | Javasolt: egy feladat = egy ág (pl. `feature/12-excel-import`), a `200`-ból vagy a `main`-ből. |
| `fix/…`, `docs/…` | Hibajavítás, illetve csak dokumentáció. |

A saját ágakkal jobban látszik, ki mit csinált, és kevesebb az ütközés. A GitHubon érdemes bekapcsolni a `main`
védelmét: pull request kötelező, legalább egy jóváhagyással, és a CI-nak zöldnek kell lennie.

## Munkafolyamat

1. Minden feladathoz tartozik egy **GitHub Issue** felelőssel és mérföldkővel (`03_alkalom/05_Fejlesztesi_utemterv.md`).
2. Kicsi, gyakori, értelmes commitok.
3. **Pull request**, `Closes #<issue>` hivatkozással.
4. A CI (`.github/workflows/ci.yml`) lefordítja a megoldást és lefuttatja a teszteket – piros CI-val nem vonunk össze.
5. Legalább **egy másik csapattag** átnézi és jóváhagyja. Így mindenki ismeri a többiek kódját is, ami a bemutatón
   elvárás.

## Commitüzenetek

Formátum: `<típus>: <rövid leírás>` – magyarul, felszólító vagy leíró módban, egységesen.

| Típus | Mikor |
|---|---|
| `feat` | új funkció |
| `fix` | hibajavítás |
| `db` | adatmodell, migráció, adatbázis-szkript |
| `docs` | dokumentáció |
| `test` | tesztek |
| `refactor` | átszervezés működésváltozás nélkül |
| `chore` | build, konfiguráció, függőségek, CI |

Példák:
```
feat: eszköz azonosítása bármely hozzárendelt kód alapján
db: leolvasás és leltáridőszak táblák, első migráció
fix: az ismételt beolvasás ne növelje a darabszámot
```

## Adatmodell-változtatás

1. Módosítsd az entitást és a konfigurációs osztályát (`src/Szerver/Adat/`).
2. Készíts migrációt: `dotnet ef migrations add <ErtelmesNev> --project src/Szerver --output-dir Adat/Migraciok`.
3. Futtasd a teszteket – az `A_migraciok_naprakeszek` teszt elbukik, ha a migráció kimaradt.
4. Ha a változás érinti a demóadatokat, igazítsd a `db/demoadatok.sql`-t, és futtasd le a `db/ellenorzes.sql`-t.
5. Rögzítsd a döntést a `docs/dontesi_naplo.md`-ben, ha tervezési kérdés volt.

## Pull request ellenőrzőlista

- [ ] A megoldás lefordul, a tesztek sikeresek (a CI zöld)
- [ ] Új logikához új teszt tartozik
- [ ] Nincs benne titkos adat (jelszó, kulcs, kapcsolati adat) – ezek az `appsettings.Local.json`-ba valók
- [ ] Ha AI-t használtam, rögzítettem a `docs/AI_hasznalati_naplo.md`-ben
- [ ] Ha tervezési döntést hoztam, rögzítettem a `docs/dontesi_naplo.md`-ben
- [ ] A változtatás minden sorát el tudom magyarázni
