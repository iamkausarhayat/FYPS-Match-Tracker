using FypTracker.Models;

namespace FypTracker.Services
{
    public class MeaningResult
    {
        public double Score { get; set; }              // 0 to 100, after scaling
        public double RawCosine { get; set; }
        public double TitleCosine { get; set; }
        public double DescriptionCosine { get; set; }
        public string BestPassage { get; set; } = string.Empty;   // the passage of B that matched best
    }

    public class MeaningScorer
    {
        private readonly IEmbedder _embedder;

        public MeaningScorer(IEmbedder embedder)
        {
            _embedder = embedder;
        }

        // Title + description -> fingerprint (this uses the AI model)
        public async Task<TextFingerprint> CreateFingerprintAsync(string title, string description)
        {
            var chunkTexts = TextChunker.Split(description);

            if (chunkTexts.Count == 0)
            {
                chunkTexts.Add(title);   // never leave a fingerprint without chunks
            }

            return new TextFingerprint
            {
                TitleVector = await _embedder.EmbedAsync(title),
                ChunkTexts = chunkTexts,
                ChunkVectors = await _embedder.EmbedManyAsync(chunkTexts)
            };
        }

        // Two fingerprints -> meaning score. Pure math, needs no AI model, so it is fast.
        public static MeaningResult Compare(TextFingerprint a, TextFingerprint b)
        {
            if (a.ChunkVectors.Count == 0 || b.ChunkVectors.Count == 0)
            {
                return new MeaningResult();
            }

            double titleCosine = VectorMath.Cosine(a.TitleVector, b.TitleVector);

            // For every chunk of A find its best partner in B. Also remember the best pair of all.
            double bestPair = double.MinValue;
            int bestPartnerInB = 0;
            double sumAToB = 0;

            foreach (var chunkA in a.ChunkVectors)
            {
                double best = double.MinValue;

                for (int j = 0; j < b.ChunkVectors.Count; j++)
                {
                    double cosine = VectorMath.Cosine(chunkA, b.ChunkVectors[j]);

                    if (cosine > best) best = cosine;

                    if (cosine > bestPair)
                    {
                        bestPair = cosine;
                        bestPartnerInB = j;
                    }
                }

                sumAToB += best;
            }

            // The same thing the other way round, so long and short texts are treated fairly.
            double sumBToA = 0;

            foreach (var chunkB in b.ChunkVectors)
            {
                double best = double.MinValue;

                foreach (var chunkA in a.ChunkVectors)
                {
                    best = Math.Max(best, VectorMath.Cosine(chunkB, chunkA));
                }

                sumBToA += best;
            }

            double descriptionCosine =
                (sumAToB / a.ChunkVectors.Count + sumBToA / b.ChunkVectors.Count) / 2;

            double rawCosine =
                SimilarityRules.TitleWeight * titleCosine +
                SimilarityRules.DescriptionWeight * descriptionCosine;

            return new MeaningResult
            {
                Score = SimilarityRules.ScaleCosine(rawCosine),
                RawCosine = rawCosine,
                TitleCosine = titleCosine,
                DescriptionCosine = descriptionCosine,
                BestPassage = b.ChunkTexts[bestPartnerInB]
            };
        }
    }
}