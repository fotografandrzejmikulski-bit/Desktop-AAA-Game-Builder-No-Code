using AAA.Core.GameDesign.AutoNaprawa;
using AAA.Core.GameDesign.Models;
using AAA.Core.GameDesign.Validation;
using AAA.Core.Storage;

namespace AAA.Core.Conversation;

public enum StatusKomendy
{
    Sukces,
    WymagaNaprawy,
    Blad
}

public class WynikKomendy
{
    public StatusKomendy Status { get; init; }
    public string Komunikat { get; init; } = string.Empty;
    public RepairRequest? ZadanieNaprawy { get; init; }
    public GddDraft? WygenerowaneDraft { get; init; }
}

/// <summary>Obsługa komendy "Wygeneruj grę" – walidacja i przygotowanie do zatwierdzenia lub naprawy</summary>
public static class WygenerujGreCommand
{
    /// <summary>
    /// Przetwarza wygenerowany roboczy GDD: waliduje go i zwraca wynik.
    /// Jeśli są błędy krytyczne – zwraca RepairRequest zamiast proponować zatwierdzenie.
    /// </summary>
    public static WynikKomendy Wykonaj(ProjectState stan, GddDraft wygenerowaneDraft)
    {
        stan.Gdd.Roboczy = wygenerowaneDraft;
        stan.DataModyfikacji = DateTime.UtcNow;

        var wynikWalidacji = GddValidator.Waliduj(wygenerowaneDraft);

        if (wynikWalidacji.MaBledyKrytyczne)
        {
            var repairRequest = RepairRequestBuilder.Zbuduj(
                wygenerowaneDraft,
                wynikWalidacji,
                stan.Gdd.Zatwierdzony);

            return new WynikKomendy
            {
                Status = StatusKomendy.WymagaNaprawy,
                Komunikat = $"Wygenerowany GDD zawiera {wynikWalidacji.Bledy.Count(b => b.Waznosc == Waznosc.Blad)} błąd(y) krytyczne. Przygotowano żądanie naprawy.",
                ZadanieNaprawy = repairRequest,
                WygenerowaneDraft = wygenerowaneDraft
            };
        }

        return new WynikKomendy
        {
            Status = StatusKomendy.Sukces,
            Komunikat = "GDD zostało wygenerowane pomyślnie. Możesz je przejrzeć i zatwierdzić.",
            WygenerowaneDraft = wygenerowaneDraft
        };
    }

    /// <summary>Zatwierdza roboczy GDD jako aktywny (używany przez podgląd i build)</summary>
    public static WynikKomendy Zatwierdz(ProjectState stan)
    {
        if (stan.Gdd.Roboczy is null)
            return new WynikKomendy { Status = StatusKomendy.Blad, Komunikat = "Brak roboczego GDD do zatwierdzenia." };

        var wynikWalidacji = GddValidator.Waliduj(stan.Gdd.Roboczy);
        if (wynikWalidacji.MaBledyKrytyczne)
            return new WynikKomendy
            {
                Status = StatusKomendy.WymagaNaprawy,
                Komunikat = "Nie można zatwierdzić GDD zawierającego błędy krytyczne.",
                ZadanieNaprawy = RepairRequestBuilder.Zbuduj(stan.Gdd.Roboczy, wynikWalidacji, stan.Gdd.Zatwierdzony)
            };

        stan.Gdd.Zatwierdzony = stan.Gdd.Roboczy;
        stan.DataModyfikacji = DateTime.UtcNow;

        return new WynikKomendy { Status = StatusKomendy.Sukces, Komunikat = "GDD zostało zatwierdzone." };
    }
}
