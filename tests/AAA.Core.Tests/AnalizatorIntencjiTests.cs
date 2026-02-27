using AAA.Core.Conversation;
using Xunit;

namespace AAA.Core.Tests;

public class AnalizatorIntencjiTests
{
    [Fact]
    public void Analizuj_PustyTekst_ZwracaNieznana()
    {
        var wynik = AnalizatorIntencji.Analizuj("   ");
        Assert.Equal(IntencjaRozmowy.Nieznana, wynik);
    }

    [Fact]
    public void Analizuj_TekstZaczynajacySieOdKlamry_ZwracaGenerujGre()
    {
        var wynik = AnalizatorIntencji.Analizuj("{ \"Tytul\": \"Test\" }");
        Assert.Equal(IntencjaRozmowy.GenerujGre, wynik);
    }

    [Fact]
    public void Analizuj_SlowozatwierdzdGdd_ZwracaZatwierdzdGdd()
    {
        var wynik = AnalizatorIntencji.Analizuj("zatwierdzam ten projekt");
        Assert.Equal(IntencjaRozmowy.ZatwierdźGdd, wynik);
    }

    [Fact]
    public void Analizuj_SlowoEdytuj_ZwracaEdytujSekcje()
    {
        var wynik = AnalizatorIntencji.Analizuj("edytuj sekcję postaci");
        Assert.Equal(IntencjaRozmowy.EdytujSekcje, wynik);
    }

    [Fact]
    public void Analizuj_SlowoPokazGdd_ZwracaPokazPodglad()
    {
        var wynik = AnalizatorIntencji.Analizuj("pokaż gdd");
        Assert.Equal(IntencjaRozmowy.PokazPodglad, wynik);
    }

    [Fact]
    public void Analizuj_NowaSesja_ZwracaNowaSesja()
    {
        var wynik = AnalizatorIntencji.Analizuj("nowa sesja");
        Assert.Equal(IntencjaRozmowy.NowaSesja, wynik);
    }

    [Fact]
    public void Analizuj_PokazBledy_ZwracaPokazBledy()
    {
        var wynik = AnalizatorIntencji.Analizuj("pokaż błędy walidacji");
        Assert.Equal(IntencjaRozmowy.PokazBledy, wynik);
    }

    [Fact]
    public void Analizuj_ZwyklyOpisGry_ZwracaGenerujGre()
    {
        var wynik = AnalizatorIntencji.Analizuj("Zrób mi grę RPG fantasy z elfami");
        Assert.Equal(IntencjaRozmowy.GenerujGre, wynik);
    }
}
