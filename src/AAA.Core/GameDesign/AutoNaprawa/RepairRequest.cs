using System.Text;
using System.Text.Json;
using AAA.Core.GameDesign.Models;
using AAA.Core.GameDesign.Validation;

namespace AAA.Core.GameDesign.AutoNaprawa;

/// <summary>Dane do żądania naprawy GDD przez AI</summary>
public class RepairRequest
{
    public string Prompt { get; init; } = string.Empty;
    public GddDraft WadliwyDraft { get; init; } = new();
    public IReadOnlyList<BladWalidacji> Bledy { get; init; } = [];
}

public static class RepairRequestBuilder
{
    private static readonly JsonSerializerOptions JsonOpcje = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static RepairRequest Zbuduj(GddDraft wadliwyDraft, WynikWalidacji wynikWalidacji, GddDraft? zatwierdzonyDraft = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Napraw poniższy obiekt GddDraft zgodnie ze wskazanymi błędami.");
        sb.AppendLine();
        sb.AppendLine("## Zasady:");
        sb.AppendLine("1. Zwróć WYŁĄCZNIE poprawny JSON GddDraft bez żadnych komentarzy ani dodatkowego tekstu.");
        sb.AppendLine("2. Nie zmieniaj zatwierdzonych sekcji GDD.");
        sb.AppendLine("3. Jeśli brakuje danych, dodaj minimalne sensowne wartości.");
        sb.AppendLine("4. Zachowaj wszystkie istniejące Id i nie duplikuj ich.");
        sb.AppendLine();

        sb.AppendLine("## Błędy do naprawy:");
        var bledy = wynikWalidacji.Bledy.Where(b => b.Waznosc == Waznosc.Blad).ToList();
        var ostrzezenia = wynikWalidacji.Bledy.Where(b => b.Waznosc == Waznosc.Ostrzezenie).ToList();

        foreach (var blad in bledy)
            sb.AppendLine($"- [BŁĄD] {blad.Komunikat}{(blad.Sciezka != null ? $" (ścieżka: {blad.Sciezka})" : "")}");

        foreach (var ostrz in ostrzezenia)
            sb.AppendLine($"- [OSTRZEŻENIE] {ostrz.Komunikat}{(ostrz.Sciezka != null ? $" (ścieżka: {ostrz.Sciezka})" : "")}");

        sb.AppendLine();

        if (zatwierdzonyDraft is not null)
        {
            sb.AppendLine("## Zatwierdzone sekcje (NIE ZMIENIAJ):");
            sb.AppendLine("```json");
            sb.AppendLine(JsonSerializer.Serialize(zatwierdzonyDraft, JsonOpcje));
            sb.AppendLine("```");
            sb.AppendLine();
        }

        sb.AppendLine("## Wadliwy GddDraft do naprawy:");
        sb.AppendLine("```json");
        sb.AppendLine(JsonSerializer.Serialize(wadliwyDraft, JsonOpcje));
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("Zwróć poprawiony GddDraft jako czysty JSON:");

        return new RepairRequest
        {
            Prompt = sb.ToString(),
            WadliwyDraft = wadliwyDraft,
            Bledy = wynikWalidacji.Bledy
        };
    }
}
