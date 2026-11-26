using System.Text.RegularExpressions;

namespace FypTracker.Services
{
    // Turns raw text into clean, comparable keywords.
    public static class TextCleaner
    {
        // Words that carry no real meaning, plus words found in almost every FYP description.
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            // common English words
            "the", "and", "for", "are", "was", "were", "been", "being", "its", "this", "that", "these", "those",
            "with", "from", "into", "onto", "over", "under", "about", "after", "before", "between", "through",
            "during", "will", "would", "can", "could", "should", "may", "might", "must", "shall", "does", "did",
            "has", "have", "had", "not", "but", "than", "then", "there", "their", "they", "them", "our", "you",
            "your", "his", "her", "who", "whom", "which", "what", "when", "where", "why", "how", "all", "any",
            "each", "every", "both", "few", "more", "most", "other", "some", "such", "only", "own", "same",
            "too", "very", "just", "also", "again", "further", "once", "here", "while", "per", "via", "etc",
            // words found in almost every project description
            "project", "projects", "system", "systems", "application", "applications", "app", "apps",
            "software", "using", "use", "used", "based", "develop", "developed", "development", "proposed",
            "propose", "aim", "aims", "provide", "provides", "allow", "allows", "user", "users", "platform",
            "solution", "tool", "tools", "help", "helps", "new", "make", "makes", "enable", "enables", "easy"
        };

        // Text -> list of clean keywords
        public static List<string> Tokenize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            return Regex.Split(text.ToLowerInvariant(), "[^a-z0-9]+")
                        .Where(word => word.Length >= 3)
                        .Where(word => !word.All(char.IsDigit))      // drop pure numbers like 2023
                        .Where(word => !StopWords.Contains(word))
                        .Select(Stem)
                        .Where(word => word.Length >= 3)
                        .ToList();
        }

        // Text -> set of unique keywords
        public static HashSet<string> ToWordSet(string? text)
        {
            return new HashSet<string>(Tokenize(text));
        }

        // Text -> how many times each keyword appears
        public static Dictionary<string, int> CountWords(string? text)
        {
            var counts = new Dictionary<string, int>();

            foreach (var word in Tokenize(text))
            {
                counts[word] = counts.GetValueOrDefault(word) + 1;
            }

            return counts;
        }

        // The most frequent keywords (used later to search research papers)
        public static List<string> TopKeywords(string? text, int count)
        {
            return CountWords(text)
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Take(count)
                .Select(pair => pair.Key)
                .ToList();
        }

        // A very light word trimmer: students -> student, booking -> book, studies -> study
        private static string Stem(string word)
        {
            if (word.Length > 5 && word.EndsWith("ing")) return word[..^3];
            if (word.Length > 4 && word.EndsWith("ies")) return word[..^3] + "y";
            if (word.Length > 4 && word.EndsWith("ed")) return word[..^2];

            if (word.Length > 3 && word.EndsWith("s")
                && !word.EndsWith("ss") && !word.EndsWith("us") && !word.EndsWith("is"))
            {
                return word[..^1];
            }

            return word;
        }
    }
}