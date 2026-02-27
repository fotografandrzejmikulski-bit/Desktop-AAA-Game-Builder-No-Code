using System.Text.Json;
using System.Text.Json.Serialization;
using AAA.Core.GameDesign.Models;

namespace AAA.Core.Storage;

public class GddDocument
{
    /// <summary>Roboczy szkic GDD (może zawierać błędy)</summary>
    public GddDraft? Roboczy { get; set; }

    /// <summary>Zatwierdzony GDD – używany przez podgląd i build</summary>
    public GddDraft? Zatwierdzony { get; set; }
}

public class ProjectState
{
    public string NazwaProjektu { get; set; } = "Nowy Projekt";
    public GddDocument Gdd { get; set; } = new();
    public DateTime DataUtworzenia { get; set; } = DateTime.UtcNow;
    public DateTime DataModyfikacji { get; set; } = DateTime.UtcNow;

    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Zapisuje stan projektu do pliku JSON.</summary>
    public void ZapiszDoPliku(string sciezka)
    {
        var json = JsonSerializer.Serialize(this, _opts);
        File.WriteAllText(sciezka, json, System.Text.Encoding.UTF8);
    }

    /// <summary>Wczytuje stan projektu z pliku JSON.</summary>
    public static ProjectState WczytajZPliku(string sciezka)
    {
        var json = File.ReadAllText(sciezka, System.Text.Encoding.UTF8);
        return JsonSerializer.Deserialize<ProjectState>(json, _opts)
               ?? throw new InvalidDataException("Plik projektu jest pusty lub nieprawidłowy.");
    }
}
