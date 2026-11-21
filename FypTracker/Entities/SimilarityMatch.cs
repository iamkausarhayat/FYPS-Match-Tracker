using System.ComponentModel.DataAnnotations;

namespace FypTracker.Entities
{
    // One matched item inside a check: a department FYP or a research paper
    public class SimilarityMatch
    {
        public int Id { get; set; }

        public MatchSource Source { get; set; }

        public double MeaningScore { get; set; }
        public double WordScore { get; set; }
        public double FinalScore { get; set; }

        [StringLength(500)]
        public string CommonKeywords { get; set; } = string.Empty;   // comma separated

        [StringLength(1000)]
        public string BestPassage { get; set; } = string.Empty;

        // Required foreign key: a match always belongs to one check
        public int SimilarityCheckId { get; set; }
        public SimilarityCheck SimilarityCheck { get; set; } = null!;

        // Exactly one of these two is filled, depending on Source
        public int? FypProjectId { get; set; }
        public FypProject? FypProject { get; set; }

        public int? ExternalPaperId { get; set; }
        public ExternalPaper? ExternalPaper { get; set; }
    }
}