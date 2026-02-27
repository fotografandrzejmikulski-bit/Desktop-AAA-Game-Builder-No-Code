namespace AAA.Core.Conversation;

/// <summary>Wykrywacz intencji oparty na słowach kluczowych języka polskiego.</summary>
public static class AnalizatorIntencji
{
    public static IntencjaRozmowy Analizuj(string tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
            return IntencjaRozmowy.Nieznana;

        var przyciete = tekst.TrimStart();

        // Reguła 1: JSON
        if (przyciete.StartsWith('{'))
            return IntencjaRozmowy.GenerujGre;

        var lower = tekst.ToLowerInvariant();

        // Reguła 2: zatwierdzenie
        if (Zawiera(lower, "zatwierdzam", "zatwierdź", "potwierdzam", "akceptuję",
                    "zgadzam się", "ok zatwierdź", "tak zatwierdź"))
            return IntencjaRozmowy.ZatwierdźGdd;

        // Reguła 3: edycja
        if (Zawiera(lower, "edytuj", "zmień", "popraw", "dodaj region", "dodaj postać",
                    "dodaj quest", "usuń", "modyfikuj", "zaktualizuj"))
            return IntencjaRozmowy.EdytujSekcje;

        // Reguła 4: podgląd
        if (Zawiera(lower, "pokaż gdd", "podgląd", "jak wygląda", "pokaż podgląd",
                    "przedstaw gdd", "wyświetl gdd", "pokaż projekt"))
            return IntencjaRozmowy.PokazPodglad;

        // Reguła 5: nowa sesja
        if (Zawiera(lower, "nowa sesja", "zacznij od nowa", "reset",
                    "zacznij jeszcze raz", "nowy projekt"))
            return IntencjaRozmowy.NowaSesja;

        // Reguła 6: pokaż błędy
        if (Zawiera(lower, "pokaż błędy", "jakie błędy", "lista błędów",
                    "walidacja", "co jest nie tak"))
            return IntencjaRozmowy.PokazBledy;

        // Reguła 7: puste/białe znaki już obsłużone powyżej
        // Reguła 8 (domyślna): każdy inny tekst to próba wygenerowania gry
        return IntencjaRozmowy.GenerujGre;
    }

    private static bool Zawiera(string lower, params string[] slowa)
        => slowa.Any(s => lower.Contains(s, StringComparison.Ordinal));
}
