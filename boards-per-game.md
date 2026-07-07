# Boardy per gra — zmiany na frontendzie i wymagane prace na backendzie

Dotychczas graf postaci był jeden, współdzielony między wszystkimi grami RPG.
Ta zmiana wprowadza **boardy**: jeden board = jeden graf jednej gry, z własną
nazwą i własną konfiguracją wizualną ustawianą przez MG. Kontrakt API
(`LoreWeave/Utilities/Contract.yaml`) został podbity do **2.0.0** (zmiana
łamiąca — wszystkie ścieżki danych przeszły pod board).

---

## 1. Zmiany w kontrakcie API (v2.0.0)

### Nowy zasób `Boards`

| Endpoint | Operacja | Opis |
| --- | --- | --- |
| `GET /v1/boards` | `getBoards` | Lista boardów (max 100). |
| `POST /v1/boards` | `createBoard` | Tworzy board; body `CreateBoardDto { name }`. Board startuje z **domyślną konfiguracją nadawaną po stronie serwera**. Zwraca `201` + uuid. |
| `GET /v1/boards/{boardId}` | `getBoardById` | Zwraca `BoardDto` + nagłówek `ETag` (wersja do optymistycznej współbieżności). |
| `PUT /v1/boards/{boardId}` | `updateBoard` | `If-Match` wymagany; body `UpdateBoardDto { name, configuration }`. `204 / 400 / 404 / 412`. |
| `DELETE /v1/boards/{boardId}` | `deleteBoard` | **Kasuje board wraz ze wszystkimi postaciami, relacjami i faktami na nim.** `204 / 404`. |

### `BoardConfigurationDto` (wszystkie pola wymagane)

| Pole | Typ / zakres | Znaczenie |
| --- | --- | --- |
| `characterNodeColor` | hex `^#[0-9a-fA-F]{6}$` | kolor węzłów postaci |
| `factNodeColor` | hex | kolor węzłów faktów |
| `relationEdgeColor` | hex | kolor krawędzi relacji |
| `factEdgeColor` | hex | kolor połączeń postać→fakt |
| `pathHighlightColor` | hex | kolor podświetlenia znalezionej ścieżki |
| `nodeRadius` | int 8–48 | promień węzła postaci (px) |
| `edgeWidth` | int 1–12 | grubość krawędzi relacji (px) |
| `curvedEdges` | bool | krzywe vs proste krawędzie |
| `showGrid` | bool | widoczność siatki tła |
| `scalingObjects` | bool | zoom skaluje obiekty razem z odległościami |

Domyślna konfiguracja (serwer musi ją nadawać przy `createBoard`):
`#4466cc / #d97706 / #aaaaaa / #d9a066 / #a855f7`, `nodeRadius=16`,
`edgeWidth=3`, `curvedEdges=true`, `showGrid=true`, `scalingObjects=true`
(odpowiada dotychczasowej, zaszytej na sztywno palecie frontendu — istniejące
grafy wyglądają po migracji identycznie).

### Przeniesienie istniejących ścieżek pod board

Każdy dotychczasowy endpoint danych dostał prefiks `/v1/boards/{boardId}`
(parametr `BoardId` zdefiniowany na poziomie ścieżki):

| Było | Jest |
| --- | --- |
| `/v1/characters` (w tym search po `nameFilter`) | `/v1/boards/{boardId}/characters` |
| `/v1/characters/{id}` | `/v1/boards/{boardId}/characters/{id}` |
| `/v1/characters/knows` | `/v1/boards/{boardId}/characters/knows` |
| `/v1/characters/knows/{from}/to/{to}` | `/v1/boards/{boardId}/characters/knows/{from}/to/{to}` |
| `/v1/characters/path/{from}/to/{to}` | `/v1/boards/{boardId}/characters/path/{from}/to/{to}` |
| `/v1/characters/{id}/facts` | `/v1/boards/{boardId}/characters/{id}/facts` |
| `/v1/characters/{characterId}/facts/{factId}` | `/v1/boards/{boardId}/characters/{characterId}/facts/{factId}` |
| `/v1/facts/{id}` | `/v1/boards/{boardId}/facts/{id}` |

