using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AAA.Core.LLM;

/// <summary>Klient HTTP kompatybilny z API OpenAI (ChatCompletion).</summary>
public class OpenAiKlientLLM : IKlientLLM
{
    private static readonly HttpClient _httpClient = new();
    private readonly UstawieniaLLM _ustawienia;

    public OpenAiKlientLLM(UstawieniaLLM ustawienia)
    {
        _ustawienia = ustawienia;
    }

    public async Task<string> WyslijPromptAsync(string prompt, CancellationToken ct = default)
    {
        var url = $"{_ustawienia.AdresEndpoint.TrimEnd('/')}/chat/completions";

        var cialoZadania = new
        {
            model = _ustawienia.Model,
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = _ustawienia.MaxTokenow
        };

        var jsonCialo = JsonSerializer.Serialize(cialoZadania);

        for (int proba = 1; proba <= _ustawienia.MaxPonowien; proba++)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_ustawienia.LimitCzasuSek));

            try
            {
                using var zadanie = new HttpRequestMessage(HttpMethod.Post, url);
                zadanie.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _ustawienia.KluczApi);
                zadanie.Content = new StringContent(jsonCialo, Encoding.UTF8, "application/json");

                var odpowiedz = await _httpClient.SendAsync(zadanie, cts.Token);

                // 401 – nie ponawiaj
                if (odpowiedz.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new InvalidOperationException("Nieprawidłowy klucz API (401 Unauthorized).");

                // 429 – poczekaj i ponów
                if (odpowiedz.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (proba < _ustawienia.MaxPonowien)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5 * proba), ct);
                        continue;
                    }
                    odpowiedz.EnsureSuccessStatusCode();
                }

                odpowiedz.EnsureSuccessStatusCode();

                var jsonOdpowiedzi = await odpowiedz.Content.ReadAsStringAsync(cts.Token);
                return WyekstrahujTresc(jsonOdpowiedzi);
            }
            catch (InvalidOperationException)
            {
                // Błąd autoryzacji – nie ponawiaj
                throw;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Przekroczono limit czasu
                if (proba >= _ustawienia.MaxPonowien)
                    throw new TimeoutException($"Przekroczono limit czasu ({_ustawienia.LimitCzasuSek}s) po {proba} próbach.");
            }
            catch (HttpRequestException) when (proba < _ustawienia.MaxPonowien)
            {
                // Błąd sieci – ponów
            }
        }

        throw new HttpRequestException($"Nie udało się połączyć po {_ustawienia.MaxPonowien} próbach.");
    }

    private static string WyekstrahujTresc(string json)
    {
        using var dokument = JsonDocument.Parse(json);
        return dokument.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }
}
