using AAA.Core.GameDesign.AutoNaprawa;
using AAA.Core.GameDesign.Models;
using AAA.Core.GameDesign.Validation;
using AAA.Core.LLM;
using Xunit;

namespace AAA.Core.Tests;

public class OrkiestratorNaprawyTests
{
    // Minimalny poprawny GddDraft JSON (musi przejść GddValidator)
    private const string PoprawnyGddJson = """
        {
          "Tytul": "Gra Testowa",
          "Gatunek": "RPG",
          "Swiat": {
            "Nazwa": "Testowy Świat",
            "Regiony": [{ "Id": "r1", "Nazwa": "Region Testowy" }],
            "PunktyZainteresowania": []
          },
          "Postacie": [{ "Id": "p1", "Nazwa": "Testowy Bohater", "Rola": "Gracz" }],
          "Questy": [{
            "Id": "q1",
            "Tytul": "Testowy Quest",
            "Kroki": [{ "Id": "k1", "Opis": "Zrób coś" }]
          }]
        }
        """;

    private class MockLLM(string odpowiedz) : IKlientLLM
    {
        public Task<string> WyslijPromptAsync(string prompt, CancellationToken ct = default)
            => Task.FromResult(odpowiedz);
    }

    private class ThrowingLLM : IKlientLLM
    {
        public Task<string> WyslijPromptAsync(string prompt, CancellationToken ct = default)
            => throw new InvalidOperationException("Symulowany błąd sieci");
    }

    private static RepairRequest BudujTestoweZadanie()
    {
        var draft = new GddDraft { Tytul = null }; // brak tytułu – błąd krytyczny
        var wynik = GddValidator.Waliduj(draft);
        return RepairRequestBuilder.Zbuduj(draft, wynik);
    }

    [Fact]
    public async Task NaprawAsync_PoprawnyJsonOdLLM_ZwracaSukces()
    {
        var orkiestrator = new OrkiestratorNaprawy(new MockLLM(PoprawnyGddJson));
        var wynik = await orkiestrator.NaprawAsync(BudujTestoweZadanie());

        Assert.True(wynik.Sukces);
        Assert.NotNull(wynik.NaprawioneDraft);
        Assert.Equal("Gra Testowa", wynik.NaprawioneDraft!.Tytul);
    }

    [Fact]
    public async Task NaprawAsync_NiepoprawnyJson_ZwracaBladSukcesu()
    {
        var orkiestrator = new OrkiestratorNaprawy(new MockLLM("To nie jest JSON!"));
        var wynik = await orkiestrator.NaprawAsync(BudujTestoweZadanie(), maxProb: 1);

        Assert.False(wynik.Sukces);
    }

    [Fact]
    public async Task NaprawAsync_LLMRzucaWyjatek_ZwracaBladZKomunikatem()
    {
        var orkiestrator = new OrkiestratorNaprawy(new ThrowingLLM());
        var wynik = await orkiestrator.NaprawAsync(BudujTestoweZadanie(), maxProb: 1);

        Assert.False(wynik.Sukces);
        Assert.Contains("Błąd", wynik.Komunikat, StringComparison.OrdinalIgnoreCase);
    }
}
