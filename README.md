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
    Conversation/     — obsługa komend czatu (WygenerujGreCommand)
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

### 🔲 Moduł 2 – Warstwa konwersacji (planowany)

- Parsowanie intencji użytkownika z tekstu po polsku
- Routing komend (generuj / zatwierdź / edytuj sekcję / pokaż podgląd)
- Historia rozmowy i kontekst sesji

### 🔲 Moduł 3 – Integracja z LLM (planowany)

- Klient HTTP do wybranego LLM (OpenAI / Azure OpenAI / lokalny model)
- Obsługa re-ask: wysłanie `RepairRequest.Prompt`, odebranie poprawionego JSON i ponowna walidacja
- Timeout i retry

### 🔲 Moduł 4 – Podgląd i Build (planowany)

- Renderer podglądu GDD w formacie czytelnym dla użytkownika
- Generowanie plików projektu gry na podstawie zatwierdzonego GDD

## Wymagania

- .NET 8.0 SDK
- Windows (docelowo WinUI 3 / WPF)

## Budowanie i testy

```bash
dotnet build
dotnet test
```

## Licencja

Projekt prywatny.
