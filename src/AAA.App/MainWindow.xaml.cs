using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using AAA.Core.Build;
using AAA.Core.Conversation;
using AAA.Core.GameDesign.Models;
using AAA.Core.GameDesign.Validation;
using AAA.Core.LLM;
using AAA.Core.Storage;

namespace AAA.App;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ChatWiadomosc> _wiadomosci = [];
    private ProjectState _stan = new();
    private HistoriaRozmowy _historia = new();
    private UstawieniaLLM _ustawieniaLLM = UstawieniaLLM.Wczytaj();

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
        OdswiezStatusLLM();
        DodajWiadomoscBota(
            "Witaj! Jestem AAA Game Builder.\n\n" +
            "Opisz swoją grę po polsku – powiedz mi o gatunku, świecie, postaciach i questach. " +
            "Możesz też wkleić gotowy JSON z definicją GDD.\n\n" +
            "Przykład: \"Zrób mi grę OpenWorld fantasy z dwoma regionami: Las Elfów i Miasto Ludzi.\"\n\n" +
            "Wpisz swój pomysł poniżej i kliknij Wyślij.");
    }

    private async void Wyslij_Click(object sender, RoutedEventArgs e) => await PrzetworzWejscieAsync();

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true;
            await PrzetworzWejscieAsync();
        }
    }

    private void Ustawienia_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow(_ustawieniaLLM) { Owner = this };
        if (win.ShowDialog() == true)
        {
            _ustawieniaLLM = UstawieniaLLM.Wczytaj();
            OdswiezStatusLLM();
            DodajWiadomoscBota("⚙ Ustawienia LLM zapisane.");
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
            _historia.Wyczysc();
            ApproveButton.Visibility = Visibility.Collapsed;
            OdswiezPodglad();
            OdswiezPrzyciski();
            UstawStatus("Nowa sesja – gotowy.");
            DodajWiadomoscBota("Sesja zresetowana. Opisz nową grę.");
        }
    }

    private void ZapiszProjekt_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Zapisz projekt",
            Filter = "Projekt GDD (*.gdd.json)|*.gdd.json|Wszystkie pliki (*.*)|*.*",
            FileName = _stan.NazwaProjektu,
            DefaultExt = ".gdd.json"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            _stan.DataModyfikacji = DateTime.UtcNow;
            _stan.ZapiszDoPliku(dialog.FileName);
            UstawStatus($"Projekt zapisany: {dialog.FileName}");
            DodajWiadomoscBota($"✅ Projekt zapisany do pliku:\n{dialog.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie udało się zapisać projektu:\n{ex.Message}", "Błąd zapisu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OtworzProjekt_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Otwórz projekt",
            Filter = "Projekt GDD (*.gdd.json)|*.gdd.json|Wszystkie pliki (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            _stan = ProjectState.WczytajZPliku(dialog.FileName);
            _wiadomosci.Clear();
            ApproveButton.Visibility = _stan.Gdd.Roboczy is not null ? Visibility.Visible : Visibility.Collapsed;
            OdswiezPodglad();
            OdswiezPrzyciski();
            UstawStatus($"Projekt wczytany: {dialog.FileName}");
            DodajWiadomoscBota($"📂 Projekt wczytany z pliku:\n{dialog.FileName}\n\nNazwa projektu: {_stan.NazwaProjektu}");
            if (_stan.Gdd.Zatwierdzony is not null)
                DodajWiadomoscBota("ℹ Projekt zawiera zatwierdzony GDD. Możesz kontynuować pracę.");
            else if (_stan.Gdd.Roboczy is not null)
                DodajWiadomoscBota("ℹ Projekt zawiera roboczy GDD oczekujący na zatwierdzenie.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie udało się otworzyć projektu:\n{ex.Message}", "Błąd odczytu", MessageBoxButton.OK, MessageBoxImage.Error);
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

        OdswiezPodglad();
        OdswiezPrzyciski();
    }

    private void BudujProjekt_Click(object sender, RoutedEventArgs e)
    {
        if (_stan.Gdd.Zatwierdzony is null)
        {
            DodajWiadomoscBota("⚠ Brak zatwierdzonego GDD do zbudowania.");
            return;
        }

        var dlg = new OpenFolderDialog { Title = "Wybierz folder docelowy projektu" };
        if (dlg.ShowDialog() != true) return;

        var folder = Path.Combine(dlg.FolderName, _stan.NazwaProjektu.Replace(" ", "_"));
        try
        {
            var wynik = ProjectBuilder.Zbuduj(_stan.Gdd.Zatwierdzony, folder);
            if (wynik.Sukces)
            {
                DodajWiadomoscBota($"🔨 {wynik.Komunikat}\n\nWygenerowane pliki ({wynik.LiczbaWygenerowanchPlikow}):\n" +
                    string.Join("\n", wynik.WygenerowanePliki.Select(p => $"  • {Path.GetFileName(p)}")));
                UstawStatus($"Projekt zbudowany: {folder}");
                try { Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true }); }
                catch (Exception ex) { UstawStatus($"Projekt zbudowany (nie można otworzyć folderu: {ex.Message})"); }
            }
            else
            {
                DodajWiadomoscBota($"❌ {wynik.Komunikat}");
            }
        }
        catch (Exception ex)
        {
            DodajWiadomoscBota($"⚠ Błąd budowania: {ex.Message}");
        }
    }

    private void EksportujMd_Click(object sender, RoutedEventArgs e)
    {
        var draft = _stan.Gdd.Zatwierdzony ?? _stan.Gdd.Roboczy;
        if (draft is null)
        {
            DodajWiadomoscBota("⚠ Brak GDD do eksportu.");
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = "Eksportuj GDD jako Markdown",
            Filter = "Markdown (*.md)|*.md|Wszystkie pliki (*.*)|*.*",
            FileName = (draft.Tytul ?? "gdd").Replace(" ", "_"),
            DefaultExt = ".md"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            File.WriteAllText(dlg.FileName, GddRenderer.RenderujMarkdown(draft), System.Text.Encoding.UTF8);
            UstawStatus($"GDD wyeksportowane: {dlg.FileName}");
            DodajWiadomoscBota($"📄 GDD wyeksportowane do:\n{dlg.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd eksportu:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task PrzetworzWejscieAsync()
    {
        var tekst = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(tekst)) return;

        var intencja = AnalizatorIntencji.Analizuj(tekst);
        _historia.DodajWiadomoscUzytkownika(tekst, intencja);
        DodajWiadomoscUzytkownika(tekst);
        InputBox.Clear();
        SendButton.IsEnabled = false;
        UstawStatus("Przetwarzam…");

        try
        {
            switch (intencja)
            {
                case IntencjaRozmowy.ZatwierdźGdd:
                    Zatwierdz_Click(this, new RoutedEventArgs());
                    break;

                case IntencjaRozmowy.PokazPodglad:
                    OdswiezPodglad();
                    var podglad = GddRenderer.RenderujPodgladKrotki(_stan.Gdd.Zatwierdzony ?? _stan.Gdd.Roboczy);
                    DodajWiadomoscBota(podglad);
                    break;

                case IntencjaRozmowy.NowaSesja:
                    NowaSesja_Click(this, new RoutedEventArgs());
                    break;

                case IntencjaRozmowy.PokazBledy:
                    var draft0 = _stan.Gdd.Roboczy ?? _stan.Gdd.Zatwierdzony;
                    if (draft0 is null)
                    {
                        DodajWiadomoscBota("ℹ Brak GDD do walidacji.");
                    }
                    else
                    {
                        var wynikW = GddValidator.Waliduj(draft0);
                        if (!wynikW.Bledy.Any())
                        {
                            DodajWiadomoscBota("✅ GDD jest poprawne – brak błędów.");
                        }
                        else
                        {
                            var sb0 = new StringBuilder("📋 Wynik walidacji:\n");
                            foreach (var b in wynikW.Bledy)
                                sb0.AppendLine($"  [{b.Waznosc}] {b.Komunikat}");
                            DodajWiadomoscBota(sb0.ToString().TrimEnd());
                        }
                    }
                    break;

                default:
                    // GenerujGre, EdytujSekcje, Nieznana
                    GddDraft? draft = ProbujParsowacJson(tekst) ?? BudujDraftZTekstu(tekst);
                    var wynik = WygenerujGreCommand.Wykonaj(_stan, draft);

                    if (wynik.Status == StatusKomendy.WymagaNaprawy && _ustawieniaLLM.CzySkonfigurowany)
                    {
                        DodajWiadomoscBota("⚠ GDD zawiera błędy krytyczne. Wysyłam do AI w celu automatycznej naprawy…");
                        UstawStatus("AI naprawia GDD…");

                        var orkiestrator = new OrkiestratorNaprawy(new OpenAiKlientLLM(_ustawieniaLLM));
                        var wynikNaprawy = await orkiestrator.NaprawAsync(wynik.ZadanieNaprawy!, _ustawieniaLLM.MaxPonowien);

                        if (wynikNaprawy.Sukces && wynikNaprawy.NaprawioneDraft is not null)
                        {
                            var wynikPonowny = WygenerujGreCommand.Wykonaj(_stan, wynikNaprawy.NaprawioneDraft);
                            PokazWynik(wynikPonowny);
                            DodajWiadomoscBota($"🤖 {wynikNaprawy.Komunikat}");
                        }
                        else
                        {
                            DodajWiadomoscBota($"🤖 {wynikNaprawy.Komunikat}");
                            PokazWynik(wynik);
                        }
                    }
                    else
                    {
                        PokazWynik(wynik);
                    }

                    OdswiezPodglad();
                    OdswiezPrzyciski();
                    break;
            }

            UstawStatus("Gotowy.");
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

    // ── Helpery odświeżania UI ────────────────────────────────────────────

    private void OdswiezPodglad()
    {
        var draft = _stan.Gdd.Zatwierdzony ?? _stan.Gdd.Roboczy;
        if (draft is null)
        {
            PreviewText.Text = "Brak GDD do wyświetlenia.\n\nOpisz swoją grę w polu czatu po lewej stronie.";
            GddStatusText.Text = "";
        }
        else
        {
            PreviewText.Text = GddRenderer.RenderujMarkdown(draft);
            GddStatusText.Text = _stan.Gdd.Zatwierdzony is not null
                ? "✅ Zatwierdzony"
                : "⚠ Roboczy (oczekuje na zatwierdzenie)";
        }
    }

    private void OdswiezPrzyciski()
    {
        bool maRoboczy = _stan.Gdd.Roboczy is not null;
        bool maZatwierdzony = _stan.Gdd.Zatwierdzony is not null;
        ApproveButton.Visibility = (maRoboczy && !maZatwierdzony) ? Visibility.Visible : Visibility.Collapsed;
        BuildButton.Visibility = maZatwierdzony ? Visibility.Visible : Visibility.Collapsed;
        ExportMdButton.Visibility = (maRoboczy || maZatwierdzony) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OdswiezStatusLLM()
    {
        LlmStatusText.Text = _ustawieniaLLM.CzySkonfigurowany
            ? $"🤖 AI: {_ustawieniaLLM.Model}"
            : "🤖 AI: Brak klucza";
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
        foreach (var prefix in new[] { "tytuł:", "tytuł :", "pt.", "gra \"", "gry \"", "gra: ", "gry: " })
        {
            var idx = tekst.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var start = idx + prefix.Length;
            var end = tekst.IndexOfAny(['\n', '"', ',', '.'], start);
            var fragment = (end < 0 ? tekst[start..] : tekst[start..end]).Trim().Trim('"');
            if (fragment.Length > 0) return fragment;
        }
        return tekst.Length <= 60 ? tekst : tekst[..60].TrimEnd() + "…";
    }

    private static void WyekstrahujRegiony(string tekst, Swiat swiat)
    {
        var dolny = tekst.ToLowerInvariant();
        var keywords = new[] { "region", "kraina", "obszar", "strefa", "teren" };
        foreach (var kw in keywords)
        {
            var idx = dolny.IndexOf(kw, StringComparison.Ordinal);
            if (idx < 0) continue;
            var fragment = tekst[(idx + kw.Length)..];
            var koniec = fragment.IndexOfAny(['\n', '.', ';']);
            if (koniec > 0) fragment = fragment[..koniec];
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
