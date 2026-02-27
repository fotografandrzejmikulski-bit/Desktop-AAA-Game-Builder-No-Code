using AAA.Core.GameDesign.Models;

namespace AAA.Core.Storage;

public class GddDocument
{
    /// <summary>Roboczy szkic GDD (może zawierać błędy)</summary>
    public GddDraft? Roboczy { get; set; }

    /// <summary>Zatwierdzony GDD – używany przez podgląd i build</summary>
    public GddDraft? Zatwierdzony { get; set; }
}

public class ProjectState
{
    public string NazwaProjektu { get; set; } = "Nowy Projekt";
    public GddDocument Gdd { get; set; } = new();
    public DateTime DataUtworzenia { get; set; } = DateTime.UtcNow;
    public DateTime DataModyfikacji { get; set; } = DateTime.UtcNow;
}