### Tagi

Wszystkie operacje otagowane: `Boards`, `Characters`, `Relations`, `Facts`
(katalog tagów z opisami w sekcji `tags` na górze kontraktu).

---

## 2. Zmiany na frontendzie (LoreWeaveUI)

### Warstwa HTTP / serwis

- `LoreWeaveApiClient.ts` + `ValidationRules.ts` — przegenerowane NSwag-iem
  z kontraktu 2.0.0 (`scripts/sync-api/sync-api.sh`); generator reguł
  walidacji rozszerzony o `BOARD_NAME_MAX_LENGTH`.
- `LoreWeaveApiService` trzyma **aktywny board** (`setActiveBoard` /
  `activeBoardId`) i sam dokleja `boardId` do każdego wywołania — komponenty
  pozostały board-agnostyczne (żaden istniejący komponent nie zmienił
  wywołań). Żądanie danych bez ustawionego boardu rzuca błąd (bug okablowania,
  nie stan biznesowy).
- Nowe metody: `getBoardsAsync`, `createBoardAsync`, `getBoardAsync`
  (z wersją z ETag), `boardExistsAsync`, `updateBoardAsync` (If-Match),
  `deleteBoardAsync`.
- Nowe modele domenowe (`services/Models/`): `Board`, `VersionedBoard`,
  `UpdateBoard`, `BoardConfiguration` (z `createDefault()` i `clone()`).

### Styl grafu i legenda

- `useGraphConfiguration` przyjmuje opcjonalny reaktywny
  `boardConfiguration` i zwraca dodatkowo `palette` (computed): kolory boardu
  nałożone na domyślne, a warianty „ukryte dla graczy" **wyliczane** przez
  rozjaśnienie kolorów MG (`washOutColor`, 60% bieli). Opcje
  `nodeRadius/edgeWidth/curvedEdges/showGrid/scalingObjects` aplikowane
  reaktywnie — zapis w modalu widać natychmiast, bez przeładowania.
- `GraphLegendComponent` dostaje `palette` jako prop — legenda zawsze
  odzwierciedla to, co ustawił użytkownik, i nie może się rozjechać ze stylem
  grafu.

### Nowe komponenty

- `BoardSettingsComponent.vue` — modal MG: nazwa boardu, 5 kolorów
  (`<input type="color">`), rozmiar węzłów, grubość krawędzi oraz przełączniki
  krzywych krawędzi / siatki / skalowania przy zoomie. Wzorzec jak inne
  modale edycji: ładuje świeży board (i ETag) przy każdym otwarciu, `412`
  przeładowuje formularz i pozwala ponowić. Emituje `boardUpdated`.
- `SelectBoardComponent.vue` — modal wyboru/utworzenia boardu (tylko
  standalone). Pierwszy wybór (brak aktywnego boardu) jest niezamykalny;
  późniejsze przełączanie ma Cancel. Emituje `boardSelected(boardId)`.

### App.vue i hosty

- Nowy klucz wstrzykiwania `BOARD_RESOLVER_KEY`
  (`(() => Promise<string>) | null`).
- **Foundry** (`foundry/board-host.ts`): board jest skorelowany ze światem —
  world setting `boardId` (ukryty, world scope). Klient MG przy pierwszym
  otwarciu tworzy board o nazwie świata (`game.world.title`, przycięte do 50
  znaków) i zapisuje link; gracze tylko czytają setting. Link jest
  samonaprawialny: gdy backend nie zna zapisanego id (reset bazy), MG tworzy
  board ponownie. **Użytkownik w Foundry nigdy nie wybiera boardu.**
  `document-sync` (aktorzy/dzienniki → graf) buduje serwis per operacja już
  ze wskazanym boardem świata.
- **Standalone**: przy starcie przywracany ostatni board z `localStorage`
  (`loreweaveui:active-board`), w innym razie otwiera się picker. Przycisk
  „Boards" (prawy górny róg) pozwala przełączać boardy; obok nazwa aktywnego
  boardu i (dla MG) przycisk „Board settings".
