# Desktop-AAA-Game-Builder-No-Code

Aplikacja desktopowa (Windows) do tworzenia gier AAA bez pisania kodu – sterowana rozmową po polsku.

## Opis

No-code game builder napędzany konwersacją z AI. Użytkownik opisuje swoją grę w języku polskim, a aplikacja generuje, waliduje i zatwierdza dokument Game Design Document (GDD), na podstawie którego budowana jest gra.

## Architektura

```
src/
  AAA.Core/
    GameDesign/
      Models/         — modele domenowe (GddDraft, Swiat, Region, Postac, Quest…)
      Validation/     — walidator GDD z komunikatami po polsku
      AutoNaprawa/    — budowanie promptu do AI w celu naprawy błędnego GDD
    Storage/          — stan projektu (ProjectState, GddDocument)
    Conversation/     — obsługa komend czatu, wykrywanie intencji, historia rozmowy
    LLM/              — interfejs IKlientLLM, OpenAiKlientLLM, OrkiestratorNaprawy, UstawieniaLLM
    Build/            — GddRenderer (Markdown), ProjectBuilder (pliki projektu)
tests/
  AAA.Core.Tests/     — testy jednostkowe xUnit
```

## Moduły

### ✅ Moduł 1 – GDD Schema + Walidator + Auto-naprawa

- **Modele GDD** – `GddDraft`, `Swiat`, `Region`, `PunktZainteresowania`, `Postac`, `Quest`, `KrokQuestu`
- **GddDocument** – przechowuje roboczy (`Roboczy`) i zatwierdzony (`Zatwierdzony`) GDD; podgląd i build używają wyłącznie zatwierdzonego
- **GddValidator** – walidacja strukturalna z listą `BladWalidacji` (Blad / Ostrzezenie / Info):
  - tytuł gry jest wymagany
  - OpenWorld wymaga co najmniej jednego regionu
  - unikalne ID regionów, postaci i questów
  - niepuste nazwy regionów
  - prawidłowe referencje `RegionId` w POI
  - questy muszą mieć tytuł i co najmniej jeden krok
- **RepairRequestBuilder** – buduje deterministyczny prompt do AI zawierający streszczenie błędów, wadliwy GDD (JSON) i zasady naprawy
- **WygenerujGreCommand** – integruje walidację z przepływem generowania gry; przy błędach krytycznych zwraca `RepairRequest` zamiast proponować zatwierdzenie
- **8 testów jednostkowych** (xUnit)

### ✅ Moduł 2 – Warstwa konwersacji

- Parsowanie intencji użytkownika z tekstu po polsku (`AnalizatorIntencji`)
- Routing komend (generuj / zatwierdź / edytuj sekcję / pokaż podgląd / nowa sesja / pokaż błędy)
- Historia rozmowy i kontekst sesji (`HistoriaRozmowy`)

### ✅ Moduł 3 – Integracja z LLM

- Klient HTTP kompatybilny z API OpenAI (`OpenAiKlientLLM`) – retry, timeout, obsługa 401/429
- Persystowane ustawienia LLM (`UstawieniaLLM`) – klucz API, endpoint, model
- Orkiestrator iteracyjnej naprawy GDD przez AI (`OrkiestratorNaprawy`)
- Okno ustawień LLM z testem połączenia (`SettingsWindow`)

### ✅ Moduł 4 – Podgląd i Build

- Renderer podglądu GDD w formacie Markdown (`GddRenderer`)
- Generowanie plików projektu gry na podstawie zatwierdzonego GDD (`ProjectBuilder`)
- Panel podglądu GDD w MainWindow (split-panel layout)
- Eksport GDD do pliku Markdown

## Wymagania

- .NET 8.0 SDK
- Windows 10 lub nowszy (WPF)

## Budowanie i testy

```bash
dotnet build
dotnet test
```

## Budowanie instalatora

Plik instalacyjny (`AAAGameBuilder-Setup-vX.Y.Z.exe`) jest generowany automatycznie przez CI/CD przy każdym pushu do `main`, pull requeście oraz przy tworzeniu tagu `vX.Y.Z`.

### Automatyczne budowanie przez GitHub Actions

Workflow `.github/workflows/build-installer.yml` wykonuje kolejno:
1. Przywrócenie zależności i uruchomienie testów
2. `dotnet publish` – publikacja aplikacji jako self-contained single-file (win-x64, brak wymagania .NET Runtime)
3. Kompilacja instalatora Inno Setup 6
4. Wyliczenie sumy kontrolnej SHA-256 i zapis do `SHA256SUMS.txt`
5. Upload artefaktów do GitHub Actions
6. *(tylko przy tagu `v*`)* – dołączenie instalatora do GitHub Release

Artefakty są dostępne w zakładce **Actions → wybierz run → Artifacts**.

### Ręczne budowanie

#### 1. Wymagania wstępne
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (zainstalowany w domyślnej lokalizacji)

#### 2. Publikacja aplikacji

```powershell
dotnet publish src/AAA.App/AAA.App.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output publish `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

#### 3. Kompilacja instalatora

```cmd
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup.iss
```

Wynikowy plik `AAAGameBuilder-Setup-v1.0.0.exe` zostanie zapisany w katalogu `artifacts\`.

#### 4. Tworzenie oficjalnego Release

```bash
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions automatycznie zbuduje instalator i dołączy go do Release.

### Zawartość instalatora

Instalator (Inno Setup 6) wykonuje:
- Instalację aplikacji do `%ProgramFiles%\AAA Game Builder\`
- Utworzenie grupy w Menu Start
- Opcjonalny skrót na pulpicie
- Wpis w Panelu Sterowania → Dodaj/Usuń programy (z ikoną i wersją)
- Możliwość uruchomienia aplikacji po zakończeniu instalacji
- Czysty dezinstalator usuwający wszystkie pliki aplikacji

## Licencja

Projekt prywatny.
