using System.ComponentModel.DataAnnotations;

namespace FypTracker.Entities
{
    // One time a student or guest pressed "Analyze"
    public class SimilarityCheck
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(3000)]
        public string Description { get; set; } = string.Empty;

        // Optional for the student: an empty text is saved when it was not given
        [StringLength(60)]
        public string SupervisorName { get; set; } = string.Empty;

        [StringLength(260)]
        public string? FileName { get; set; }

        public double OverallScore { get; set; }

        [Required, StringLength(20)]
        public string Verdict { get; set; } = string.Empty;    // Unique, Similar or Already Done

        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        // Coverage: how many projects / universities this check was compared with
        public int ComparedProjectCount { get; set; }
        public int ComparedUniversityCount { get; set; }

        // The suggestion box (headline, message, tips) saved as JSON, so the result page
        // shows the same text every time. Empty for old checks.
        public string? SuggestionJson { get; set; }

        // Optional foreign key: empty means the check was made by a guest
        public int? UserId { get; set; }
        public AppUser? User { get; set; }

        public List<SimilarityMatch> Matches { get; set; } = new();
    }
}