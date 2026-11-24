using FypTracker.Models;

namespace FypTracker.Services
{
    public class WordSimilarityResult
    {
        public double Score { get; set; }                         // 0 to 100
        public List<string> CommonKeywords { get; set; } = new();
    }

    // Compares two texts by the exact words they share (the 20% part of the final score).
    // textA = the student's idea, textB = one project.
    // Question asked: "how many of the STUDENT's words and word-pairs are found in the project?"
    // (The old Jaccard score divided by ALL words of both texts, so a short idea against a long
    //  project always looked tiny, even when it was copied.)
    public static class WordSimilarity
    {
        public static WordSimilarityResult Compare(string? textA, string? textB)
        {
            var tokensA = TextCleaner.Tokenize(textA);
            var tokensB = TextCleaner.Tokenize(textB);

            if (tokensA.Count == 0 || tokensB.Count == 0)
            {
                return new WordSimilarityResult();
            }

            var wordsA = new HashSet<string>(tokensA);
            var wordsB = new HashSet<string>(tokensB);

            // single words of A that also appear in B
            var common = new HashSet<string>(wordsA);
            common.IntersectWith(wordsB);

            double wordCoverage = (double)common.Count / wordsA.Count;

            // two-word phrases of A that also appear in B (like "accident location", "crawler classifier")
            var pairsA = MakePairs(tokensA);
            var pairsB = MakePairs(tokensB);

            double pairCoverage = 0;
            if (pairsA.Count > 0)
            {
                int pairsFound = 0;
                foreach (var pair in pairsA)
                {
                    if (pairsB.Contains(pair))
                    {
                        pairsFound++;
                    }
                }

                pairCoverage = (double)pairsFound / pairsA.Count;
            }

            double score = SimilarityRules.SingleWordWeight * wordCoverage
                         + SimilarityRules.PhraseWeight * pairCoverage;

            // show the most frequent common words first
            var countsA = TextCleaner.CountWords(textA);
            var keywords = common
                .OrderByDescending(word => countsA[word])
                .ThenBy(word => word)
                .Take(8)
                .ToList();

            return new WordSimilarityResult
            {
                Score = Math.Round(score * 100, 1),
                CommonKeywords = keywords
            };
        }

        private static HashSet<string> MakePairs(List<string> tokens)
        {
            var pairs = new HashSet<string>();

            for (int i = 0; i + 1 < tokens.Count; i++)
            {
                pairs.Add(tokens[i] + " " + tokens[i + 1]);
            }

            return pairs;
        }
    }
}