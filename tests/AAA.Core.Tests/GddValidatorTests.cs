using AAA.Core.GameDesign.Models;
using AAA.Core.GameDesign.Validation;
using Xunit;

namespace AAA.Core.Tests;

public class GddValidatorTests
{
    // Przypadek 1: Puste GDD (null)
    [Fact]
    public void Waliduj_NullDraft_ZwracaBladKrytyczny()
    {
        var wynik = GddValidator.Waliduj(null);

        Assert.True(wynik.MaBledyKrytyczne);
        Assert.Single(wynik.Bledy);
        Assert.Equal(Waznosc.Blad, wynik.Bledy[0].Waznosc);
    }

    // Przypadek 2: OpenWorld bez regionów
    [Fact]
    public void Waliduj_OpenWorldBezRegionow_ZwracaBladKrytyczny()
    {
        var draft = new GddDraft
        {
            Tytul = "Test",
            Gatunek = GatunekGry.OpenWorld,
            Swiat = new Swiat { Regiony = [] }
        };

        var wynik = GddValidator.Waliduj(draft);

        Assert.True(wynik.MaBledyKrytyczne);
        Assert.Contains(wynik.Bledy, b => b.Waznosc == Waznosc.Blad && b.Sciezka == "Swiat.Regiony");
    }

    // Przypadek 3: Duplikaty ID regionów
    [Fact]
    public void Waliduj_DuplikatIdRegionu_ZwracaBladKrytyczny()
    {
        var draft = new GddDraft
        {
            Tytul = "Test",
            Gatunek = GatunekGry.OpenWorld,
            Swiat = new Swiat
            {
                Regiony =
                [
                    new Region { Id = "r1", Nazwa = "Region 1" },
                    new Region { Id = "r1", Nazwa = "Region 1 kopia" }
                ]
            }
        };

        var wynik = GddValidator.Waliduj(draft);

        Assert.True(wynik.MaBledyKrytyczne);
        Assert.Contains(wynik.Bledy, b => b.Waznosc == Waznosc.Blad && b.Komunikat.Contains("r1"));
    }

    // Przypadek 4: Puste nazwy regionów
    [Fact]
    public void Waliduj_PustaNazwaRegionu_ZwracaBladKrytyczny()
    {
        var draft = new GddDraft
        {
            Tytul = "Test",
            Gatunek = GatunekGry.OpenWorld,
            Swiat = new Swiat
            {
                Regiony = [new Region { Id = "r1", Nazwa = "" }]
            }
        };

        var wynik = GddValidator.Waliduj(draft);

        Assert.True(wynik.MaBledyKrytyczne);
        Assert.Contains(wynik.Bledy, b => b.Waznosc == Waznosc.Blad && b.Komunikat.Contains("pustą nazwę"));
    }

    // Przypadek 5: Duplikaty postaci
    [Fact]
    public void Waliduj_DuplikatIdPostaci_ZwracaBladKrytyczny()
    {
        var draft = new GddDraft
        {
            Tytul = "Test",
            Postacie =
            [
                new Postac { Id = "p1", Nazwa = "Bohater" },
                new Postac { Id = "p1", Nazwa = "Bohater kopia" }
            ]
        };

        var wynik = GddValidator.Waliduj(draft);

        Assert.True(wynik.MaBledyKrytyczne);
        Assert.Contains(wynik.Bledy, b => b.Waznosc == Waznosc.Blad && b.Komunikat.Contains("p1"));
    }

    // Przypadek 6: Quest bez tytułu i bez kroków
    [Fact]
    public void Waliduj_QuestBezTytuluIKrokow_ZwracaBledyKrytyczne()
    {
        var draft = new GddDraft
        {
            Tytul = "Test",
            Questy =
            [
                new Quest { Id = "q1", Tytul = null, Kroki = [] }
            ]
        };

        var wynik = GddValidator.Waliduj(draft);

        Assert.True(wynik.MaBledyKrytyczne);
        Assert.Contains(wynik.Bledy, b => b.Sciezka == "Questy[q1].Tytul");
        Assert.Contains(wynik.Bledy, b => b.Sciezka == "Questy[q1].Kroki");
    }

    // Bonus: Poprawne GDD bez błędów
    [Fact]
    public void Waliduj_PoprawneGdd_BrakBledow()
    {
        var draft = new GddDraft
        {
            Tytul = "Moja Gra",
            Gatunek = GatunekGry.OpenWorld,
            Swiat = new Swiat
            {
                Regiony = [new Region { Id = "r1", Nazwa = "Kraina Mgły" }],
                PunktyZainteresowania =
                [
                    new PunktZainteresowania { Id = "poi1", Nazwa = "Zamek", RegionId = "r1" }
                ]
            },
            Postacie = [new Postac { Id = "p1", Nazwa = "Elara" }],
            Questy =
            [
                new Quest
                {
                    Id = "q1",
                    Tytul = "Odnalezienie miecza",
                    Kroki = [new KrokQuestu { Id = "k1", Opis = "Idź do zamku" }]
                }
            ]
        };

        var wynik = GddValidator.Waliduj(draft);

        Assert.False(wynik.MaBledyKrytyczne);
        Assert.DoesNotContain(wynik.Bledy, b => b.Waznosc == Waznosc.Blad);
    }

    // Bonus: POI z nieistniejącym RegionId
    [Fact]
    public void Waliduj_PoiZNieistniejacymRegionId_ZwracaBladKrytyczny()
    {
        var draft = new GddDraft
        {
            Tytul = "Test",
            Gatunek = GatunekGry.OpenWorld,
            Swiat = new Swiat
            {
                Regiony = [new Region { Id = "r1", Nazwa = "Region 1" }],
                PunktyZainteresowania =
                [
                    new PunktZainteresowania { Id = "poi1", Nazwa = "Zamek", RegionId = "r-nieistniejacy" }
                ]
            }
        };

        var wynik = GddValidator.Waliduj(draft);

        Assert.True(wynik.MaBledyKrytyczne);
        Assert.Contains(wynik.Bledy, b => b.Komunikat.Contains("r-nieistniejacy"));
    }
}
