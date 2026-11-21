namespace FypTracker.Models
{
    // One place for every number in our similarity rule, so tuning later is easy.
    public static class SimilarityRules
    {
        // Final score = 80% meaning + 20% words
        public const double MeaningWeight = 0.8;
        public const double WordWeight = 0.2;

        // Verdict limits (tuned with real tests in B10)
        public const double SimilarFrom = 40;
        public const double AlreadyDoneFrom = 70;

        // Inside the meaning score: how much the title and the description count
        public const double TitleWeight = 0.25;
        public const double DescriptionWeight = 0.75;

        // Raw cosine of our model: unrelated texts score about 0.2, near-copies about 0.85.
        // We stretch that range to 0..100. Tuned with real tests in B10.
        public const double CosineFloor = 0.20;
        public const double CosineCeiling = 0.85;
        // The AI model that creates the meaning fingerprints
        public const string EmbeddingModelName = "all-mpnet-base-v2";

        public static double ScaleCosine(double rawCosine)
        {
            double scaled = (rawCosine - CosineFloor) / (CosineCeiling - CosineFloor);
            return Math.Round(Math.Clamp(scaled, 0, 1) * 100, 1);
        }

        public static double Combine(double meaningScore, double wordScore)
        {
            return Math.Round(MeaningWeight * meaningScore + WordWeight * wordScore, 1);
        }

        public static string VerdictFor(double finalScore)
        {
            if (finalScore >= AlreadyDoneFrom) return "Already Done";
            if (finalScore >= SimilarFrom) return "Similar";
            return "Unique";
        }
    }
}