using FypTracker.Models;

namespace FypTracker.Services
{
    public class MeaningResult
    {
        public double Score { get; set; }              // 0 to 100, after scaling
        public double RawCosine { get; set; }
        public double TitleCosine { get; set; }
        public double DescriptionCosine { get; set; }
        public double CoverageCosine { get; set; }
        public double BestPairCosine { get; set; }
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

        // A = the student's idea, B = one project from the database.
        // Pure math, needs no AI model, so it is fast.
        //
        // Important: the question is "how much of the STUDENT's idea already exists in B?"
        // We do NOT average B's chunks against A. A long project has many chunks that are about
        // other things, and averaging them used to pull even a copied paragraph down to about 30%.
        public static MeaningResult Compare(TextFingerprint a, TextFingerprint b)
        {
            if (a.ChunkVectors.Count == 0 || b.ChunkVectors.Count == 0)
            {
                return new MeaningResult();
            }

            double titleCosine = Math.Max(0, VectorMath.Cosine(a.TitleVector, b.TitleVector));

            double bestPair = double.MinValue;
            int bestPartnerInB = 0;
            double sumBest = 0;

            // For every chunk of A find its best partner in B.
            for (int i = 0; i < a.ChunkVectors.Count; i++)
            {
                double best = double.MinValue;

                for (int j = 0; j < b.ChunkVectors.Count; j++)
                {
                    double cosine = VectorMath.Cosine(a.ChunkVectors[i], b.ChunkVectors[j]);

                    if (cosine > best)
                    {
                        best = cosine;
                    }

                    if (cosine > bestPair)
                    {
                        bestPair = cosine;
                        bestPartnerInB = j;
                    }
                }

                sumBest += best;
            }

            double coverage = sumBest / a.ChunkVectors.Count;

            double descriptionCosine =
                SimilarityRules.CoverageWeight * coverage +
                SimilarityRules.BestPairWeight * bestPair;

            double rawCosine =
                SimilarityRules.TitleWeight * titleCosine +
                SimilarityRules.DescriptionWeight * descriptionCosine;

            return new MeaningResult
            {
                Score = SimilarityRules.ScaleCosine(rawCosine),
                RawCosine = rawCosine,
                TitleCosine = titleCosine,
                DescriptionCosine = descriptionCosine,
                CoverageCosine = coverage,
                BestPairCosine = bestPair,
                BestPassage = b.ChunkTexts[bestPartnerInB]
            };
        }
    }
}