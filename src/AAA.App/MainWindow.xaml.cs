using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using AAA.Core.Conversation;
using AAA.Core.GameDesign.Models;
using AAA.Core.GameDesign.Validation;
using AAA.Core.Storage;

namespace AAA.App;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ChatWiadomosc> _wiadomosci = [];
    private ProjectState _stan = new();

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public MainWindow()
    {
        InitializeComponent();
        ChatItems.ItemsSource = _wiadomosci;
        DodajWiadomoscBota(
            "Witaj! Jestem AAA Game Builder.\n\n" +
            "Opisz swoją grę po polsku – powiedz mi o gatunku, świecie, postaciach i questach. " +
            "Możesz też wkleić gotowy JSON z definicją GDD.\n\n" +
            "Przykład: \"Zrób mi grę OpenWorld fantasy z dwoma regionami: Las Elfów i Miasto Ludzi.\"\n\n" +
            "Wpisz swój pomysł poniżej i kliknij Wyślij.");
    }

    private void Wyslij_Click(object sender, RoutedEventArgs e) => PrzetworzWejscie();

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            PrzetworzWejscie();
        }
    }

    private void NowaSesja_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
                "Czy na pewno chcesz rozpocząć nową sesję?\nBieżące dane projektu zostaną usunięte.",
                "Nowa sesja",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _wiadomosci.Clear();
            _stan = new ProjectState();
            ApproveButton.Visibility = Visibility.Collapsed;
            UstawStatus("Nowa sesja – gotowy.");
            DodajWiadomoscBota("Sesja zresetowana. Opisz nową grę.");
        }
    }

    private void Zatwierdz_Click(object sender, RoutedEventArgs e)
    {
        var wynik = WygenerujGreCommand.Zatwierdz(_stan);
        ApproveButton.Visibility = Visibility.Collapsed;

        if (wynik.Status == StatusKomendy.Sukces)
        {
            DodajWiadomoscBota("✅ GDD zostało zatwierdzone!\n\nMożesz teraz zbudować grę lub wyeksportować projekt.");
            UstawStatus("GDD zatwierdzone.");
        }
        else
        {
            DodajWiadomoscBota($"❌ Nie udało się zatwierdzić.\n\n{wynik.Komunikat}");
            UstawStatus("Błąd zatwierdzania.");
        }
    }

    private void PrzetworzWejscie()
    {
        var tekst = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(tekst)) return;

        DodajWiadomoscUzytkownika(tekst);
        InputBox.Clear();
        SendButton.IsEnabled = false;
        UstawStatus("Przetwarzam…");

        try
        {
            GddDraft? draft = ProbujParsowacJson(tekst) ?? BudujDraftZTekstu(tekst);
            var wynik = WygenerujGreCommand.Wykonaj(_stan, draft);
            PokazWynik(wynik);
        }
        catch (Exception ex)
        {
            DodajWiadomoscBota($"⚠ Wystąpił nieoczekiwany błąd:\n{ex.Message}");
            UstawStatus("Błąd.");
        }
        finally
        {
            SendButton.IsEnabled = true;
        }
    }

    private void PokazWynik(WynikKomendy wynik)
    {
        var sb = new StringBuilder();
        sb.AppendLine(wynik.Komunikat);

        if (wynik.Status == StatusKomendy.Sukces)
        {
            sb.AppendLine();
            sb.AppendLine("📋 Podgląd wygenerowanego GDD:");
            sb.AppendLine(PodgladGdd(wynik.WygenerowaneDraft));
            sb.AppendLine();
            sb.AppendLine("Jeśli GDD wygląda dobrze, kliknij ✔ Zatwierdź GDD.");
            ApproveButton.Visibility = Visibility.Visible;
            UstawStatus("GDD wygenerowane – oczekuje na zatwierdzenie.");
        }
        else if (wynik.Status == StatusKomendy.WymagaNaprawy)
        {
            sb.AppendLine();
            sb.AppendLine("🔧 Błędy walidacji:");
            if (wynik.ZadanieNaprawy?.Bledy is { Count: > 0 } bledy)
                foreach (var b in bledy)
                    sb.AppendLine($"  • [{b.Waznosc}] {b.Komunikat}");
            sb.AppendLine();
            sb.AppendLine("Popraw opis gry i spróbuj ponownie.");
            ApproveButton.Visibility = Visibility.Collapsed;
            UstawStatus("GDD wymaga naprawy.");
        }
        else
        {
            UstawStatus($"Status: {wynik.Status}");
        }

        DodajWiadomoscBota(sb.ToString().Trim());
    }

    // ── Helpery domenowe ──────────────────────────────────────────────────

    private static GddDraft? ProbujParsowacJson(string tekst)
    {
        var przyciete = tekst.TrimStart();
        if (!przyciete.StartsWith('{')) return null;
        try { return JsonSerializer.Deserialize<GddDraft>(tekst, _jsonOpts); }
        catch { return null; }
    }

    /// <summary>Buduje uproszczony GddDraft z opisu tekstowego.</summary>
    private static GddDraft BudujDraftZTekstu(string tekst)
    {
        var draft = new GddDraft { Tytul = WyekstrahujTytul(tekst) };

        if (tekst.Contains("OpenWorld", StringComparison.OrdinalIgnoreCase) ||
            tekst.Contains("otwart", StringComparison.OrdinalIgnoreCase))
            draft.Gatunek = GatunekGry.OpenWorld;
        else if (tekst.Contains("RPG", StringComparison.OrdinalIgnoreCase))
            draft.Gatunek = GatunekGry.RPG;
        else if (tekst.Contains("FPS", StringComparison.OrdinalIgnoreCase))
            draft.Gatunek = GatunekGry.FPS;
        else if (tekst.Contains("RTS", StringComparison.OrdinalIgnoreCase))
            draft.Gatunek = GatunekGry.RTS;
        else if (tekst.Contains("platformow", StringComparison.OrdinalIgnoreCase))
            draft.Gatunek = GatunekGry.Platformowka;

        draft.Swiat = new Swiat { Nazwa = "Świat", Opis = tekst };
        WyekstrahujRegiony(tekst, draft.Swiat);

        return draft;
    }

    private static string WyekstrahujTytul(string tekst)
    {
        // Spróbuj wyciągnąć tytuł podany po "gra", "gry", "pt.", "tytuł:"
        foreach (var prefix in new[] { "tytuł:", "tytuł :", "pt.", "gra \"", "gry \"", "gra: ", "gry: " })
        {
            var idx = tekst.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var start = idx + prefix.Length;
            var end = tekst.IndexOfAny(['\n', '"', ',', '.'], start);
            var fragment = (end < 0 ? tekst[start..] : tekst[start..end]).Trim().Trim('"');
            if (fragment.Length > 0) return fragment;
        }
        // Fallback: pierwsze 60 znaków
        return tekst.Length <= 60 ? tekst : tekst[..60].TrimEnd() + "…";
    }

    private static void WyekstrahujRegiony(string tekst, Swiat swiat)
    {
        // Proste wykrycie regionów po słowach kluczowych rozdzielonych przecinkami / " i "
        var dolny = tekst.ToLowerInvariant();
        var keywords = new[] { "region", "kraina", "obszar", "strefa", "teren" };
        foreach (var kw in keywords)
        {
            var idx = dolny.IndexOf(kw, StringComparison.Ordinal);
            if (idx < 0) continue;
            var fragment = tekst[(idx + kw.Length)..];
            var koniec = fragment.IndexOfAny(['\n', '.', ';']);
            if (koniec > 0) fragment = fragment[..koniec];
            // Rozdziel po przecinku i koniunkcji " i " (ze spacjami, aby nie kroić środków wyrazów)
            var nazwy = fragment
                .Split([','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .SelectMany(cz => cz.Split([" i "], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(n => n.Length > 2)
                .Distinct();
            var id = 1;
            foreach (var nazwa in nazwy)
            {
                swiat.Regiony.Add(new Region { Id = $"R{id++}", Nazwa = nazwa });
                if (swiat.Regiony.Count >= 10) break;
            }
            break;
        }
    }

    private static string PodgladGdd(GddDraft? draft)
    {
        if (draft is null) return "(brak)";
        var sb = new StringBuilder();
        sb.AppendLine($"  Tytuł:    {draft.Tytul ?? "(brak)"}");
        sb.AppendLine($"  Gatunek:  {draft.Gatunek}");
        if (draft.Swiat is not null)
        {
            sb.AppendLine($"  Świat:    {draft.Swiat.Nazwa ?? "(brak)"}");
            foreach (var r in draft.Swiat.Regiony)
                sb.AppendLine($"    • Region [{r.Id}]: {r.Nazwa}");
        }
        foreach (var p in draft.Postacie)
            sb.AppendLine($"  Postać:   [{p.Id}] {p.Nazwa} – {p.Rola}");
        foreach (var q in draft.Questy)
            sb.AppendLine($"  Quest:    [{q.Id}] {q.Tytul} ({q.Kroki.Count} kroków)");
        return sb.ToString().TrimEnd();
    }

    // ── UI helpery ────────────────────────────────────────────────────────

    private void DodajWiadomoscUzytkownika(string tekst)
        => DodajWiadomosc(tekst, isUser: true);

    private void DodajWiadomoscBota(string tekst)
        => DodajWiadomosc(tekst, isUser: false);

    private void DodajWiadomosc(string tekst, bool isUser)
    {
        _wiadomosci.Add(new ChatWiadomosc(tekst, isUser));
        ChatScroll.ScrollToBottom();
    }

    private void UstawStatus(string tekst) => StatusBar.Text = tekst;
}

public record ChatWiadomosc(string Text, bool IsUser)
{
    public Visibility UserVisibility => IsUser ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BotVisibility  => IsUser ? Visibility.Collapsed : Visibility.Visible;
}
