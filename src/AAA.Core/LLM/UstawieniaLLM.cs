using System.Text.Json;
using System.Text.Json.Serialization;

namespace AAA.Core.LLM;

/// <summary>Ustawienia klienta LLM, persystowane w %APPDATA%\AAAGameBuilder\llm-settings.json.</summary>
public class UstawieniaLLM
{
    public string KluczApi { get; set; } = "";
    public string AdresEndpoint { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxTokenow { get; set; } = 4096;
    public int LimitCzasuSek { get; set; } = 60;
    public int MaxPonowien { get; set; } = 3;

    [JsonIgnore]
    public bool CzySkonfigurowany => !string.IsNullOrWhiteSpace(KluczApi);

    private static string SciezkaPliku =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AAAGameBuilder",
            "llm-settings.json");

    private static readonly JsonSerializerOptions _opcje = new() { WriteIndented = true };

    /// <summary>Wczytuje ustawienia z pliku. W razie błędu zwraca nowe, domyślne ustawienia.</summary>
    public static UstawieniaLLM Wczytaj()
    {
        try
        {
            var sciezka = SciezkaPliku;
            if (!File.Exists(sciezka))
                return new UstawieniaLLM();
            var json = File.ReadAllText(sciezka, System.Text.Encoding.UTF8);
            return JsonSerializer.Deserialize<UstawieniaLLM>(json, _opcje) ?? new UstawieniaLLM();
        }
        catch
        {
            return new UstawieniaLLM();
        }
    }

    /// <summary>Zapisuje ustawienia do pliku JSON.</summary>
    public void Zapisz()
    {
        var sciezka = SciezkaPliku;
        Directory.CreateDirectory(Path.GetDirectoryName(sciezka)!);
        var json = JsonSerializer.Serialize(this, _opcje);
        File.WriteAllText(sciezka, json, System.Text.Encoding.UTF8);
    }
}
