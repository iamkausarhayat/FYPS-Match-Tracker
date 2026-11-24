namespace FypTracker.Models
{
    // One place for every number in our similarity rule, so tuning later is easy.
    public static class SimilarityRules
    {
        // Final score = 80% meaning + 20% words
        public const double MeaningWeight = 0.8;
        public const double WordWeight = 0.2;

        // Verdict limits:  0 to 20 = Unique,  20 to 50 = Similar,  50 to 100 = Already Done
        public const double SimilarFrom = 20;
        public const double AlreadyDoneFrom = 50;

        // How many matches we keep and show for one check (the top one + the rest)
        public const int MaxStoredMatches = 10;

        // Inside the raw meaning score: title counts less, because a student's title is often
        // worded differently even when the idea is the same.
        public const double TitleWeight = 0.15;
        public const double DescriptionWeight = 0.85;

        // Inside the description part:
        //   Coverage  = on average, how well every part of the student's idea is found in the project
        //   BestPair  = the single best matching passage (catches a copied paragraph)
        public const double CoverageWeight = 0.5;
        public const double BestPairWeight = 0.5;

        // Inside the word score: single words and two-word phrases (phrases catch copied wording)
        public const double SingleWordWeight = 0.6;
        public const double PhraseWeight = 0.4;

        // Raw cosine of our model: different projects of the same field score about 0.25,
        // near-copies about 0.85+. We stretch that range to 0..100.
        // If scores look too high or too low, change ONLY these two numbers.
        public const double CosineFloor = 0.25;
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

        public static string ClassFor(double finalScore)
        {
            return VerdictFor(finalScore) switch
            {
                "Already Done" => "done",
                "Similar" => "similar",
                _ => "unique"
            };
        }
    }
}