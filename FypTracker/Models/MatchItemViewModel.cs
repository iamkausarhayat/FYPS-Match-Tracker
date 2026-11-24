namespace FypTracker.Models
{
    // One matched project (from any university).
    public class MatchItemViewModel
    {
        public int? ProjectId { get; set; }                     // which FypProject this match points to
        public string Title { get; set; } = string.Empty;

        // One line for the PDF and small text, e.g. "UMT • Computer Science • 2024 • Supervisor: Dr. X"
        public string Detail { get; set; } = string.Empty;

        public double MeaningScore { get; set; }                // 0 to 100
        public double WordScore { get; set; }                   // 0 to 100
        public List<string> CommonKeywords { get; set; } = new();
        public string BestPassage { get; set; } = string.Empty; // why it matched

        // ---------- Where the project comes from ----------
        public string UniversityName { get; set; } = string.Empty;
        public string UniversityShort { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Area { get; set; } = string.Empty;
        public string Supervisor { get; set; } = string.Empty;  // empty when the university does not publish it
        public string Authors { get; set; } = string.Empty;     // as the university lists them

        // Link to the real project page. Empty = "Link not available".
        public string? SourceUrl { get; set; }
        public bool HasSourceUrl => !string.IsNullOrWhiteSpace(SourceUrl);

        // true when we only had the title of that project (so the match is less reliable)
        public bool IsLowConfidence { get; set; }

        public string? Url { get; set; }                        // only research papers have this link

        public double FinalScore => SimilarityRules.Combine(MeaningScore, WordScore);
        public string ScoreClass => SimilarityRules.ClassFor(FinalScore);
    }
}