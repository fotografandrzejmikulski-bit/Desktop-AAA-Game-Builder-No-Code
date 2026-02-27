using System.Text.Json;
using System.Text.Json.Serialization;
using AAA.Core.GameDesign.AutoNaprawa;
using AAA.Core.GameDesign.Models;
using AAA.Core.GameDesign.Validation;

namespace AAA.Core.LLM;

public class WynikNaprawy
{
    public bool Sukces { get; init; }
    public GddDraft? NaprawioneDraft { get; init; }
    public string Komunikat { get; init; } = "";
    public int LiczbaProb { get; init; }
}

/// <summary>Orkiestrator iteracyjnej naprawy GDD przy pomocy LLM.</summary>
public class OrkiestratorNaprawy(IKlientLLM klient)
{
    private static readonly JsonSerializerOptions _jsonOpcje = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<WynikNaprawy> NaprawAsync(
        RepairRequest zadanie,
        int maxProb = 3,
        CancellationToken ct = default)
    {
        var aktualneZadanie = zadanie;

        for (int proba = 1; proba <= maxProb; proba++)
        {
            try
            {
                var odpowiedz = await klient.WyslijPromptAsync(aktualneZadanie.Prompt, ct);
                var oczyszczony = WyekstrahujJson(odpowiedz);

                GddDraft? draft;
                try
                {
                    draft = JsonSerializer.Deserialize<GddDraft>(oczyszczony, _jsonOpcje);
                }
                catch (JsonException ex)
                {
                    return new WynikNaprawy
                    {
                        Sukces = false,
                        Komunikat = $"Błąd parsowania odpowiedzi LLM (próba {proba}/{maxProb}): {ex.Message}",
                        LiczbaProb = proba
                    };
                }

                if (draft is null)
                {
                    return new WynikNaprawy
                    {
                        Sukces = false,
                        Komunikat = $"Nie udało się sparsować odpowiedzi LLM jako GddDraft (próba {proba}/{maxProb}).",
                        LiczbaProb = proba
                    };
                }

                var wynikWalidacji = GddValidator.Waliduj(draft);

                if (!wynikWalidacji.MaBledyKrytyczne)
                {
                    return new WynikNaprawy
                    {
                        Sukces = true,
                        NaprawioneDraft = draft,
                        Komunikat = $"GDD naprawione przez AI w {proba} próbie/próbach.",
                        LiczbaProb = proba
                    };
                }

                // Nadal są błędy – przygotuj nowe żądanie naprawy
                aktualneZadanie = RepairRequestBuilder.Zbuduj(draft, wynikWalidacji);
            }
            catch (Exception ex)
            {
                return new WynikNaprawy
                {
                    Sukces = false,
                    Komunikat = $"Błąd podczas komunikacji z LLM: {ex.Message}",
                    LiczbaProb = proba
                };
            }
        }

        return new WynikNaprawy
        {
            Sukces = false,
            Komunikat = $"Nie udało się naprawić GDD po {maxProb} próbach.",
            LiczbaProb = maxProb
        };
    }

    /// <summary>Usuwa bloki ```json``` i wyodrębnia pierwszą parę nawiasów klamrowych.</summary>
    private static string WyekstrahujJson(string tekst)
    {
        // Usuń opcjonalne bloki markdown ```json ... ```
        tekst = tekst.Trim();
        if (tekst.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            tekst = tekst["```json".Length..].TrimStart();
        if (tekst.StartsWith("```"))
            tekst = tekst["```".Length..].TrimStart();
        if (tekst.EndsWith("```"))
            tekst = tekst[..^"```".Length].TrimEnd();

        // Wyodrębnij pierwszy blok { ... }
        var start = tekst.IndexOf('{');
        if (start < 0) return tekst;
        var end = tekst.LastIndexOf('}');
        if (end < start) return tekst;
        return tekst[start..(end + 1)];
    }
}
