namespace AAA.Core.Conversation;

public enum RolaWiadomosci { Uzytkownik, Bot }

public record WiadomoscHistorii(
    string Tresc,
    RolaWiadomosci Rola,
    DateTime Czas,
    IntencjaRozmowy? Intencja = null);

public class HistoriaRozmowy
{
    private readonly List<WiadomoscHistorii> _wiadomosci = [];
    public IReadOnlyList<WiadomoscHistorii> Wiadomosci => _wiadomosci.AsReadOnly();
    public int Liczba => _wiadomosci.Count;

    public void DodajWiadomoscUzytkownika(string tresc, IntencjaRozmowy? intencja = null)
        => _wiadomosci.Add(new(tresc, RolaWiadomosci.Uzytkownik, DateTime.UtcNow, intencja));

    public void DodajWiadomoscBota(string tresc)
        => _wiadomosci.Add(new(tresc, RolaWiadomosci.Bot, DateTime.UtcNow));

    public void Wyczysc() => _wiadomosci.Clear();

    public string BudujKontekstDlaLLM(int ostatnichN = 10)
    {
        var ostatnie = _wiadomosci.TakeLast(ostatnichN);
        return string.Join("\n", ostatnie.Select(w =>
            $"{(w.Rola == RolaWiadomosci.Uzytkownik ? "Użytkownik" : "Bot")}: {w.Tresc}"));
    }
}
