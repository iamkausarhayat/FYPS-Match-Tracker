namespace FypTracker.Services
{
    public class WordSimilarityResult
    {
        public double Score { get; set; }                         // 0 to 100
        public List<string> CommonKeywords { get; set; } = new();
    }

    // Compares two texts by the exact words they share (the 20% part of the final score).
    public static class WordSimilarity
    {
        public static WordSimilarityResult Compare(string? textA, string? textB)
        {
            var wordsA = TextCleaner.ToWordSet(textA);
            var wordsB = TextCleaner.ToWordSet(textB);

            if (wordsA.Count == 0 || wordsB.Count == 0)
            {
                return new WordSimilarityResult();
            }

            // words that appear in both texts
            var common = new HashSet<string>(wordsA);
            common.IntersectWith(wordsB);

            // all different words from both texts
            var all = new HashSet<string>(wordsA);
            all.UnionWith(wordsB);

            // Jaccard score = common / all
            double jaccard = (double)common.Count / all.Count;

            // show the most frequent common words first
            var countsA = TextCleaner.CountWords(textA);
            var keywords = common
                .OrderByDescending(word => countsA[word])
                .ThenBy(word => word)
                .Take(8)
                .ToList();

            return new WordSimilarityResult
            {
                Score = Math.Round(jaccard * 100, 1),
                CommonKeywords = keywords
            };
        }
    }
}