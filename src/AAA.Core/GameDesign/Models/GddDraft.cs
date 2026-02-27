using System.Text.Json.Serialization;

namespace AAA.Core.GameDesign.Models;

/// <summary>Gatunek gry</summary>
public enum GatunekGry
{
    Nieznany,
    OpenWorld,
    RPG,
    FPS,
    RTS,
    Platformowka
}

public class GddDraft
{
    public string? Tytul { get; set; }
    public GatunekGry Gatunek { get; set; } = GatunekGry.Nieznany;
    public Swiat? Swiat { get; set; }
    public List<Postac> Postacie { get; set; } = [];
    public List<Quest> Questy { get; set; } = [];
}

public class Swiat
{
    public string? Nazwa { get; set; }
    public string? Opis { get; set; }
    public List<Region> Regiony { get; set; } = [];
    public List<PunktZainteresowania> PunktyZainteresowania { get; set; } = [];
}

public class Region
{
    public string? Id { get; set; }
    public string? Nazwa { get; set; }
    public string? Opis { get; set; }
}

public class PunktZainteresowania
{
    public string? Id { get; set; }
    public string? Nazwa { get; set; }
    /// <summary>Opcjonalne ID regionu, do którego należy POI</summary>
    public string? RegionId { get; set; }
}

public class Postac
{
    public string? Id { get; set; }
    public string? Nazwa { get; set; }
    public string? Rola { get; set; }
}

public class Quest
{
    public string? Id { get; set; }
    public string? Tytul { get; set; }
    public string? Opis { get; set; }
    public List<KrokQuestu> Kroki { get; set; } = [];
}

public class KrokQuestu
{
    public string? Id { get; set; }
    public string? Opis { get; set; }
}
