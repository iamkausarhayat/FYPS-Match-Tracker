using System.Text.RegularExpressions;

namespace FypTracker.Services
{
    // Student ke idea se 3-4 CHHOTI search queries banata hai (har query mein 1 se 3 lafz).
    // Poora idea kisi doosri website ko nahi jata, sirf ye chand lafz jate hain.
    public static class KeywordQueryBuilder
    {
        public static List<string> Build(string? title, string? description, int maxQueries)
        {
            // stem -> (asli lafz, wazan). Title ke lafz 3 guna zaroori, description ke 1 guna.
            var words = new Dictionary<string, (string Raw, int Weight)>();

            void Add(string? text, int weight)
            {
                foreach (var raw in Regex.Split((text ?? string.Empty).ToLowerInvariant(), "[^a-z0-9]+"))
                {
                    if (raw.Length < 4 || raw.All(char.IsDigit))
                    {
                        continue;
                    }

                    // TextCleaner khali list deta hai agar lafz stop-word hai ("project", "system" waghera)
                    var stem = TextCleaner.Tokenize(raw).FirstOrDefault();
                    if (stem == null || TextCleaner.Tokenize(stem).Count == 0)
                    {
                        continue;
                    }

                    words[stem] = words.TryGetValue(stem, out var old)
                        ? (old.Raw, old.Weight + weight)
                        : (raw, weight);
                }
            }

            Add(title, 3);
            Add(description, 1);

            // Sab se zaroori 6 lafz (barabar wazan par lamba lafz pehle)
            var keys = words
                .OrderByDescending(p => p.Value.Weight)
                .ThenByDescending(p => p.Value.Raw.Length)
                .ThenBy(p => p.Value.Raw)
                .Take(6)
                .Select(p => p.Value.Raw)
                .ToList();

            var queries = new List<string>();

            void AddQuery(params int[] positions)
            {
                if (positions.Any(i => i >= keys.Count))
                {
                    return;
                }

                var query = string.Join(" ", positions.Select(i => keys[i]));
                if (!queries.Contains(query))
                {
                    queries.Add(query);
                }
            }

            // Pehle 2 lafz, phir 3 lafz, phir sab se zaroori akela lafz (sab se wasee), phir doosri jodi
            AddQuery(0, 1);
            AddQuery(0, 1, 2);
            AddQuery(0);
            AddQuery(1, 2);
            AddQuery(2, 3);

            return queries.Take(Math.Max(1, maxQueries)).ToList();
        }
    }
}   