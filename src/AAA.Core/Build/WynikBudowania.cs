namespace AAA.Core.Build;

public class WynikBudowania
{
    public bool Sukces { get; init; }
    public string Komunikat { get; init; } = "";
    public IReadOnlyList<string> WygenerowanePliki { get; init; } = [];
    public string? KatalogWyjsciowy { get; init; }
    public int LiczbaWygenerowanchPlikow => WygenerowanePliki.Count;
}
