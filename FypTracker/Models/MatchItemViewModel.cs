namespace FypTracker.Models
{
    // One matched item: a department FYP (research papers are switched off for now).
    public class MatchItemViewModel
    {
        public int? ProjectId { get; set; }                     // which FypProject this match points to
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;      // e.g. "Computer Science • 2023 • Supervisor: Dr. X"
        public double MeaningScore { get; set; }                // 0 to 100
        public double WordScore { get; set; }                   // 0 to 100
        public List<string> CommonKeywords { get; set; } = new();
        public string BestPassage { get; set; } = string.Empty; // why it matched
        public string? Url { get; set; }                        // only research papers have a link

        public double FinalScore => SimilarityRules.Combine(MeaningScore, WordScore);
    }
}