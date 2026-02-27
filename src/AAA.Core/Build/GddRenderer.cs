using System.Text;
using AAA.Core.GameDesign.Models;

namespace AAA.Core.Build;

/// <summary>Renderer GDD do formatu Markdown i podglądu skróconego.</summary>
public static class GddRenderer
{
    private static string MapujGatunek(GatunekGry gatunek) => gatunek switch
    {
        GatunekGry.OpenWorld  => "Open World",
        GatunekGry.RPG        => "RPG",
        GatunekGry.FPS        => "FPS (First-Person Shooter)",
        GatunekGry.RTS        => "RTS (Real-Time Strategy)",
        GatunekGry.Platformowka => "Platformówka",
        _                     => "Nieznany"
    };

    /// <summary>Renderuje pełny Markdown dla podanego GddDraft.</summary>
    public static string RenderujMarkdown(GddDraft? draft)
    {
        if (draft is null)
            return "*(Brak GDD)*";

        var sb = new StringBuilder();

        sb.AppendLine($"# {draft.Tytul ?? "(brak tytułu)"}");
        sb.AppendLine();
        sb.AppendLine($"**Gatunek:** {MapujGatunek(draft.Gatunek)}");
        sb.AppendLine();

        // Sekcja świata
        sb.AppendLine("## 🌍 Świat");
        if (draft.Swiat is null)
        {
            sb.AppendLine("*(Brak definicji świata)*");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(draft.Swiat.Nazwa))
                sb.AppendLine($"**Nazwa:** {draft.Swiat.Nazwa}");
            if (!string.IsNullOrWhiteSpace(draft.Swiat.Opis))
                sb.AppendLine($"{draft.Swiat.Opis}");
            sb.AppendLine();

            if (draft.Swiat.Regiony.Count > 0)
            {
                sb.AppendLine("### Regiony");
                foreach (var r in draft.Swiat.Regiony)
                {
                    sb.AppendLine($"- **{r.Nazwa ?? r.Id ?? "(brak)"}**" +
                                  (string.IsNullOrWhiteSpace(r.Opis) ? "" : $" – {r.Opis}"));
                }
                sb.AppendLine();
            }

            if (draft.Swiat.PunktyZainteresowania.Count > 0)
            {
                sb.AppendLine("### Punkty zainteresowania");
                foreach (var poi in draft.Swiat.PunktyZainteresowania)
                {
                    sb.AppendLine($"- **{poi.Nazwa ?? poi.Id ?? "(brak)"}**" +
                                  (string.IsNullOrWhiteSpace(poi.RegionId) ? "" : $" (region: {poi.RegionId})"));
                }
                sb.AppendLine();
            }
        }

        // Sekcja postaci
        sb.AppendLine("## 👤 Postacie");
        if (draft.Postacie.Count == 0)
        {
            sb.AppendLine("*(Brak postaci)*");
        }
        else
        {
            foreach (var p in draft.Postacie)
            {
                sb.AppendLine($"- **{p.Nazwa ?? p.Id ?? "(brak)"}**" +
                              (string.IsNullOrWhiteSpace(p.Rola) ? "" : $" – {p.Rola}"));
            }
        }
        sb.AppendLine();

        // Sekcja questów
        sb.AppendLine("## ⚔ Questy");
        if (draft.Questy.Count == 0)
        {
            sb.AppendLine("*(Brak questów)*");
        }
        else
        {
            foreach (var q in draft.Questy)
            {
                sb.AppendLine($"### {q.Tytul ?? q.Id ?? "(brak)"}");
                if (!string.IsNullOrWhiteSpace(q.Opis))
                    sb.AppendLine(q.Opis);
                if (q.Kroki.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Kroki:**");
                    for (int i = 0; i < q.Kroki.Count; i++)
                        sb.AppendLine($"{i + 1}. {q.Kroki[i].Opis ?? "(brak opisu)"}");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>Renderuje krótkie podsumowanie GddDraft (emoji + jednolinijkowe sekcje).</summary>
    public static string RenderujPodgladKrotki(GddDraft? draft)
    {
        if (draft is null)
            return "*(Brak GDD)*";

        var sb = new StringBuilder();
        sb.AppendLine($"🎮 **{draft.Tytul ?? "(brak tytułu)"}** [{MapujGatunek(draft.Gatunek)}]");
        sb.AppendLine();

        if (draft.Swiat is not null)
        {
            sb.AppendLine($"🌍 Świat: {draft.Swiat.Nazwa ?? "(brak)"}" +
                          $" | {draft.Swiat.Regiony.Count} regionów" +
                          $" | {draft.Swiat.PunktyZainteresowania.Count} POI");
        }
        else
        {
            sb.AppendLine("🌍 Świat: *(brak)*");
        }

        sb.AppendLine($"👤 Postacie: {draft.Postacie.Count}" +
                      (draft.Postacie.Count > 0
                          ? $" ({string.Join(", ", draft.Postacie.Take(3).Select(p => p.Nazwa ?? p.Id))})"
                          : ""));
        sb.AppendLine($"⚔ Questy: {draft.Questy.Count}" +
                      (draft.Questy.Count > 0
                          ? $" ({string.Join(", ", draft.Questy.Take(3).Select(q => q.Tytul ?? q.Id))})"
                          : ""));

        return sb.ToString().TrimEnd();
    }
}
