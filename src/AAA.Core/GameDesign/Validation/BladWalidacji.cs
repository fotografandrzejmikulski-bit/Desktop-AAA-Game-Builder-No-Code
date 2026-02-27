namespace AAA.Core.GameDesign.Validation;

public enum Waznosc
{
    Info,
    Ostrzezenie,
    Blad
}

public class BladWalidacji
{
    public Waznosc Waznosc { get; init; }
    public string Komunikat { get; init; } = string.Empty;
    public string? Sciezka { get; init; }

    public static BladWalidacji Blad(string komunikat, string? sciezka = null) =>
        new() { Waznosc = Waznosc.Blad, Komunikat = komunikat, Sciezka = sciezka };

    public static BladWalidacji Ostrzezenie(string komunikat, string? sciezka = null) =>
        new() { Waznosc = Waznosc.Ostrzezenie, Komunikat = komunikat, Sciezka = sciezka };

    public static BladWalidacji Info(string komunikat, string? sciezka = null) =>
        new() { Waznosc = Waznosc.Info, Komunikat = komunikat, Sciezka = sciezka };
}
