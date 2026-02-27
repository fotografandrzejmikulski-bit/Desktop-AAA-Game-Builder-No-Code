using System.Text.Json;
using System.Text.Json.Serialization;
using AAA.Core.GameDesign.Models;

namespace AAA.Core.Build;

/// <summary>Buduje pliki projektu gry na podstawie zatwierdzonego GddDraft.</summary>
public static class ProjectBuilder
{
    private static readonly JsonSerializerOptions _jsonOpcje = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static WynikBudowania Zbuduj(GddDraft draft, string katalogWyjsciowy)
    {
        var pliki = new List<string>();

        try
        {
            Directory.CreateDirectory(katalogWyjsciowy);

            // game_config.json
            var configSciezka = Path.Combine(katalogWyjsciowy, "game_config.json");
            var config = new
            {
                title = draft.Tytul,
                genre = draft.Gatunek.ToString(),
                builtAt = DateTime.UtcNow,
                version = "1.0.0"
            };
            File.WriteAllText(configSciezka, JsonSerializer.Serialize(config, _jsonOpcje), System.Text.Encoding.UTF8);
            pliki.Add(configSciezka);

            // gdd.md
            var gddMdSciezka = Path.Combine(katalogWyjsciowy, "gdd.md");
            File.WriteAllText(gddMdSciezka, GddRenderer.RenderujMarkdown(draft), System.Text.Encoding.UTF8);
            pliki.Add(gddMdSciezka);

            // world/world.json
            if (draft.Swiat is not null)
            {
                var worldDir = Path.Combine(katalogWyjsciowy, "world");
                Directory.CreateDirectory(worldDir);
                var worldSciezka = Path.Combine(worldDir, "world.json");
                var worldData = new
                {
                    name = draft.Swiat.Nazwa,
                    description = draft.Swiat.Opis,
                    regions = draft.Swiat.Regiony,
                    pointsOfInterest = draft.Swiat.PunktyZainteresowania
                };
                File.WriteAllText(worldSciezka, JsonSerializer.Serialize(worldData, _jsonOpcje), System.Text.Encoding.UTF8);
                pliki.Add(worldSciezka);
            }

            // characters/characters.json
            if (draft.Postacie.Count > 0)
            {
                var charsDir = Path.Combine(katalogWyjsciowy, "characters");
                Directory.CreateDirectory(charsDir);
                var charsSciezka = Path.Combine(charsDir, "characters.json");
                File.WriteAllText(charsSciezka, JsonSerializer.Serialize(draft.Postacie, _jsonOpcje), System.Text.Encoding.UTF8);
                pliki.Add(charsSciezka);
            }

            // quests/quests.json
            if (draft.Questy.Count > 0)
            {
                var questsDir = Path.Combine(katalogWyjsciowy, "quests");
                Directory.CreateDirectory(questsDir);
                var questsSciezka = Path.Combine(questsDir, "quests.json");
                File.WriteAllText(questsSciezka, JsonSerializer.Serialize(draft.Questy, _jsonOpcje), System.Text.Encoding.UTF8);
                pliki.Add(questsSciezka);
            }

            return new WynikBudowania
            {
                Sukces = true,
                Komunikat = $"Projekt '{draft.Tytul}' zbudowany pomyślnie.",
                WygenerowanePliki = pliki,
                KatalogWyjsciowy = katalogWyjsciowy
            };
        }
        catch (Exception ex)
        {
            return new WynikBudowania
            {
                Sukces = false,
                Komunikat = $"Błąd budowania projektu: {ex.Message}",
                WygenerowanePliki = pliki,
                KatalogWyjsciowy = katalogWyjsciowy
            };
        }
    }
}
