using DocAssistant.Api;

namespace DocAssistant.Tests
{
    public class ChunkerTests
    {
        private static string Ornek(int uzunluk) =>
            string.Concat(Enumerable.Range(0, uzunluk).Select(i => (char)('a' + i % 26)));

        [Fact]
        public void KisaMetin_TekParcaDoner()
        {
            var result = Chunker.Chunk("merhaba", 800, 100).ToList();

            Assert.Single(result);
            Assert.Equal("merhaba", result[0]);
        }

        [Fact]
        public void BosMetin_ParcaUretme()
        {
            Assert.Empty(Chunker.Chunk(""));
        }

        [Fact]
        public void UzunMetin_BirdenFazlaParcayaBolunur()
        {
            var result = Chunker.Chunk(Ornek(2000)).ToList();
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void ArdisikParcalar_OverlapKadarOrtakIcerir()
        {
            var result = Chunker.Chunk(Ornek(2000)).ToList();
            Assert.Equal(result[0][^100..],result[1][..100]);

        }

        [Fact]
        public void ParcalarTumMetniKapsar()
        {
            var text = Ornek(2000);
            var result = Chunker.Chunk(text, 800, 100).ToList();

            Assert.StartsWith(result[0], text);
            Assert.EndsWith(result[^1], text);
        }

        [Fact]
        public void OverlapBoyuttanBuyukse_HataFirlatir()
        {
            // Iterator metotlar tembel çalışır, hata ancak numaralandırınca fırlar: ToList() şart
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Chunker.Chunk("abc", 10, 10).ToList());
        }
    }
}
