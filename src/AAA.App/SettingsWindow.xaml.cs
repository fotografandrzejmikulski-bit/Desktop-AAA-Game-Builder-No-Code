using System.Windows;
using AAA.Core.LLM;

namespace AAA.App;

public partial class SettingsWindow : Window
{
    private readonly UstawieniaLLM _ustawienia;

    public SettingsWindow(UstawieniaLLM ustawienia)
    {
        InitializeComponent();
        _ustawienia = ustawienia;

        ApiKeyBox.Password = ustawienia.KluczApi;
        EndpointBox.Text = ustawienia.AdresEndpoint;
        ModelBox.Text = ustawienia.Model;
    }

    private void Zapisz_Click(object sender, RoutedEventArgs e)
    {
        var klucz = ApiKeyBox.Password.Trim();
        var endpoint = EndpointBox.Text.Trim();
        var model = ModelBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            MessageBox.Show("Adres endpoint nie może być pusty.", "Błąd walidacji",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            MessageBox.Show("Nazwa modelu nie może być pusta.", "Błąd walidacji",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _ustawienia.KluczApi = klucz;
        _ustawienia.AdresEndpoint = endpoint;
        _ustawienia.Model = model;
        _ustawienia.Zapisz();

        DialogResult = true;
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        TestButton.IsEnabled = false;
        try
        {
            var testUstawienia = new UstawieniaLLM
            {
                KluczApi = ApiKeyBox.Password.Trim(),
                AdresEndpoint = EndpointBox.Text.Trim(),
                Model = ModelBox.Text.Trim(),
                MaxTokenow = 10,
                LimitCzasuSek = 15,
                MaxPonowien = 1
            };

            if (!testUstawienia.CzySkonfigurowany)
            {
                MessageBox.Show("Podaj klucz API przed testowaniem połączenia.", "Brak klucza",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var klient = new OpenAiKlientLLM(testUstawienia);
            var odpowiedz = await klient.WyslijPromptAsync("Odpowiedz jednym słowem: OK");

            MessageBox.Show($"✅ Połączenie nawiązane!\n\nOdpowiedź: {odpowiedz}", "Test połączenia",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"❌ Błąd połączenia:\n{ex.Message}", "Test połączenia",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private void Anuluj_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
