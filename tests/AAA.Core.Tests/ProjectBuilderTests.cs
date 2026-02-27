using System.Text.Json;
using AAA.Core.Build;
using AAA.Core.GameDesign.Models;
using Xunit;

namespace AAA.Core.Tests;

public class ProjectBuilderTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectBuilderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"AAABuilderTest_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static GddDraft BudujPoprawnyDraft() => new()
    {
        Tytul = "Legenda Północy",
        Gatunek = GatunekGry.RPG,
        Swiat = new Swiat
        {
            Nazwa = "Kraina Lodu",
            Regiony = [new Region { Id = "r1", Nazwa = "Tundrowa Równina" }]
        },
        Postacie = [new Postac { Id = "p1", Nazwa = "Snorri", Rola = "Bohater" }],
        Questy =
        [
            new Quest
            {
                Id = "q1",
                Tytul = "Poszukiwanie Runów",
                Kroki = [new KrokQuestu { Id = "k1", Opis = "Znajdź runy" }]
            }
        ]
    };

    [Fact]
    public void Zbuduj_ZwracaSukces_IGenerujePliki()
    {
        var draft = BudujPoprawnyDraft();
        var wynik = ProjectBuilder.Zbuduj(draft, _tempDir);

        Assert.True(wynik.Sukces);
        Assert.True(wynik.LiczbaWygenerowanchPlikow > 0);
        Assert.All(wynik.WygenerowanePliki, p => Assert.True(File.Exists(p)));
    }

    [Fact]
    public void Zbuduj_GameConfigJson_ZawieraTytulGry()
    {
        var draft = BudujPoprawnyDraft();
        ProjectBuilder.Zbuduj(draft, _tempDir);

        var configSciezka = Path.Combine(_tempDir, "game_config.json");
        Assert.True(File.Exists(configSciezka));

        var json = File.ReadAllText(configSciezka);
        Assert.Contains("Legenda Północy", json);
    }

    [Fact]
    public void Zbuduj_GddMd_IstnijeINieJestPusty()
    {
        var draft = BudujPoprawnyDraft();
        ProjectBuilder.Zbuduj(draft, _tempDir);

        var gddSciezka = Path.Combine(_tempDir, "gdd.md");
        Assert.True(File.Exists(gddSciezka));
        Assert.True(new FileInfo(gddSciezka).Length > 0);
    }
}
