namespace DocAssistant.Api
{
    public static class Chunker
    {
        public static IEnumerable<string> Chunk(string text, int size = 800, int overlap = 100)
        {
            if (size <= 0) throw new ArgumentNullException(nameof(size));
            if (overlap < 0 || overlap >= size) throw new ArgumentOutOfRangeException(nameof(overlap));

            for (int i = 0; i < text.Length; i += size - overlap)
            {
                var len = Math.Min(size, text.Length - i);
                yield return text.Substring(i, len);
                if (i + len >= text.Length) break;
            }
        }
    }
}
