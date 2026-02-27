using AAA.Core.Build;
using AAA.Core.GameDesign.Models;
using Xunit;

namespace AAA.Core.Tests;

public class GddRendererTests
{
    [Fact]
    public void RenderujMarkdown_NullDraft_ZawieraBrakGdd()
    {
        var wynik = GddRenderer.RenderujMarkdown(null);
        Assert.Contains("Brak", wynik, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderujMarkdown_DraftZTytulem_ZawieraTytul()
    {
        var draft = new GddDraft { Tytul = "Moja Epicka Gra" };
        var wynik = GddRenderer.RenderujMarkdown(draft);
        Assert.Contains("Moja Epicka Gra", wynik);
    }

    [Fact]
    public void RenderujMarkdown_DraftZRegionami_ZawieraNazwyRegionow()
    {
        var draft = new GddDraft
        {
            Tytul = "Test",
            Swiat = new Swiat
            {
                Regiony =
                [
                    new Region { Id = "r1", Nazwa = "Las Elfów" },
                    new Region { Id = "r2", Nazwa = "Miasto Ludzi" }
                ]
            }
        };
        var wynik = GddRenderer.RenderujMarkdown(draft);
        Assert.Contains("Las Elfów", wynik);
        Assert.Contains("Miasto Ludzi", wynik);
    }

    [Fact]
    public void RenderujMarkdown_DraftZPostaciami_ZawieraNazwePostaci()
    {
        var draft = new GddDraft
        {
            Tytul = "Test",
            Postacie = [new Postac { Id = "p1", Nazwa = "Elara Srebrna", Rola = "Bohaterka" }]
        };
        var wynik = GddRenderer.RenderujMarkdown(draft);
        Assert.Contains("Elara Srebrna", wynik);
    }

    [Fact]
    public void RenderujMarkdown_DraftZQuestami_ZawieraTytulQuestu()
    {
        var draft = new GddDraft
        {
            Tytul = "Test",
            Questy =
            [
                new Quest
                {
                    Id = "q1",
                    Tytul = "Odnalezienie Świętego Miecza",
                    Kroki = [new KrokQuestu { Id = "k1", Opis = "Idź do świątyni" }]
                }
            ]
        };
        var wynik = GddRenderer.RenderujMarkdown(draft);
        Assert.Contains("Odnalezienie Świętego Miecza", wynik);
        Assert.Contains("Idź do świątyni", wynik);
    }
}
