using System.Text.RegularExpressions;

namespace FypTracker.Services
{
    // Cuts a long text into small chunks made of whole sentences.
    public static class TextChunker
    {
        public static List<string> Split(string? text, int maxWordsPerChunk = 80, int maxChunks = 10)
        {
            var chunks = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
            {
                return chunks;
            }

            // tidy spaces and new lines
            var clean = Regex.Replace(text, @"\s+", " ").Trim();

            // 1. cut into sentences (after . ! or ?)
            var sentences = Regex.Split(clean, @"(?<=[.!?])\s+")
                                 .Where(sentence => !string.IsNullOrWhiteSpace(sentence));

            // 2. a sentence that is too long is cut by words
            var pieces = new List<string>();
            foreach (var sentence in sentences)
            {
                var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (words.Length <= maxWordsPerChunk)
                {
                    pieces.Add(sentence);
                    continue;
                }

                for (int i = 0; i < words.Length; i += maxWordsPerChunk)
                {
                    pieces.Add(string.Join(' ', words.Skip(i).Take(maxWordsPerChunk)));
                }
            }

            // 3. pack the pieces into chunks of about maxWordsPerChunk words
            var current = new List<string>();
            int currentWords = 0;

            foreach (var piece in pieces)
            {
                int pieceWords = piece.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

                if (currentWords + pieceWords > maxWordsPerChunk && current.Count > 0)
                {
                    chunks.Add(string.Join(' ', current));
                    current.Clear();
                    currentWords = 0;
                }

                current.Add(piece);
                currentWords += pieceWords;
            }

            if (current.Count > 0)
            {
                chunks.Add(string.Join(' ', current));
            }

            return chunks.Take(maxChunks).ToList();   // limit keeps the check fast
        }
    }
}