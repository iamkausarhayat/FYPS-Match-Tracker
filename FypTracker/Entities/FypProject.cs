using System.ComponentModel.DataAnnotations;

namespace FypTracker.Entities
{
    // One completed FYP kept by the department
    public class FypProject
    {
        public int Id { get; set; }

        [Required, StringLength(150, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(3000, MinimumLength = 50)]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(60, MinimumLength = 3)]
        public string SupervisorName { get; set; } = string.Empty;

        [Range(2010, 2035)]
        public int Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // The "meaning fingerprint", saved as JSON text. Filled by the app, never typed by hand.
        public string? EmbeddingJson { get; set; }

        // Which AI model made the fingerprint (if we ever change the model, we rebuild them all)
        [StringLength(100)]
        public string? EmbeddingModel { get; set; }

        // Required foreign key: every project belongs to a department
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        public List<SimilarityMatch> Matches { get; set; } = new();
    }
}