- Zmiana boardu czyści zaznaczenie i podświetloną ścieżkę oraz przeładowuje
  graf. Cache pozycji węzłów pozostaje wspólny — id węzłów (uuid) są unikalne
  między boardami, więc wpisy się nie gryzą.

### Testy

Nowe/zaktualizowane specy: `BoardConfiguration`, `useGraphConfiguration`
(paleta per board, washOut, opcje widoku), `GraphLegendComponent` (paleta jako
prop), `SelectBoardComponent`, `BoardSettingsComponent` (w tym ścieżka 412),
`LoreWeaveApiService` (strażnik aktywnego boardu). Całość: 290 testów,
type-check, lint i oba buildy (standalone + Foundry) zielone.

---

## 3. Co musi zostać zaktualizowane na backendzie (RpgAssistant / LoreWeave)

Backend jest API-first — kontrakt 2.0.0 jest już źródłem prawdy; kod trzeba
do niego doprowadzić.

### Model danych

1. Nowy agregat/węzeł **Board** (`id`, `name`, `configuration`, `version`
   do ETag).
2. **Przynależność do boardu**: każda postać (a przez nią relacje i fakty)
   musi należeć do dokładnie jednego boardu. Wszystkie zapytania (paging,
   search po nazwie, path finding, pobrania po id) filtrowane po `boardId` —
   zasób istniejący na innym boardzie ma zwracać `404`, nie przeciekać.
3. **Migracja istniejących danych**: utworzyć board domyślny (np.
   „Default board") z domyślną konfiguracją i podpiąć pod niego wszystkie
   istniejące postacie/fakty — inaczej dane staną się nieosiągalne po zmianie
   ścieżek.

### API / aplikacja

4. DTO (`Api/Dtos/`): `BoardDto`, `CreateBoardDto`, `UpdateBoardDto`,
   `BoardConfigurationDto` — 1:1 ze schematami kontraktu.
5. Endpointy (`Api/Endpoints/BoardEndpoints.cs`): pięć operacji `Boards`
   z semantyką jak wyżej; `GET by id` zwraca wersję w nagłówku `ETag`
   (uint16, jak postacie), `PUT` waliduje `If-Match` → `412` przy niezgodności.
6. `createBoard` nadaje **domyślną konfigurację** (wartości w sekcji 1) —
   frontend celowo wysyła tylko nazwę.
7. `deleteBoard` kasuje **kaskadowo** wszystkie postacie, relacje KNOWS
   i fakty boardu (w tym fakty osierocone).
8. Routing istniejących endpointów: prefiks `/v1/boards/{boardId}` +
   walidacja istnienia boardu (`404` gdy board nie istnieje).
9. Walidacja `BoardConfigurationDto`: wzorzec hex dla 5 kolorów,
   `nodeRadius` 8–48, `edgeWidth` 1–12; `name` 1–50 znaków (jak postaci).
10. Mapowanie wyjątków domenowych na statusy w `ResultsToHttpResponses`
    (board not found → 404, konflikt wersji → 412).

### Testy backendu

11. Happy path + nie-happy dla boardów (walidacja, 404, 412) oraz testy
    **izolacji boardów**: postać z boardu A niewidoczna przez ścieżki boardu
    B (paging, search, path, get by id), kaskada `deleteBoard`.

### Uwagi

- Frontend zakłada, że `ETag` boardu działa identycznie jak dla postaci
  (cytowany uint16 zwracany w nagłówku, przyjmowany w `If-Match`).
- Foundry tworzy board automatycznie z nazwą świata — `createBoard` musi być
  idempotentny w sensie „wiele boardów o tej samej nazwie jest OK"
  (unikalność po `id`, nie po nazwie).
- Znane, wcześniejsze uwagi lintera kontraktu (nie z tej zmiany):
  `security-defined` (brak sekcji `security` w całym kontrakcie) oraz
  `minLength: 0` na polach opisowych — do ewentualnego osobnego sprzątania.
