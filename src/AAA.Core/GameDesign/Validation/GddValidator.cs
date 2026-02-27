using AAA.Core.GameDesign.Models;

namespace AAA.Core.GameDesign.Validation;

public class WynikWalidacji
{
    public IReadOnlyList<BladWalidacji> Bledy { get; init; } = [];
    public bool MaBledyKrytyczne => Bledy.Any(b => b.Waznosc == Waznosc.Blad);
}

public static class GddValidator
{
    public static WynikWalidacji Waliduj(GddDraft? draft)
    {
        var bledy = new List<BladWalidacji>();

        if (draft is null)
        {
            bledy.Add(BladWalidacji.Blad("GDD jest puste – brak danych do walidacji."));
            return new WynikWalidacji { Bledy = bledy };
        }

        WalidujPodstawoweInformacje(draft, bledy);
        WalidujSwiat(draft, bledy);
        WalidujPostacie(draft, bledy);
        WalidujQuesty(draft, bledy);

        return new WynikWalidacji { Bledy = bledy };
    }

    private static void WalidujPodstawoweInformacje(GddDraft draft, List<BladWalidacji> bledy)
    {
        if (string.IsNullOrWhiteSpace(draft.Tytul))
            bledy.Add(BladWalidacji.Blad("Tytuł gry jest wymagany.", "Tytul"));
    }

    private static void WalidujSwiat(GddDraft draft, List<BladWalidacji> bledy)
    {
        if (draft.Swiat is null)
        {
            if (draft.Gatunek == GatunekGry.OpenWorld)
                bledy.Add(BladWalidacji.Blad("Świat jest wymagany dla gatunku OpenWorld.", "Swiat"));
            else
                bledy.Add(BladWalidacji.Ostrzezenie("Brak definicji świata gry.", "Swiat"));
            return;
        }

        var swiat = draft.Swiat;

        if (draft.Gatunek == GatunekGry.OpenWorld && swiat.Regiony.Count == 0)
            bledy.Add(BladWalidacji.Blad("Gra OpenWorld wymaga zdefiniowania co najmniej jednego regionu.", "Swiat.Regiony"));

        // Walidacja regionów
        var idRegionow = new HashSet<string>(StringComparer.Ordinal);
        foreach (var region in swiat.Regiony)
        {
            if (string.IsNullOrWhiteSpace(region.Id))
            {
                bledy.Add(BladWalidacji.Blad("Region musi mieć niepuste Id.", "Swiat.Regiony"));
                continue;
            }

            if (!idRegionow.Add(region.Id))
                bledy.Add(BladWalidacji.Blad($"Duplikat Id regionu: \"{region.Id}\".", "Swiat.Regiony"));

            if (string.IsNullOrWhiteSpace(region.Nazwa))
                bledy.Add(BladWalidacji.Blad($"Region \"{region.Id}\" ma pustą nazwę.", $"Swiat.Regiony[{region.Id}].Nazwa"));
        }

        // Walidacja POI – sprawdzanie referencji do regionów
        foreach (var poi in swiat.PunktyZainteresowania)
        {
            if (!string.IsNullOrWhiteSpace(poi.RegionId) && !idRegionow.Contains(poi.RegionId))
                bledy.Add(BladWalidacji.Blad(
                    $"POI \"{poi.Nazwa ?? poi.Id}\" odwołuje się do nieistniejącego regionu \"{poi.RegionId}\".",
                    $"Swiat.PunktyZainteresowania[{poi.Id}].RegionId"));
        }
    }

    private static void WalidujPostacie(GddDraft draft, List<BladWalidacji> bledy)
    {
        var idPostaci = new HashSet<string>(StringComparer.Ordinal);
        foreach (var postac in draft.Postacie)
        {
            if (string.IsNullOrWhiteSpace(postac.Id))
            {
                bledy.Add(BladWalidacji.Blad("Postać musi mieć niepuste Id.", "Postacie"));
                continue;
            }

            if (!idPostaci.Add(postac.Id))
                bledy.Add(BladWalidacji.Blad($"Duplikat Id postaci: \"{postac.Id}\".", "Postacie"));

            if (string.IsNullOrWhiteSpace(postac.Nazwa))
                bledy.Add(BladWalidacji.Ostrzezenie($"Postać \"{postac.Id}\" ma pustą nazwę.", $"Postacie[{postac.Id}].Nazwa"));
        }
    }

    private static void WalidujQuesty(GddDraft draft, List<BladWalidacji> bledy)
    {
        var idQuestow = new HashSet<string>(StringComparer.Ordinal);
        foreach (var quest in draft.Questy)
        {
            if (string.IsNullOrWhiteSpace(quest.Id))
            {
                bledy.Add(BladWalidacji.Blad("Quest musi mieć niepuste Id.", "Questy"));
                continue;
            }

            if (!idQuestow.Add(quest.Id))
                bledy.Add(BladWalidacji.Blad($"Duplikat Id questa: \"{quest.Id}\".", "Questy"));

            if (string.IsNullOrWhiteSpace(quest.Tytul))
                bledy.Add(BladWalidacji.Blad($"Quest \"{quest.Id}\" musi mieć tytuł.", $"Questy[{quest.Id}].Tytul"));

            if (quest.Kroki.Count == 0)
                bledy.Add(BladWalidacji.Blad($"Quest \"{quest.Id}\" musi mieć co najmniej jeden krok.", $"Questy[{quest.Id}].Kroki"));
        }
    }
}
