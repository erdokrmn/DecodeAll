using DecodeAI.Models;

namespace DecodeAI.Services
{
    public static class ReadabilityScorer
    {
        public static double GetHumanReadabilityScore(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0.0;

            double score = 0;
            int length = text.Length;

            // 1. Replacement karakter oranı (�)
            int replacementCharCount = text.Count(c => c == '\uFFFD');
            double replacementRatio = (double)replacementCharCount / length;
            score += (1.0 - Math.Min(1.0, replacementRatio * 5)) * 0.25; // %25 ağırlık

            // 2. Yazdırılabilir karakter oranı
            double printableRatio = text.Count(c =>
                !char.IsControl(c) &&
                !char.IsSurrogate(c)) / (double)length;
            score += Math.Min(1.0, printableRatio) * 0.25; // %25 ağırlık

            // 3. Kelime sayısı
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int minWordCount = Math.Max(3, length / 30);
            double wordScore = Math.Min(1.0, (double)words.Length / minWordCount);
            score += wordScore * 0.10; // %10 ağırlık

            // 4. Ortak kelime eşleşme
            int matchCount = LanguageReference.CommonWords.Count(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
            int requiredMatches = Math.Max(2, length / 100);
            double commonWordScore = Math.Min(1.0, (double)matchCount / requiredMatches);
            score += commonWordScore * 0.3; // %40 ağırlık

            return Math.Round(score, 3);
        }
    }
}
