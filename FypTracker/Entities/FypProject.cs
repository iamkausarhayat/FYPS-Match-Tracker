using System.ComponentModel.DataAnnotations;

namespace FypTracker.Entities
{
    // One completed FYP (or thesis) kept in our database, from any university
    public class FypProject
    {
        public int Id { get; set; }

        [Required, StringLength(300, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        // Can be empty when a university only gives the title (see DataQuality)
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        // Can be empty: many universities do not publish the supervisor
        [StringLength(100)]
        public string SupervisorName { get; set; } = string.Empty;

        [Range(1990, 2035)]
        public int Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // The "meaning fingerprint", saved as JSON text. Filled by the app, never typed by hand.
        public string? EmbeddingJson { get; set; }

        // Which AI model made the fingerprint (if we ever change the model, we rebuild them all)
        [StringLength(100)]
        public string? EmbeddingModel { get; set; }

        // ---------- Where the project comes from ----------

        // Link to the original page, so the student can open the real project
        [StringLength(500)]
        public string? SourceUrl { get; set; }

        // The id the university's own system uses (for DSpace this is the item uuid)
        [StringLength(100)]
        public string? ExternalId { get; set; }

        // Student names as the university lists them (not always clean)
        [StringLength(300)]
        public string? AuthorsText { get; set; }

        // Subject area, for example "Computer science"
        [StringLength(100)]
        public string? Area { get; set; }

        // Full = we have a real description, TitleOnly = we only have the title
        public DataQuality DataQuality { get; set; } = DataQuality.Full;

        // Fingerprint of title + description, used to spot the same project twice
        [StringLength(64)]
        public string? ContentHash { get; set; }

        // When the importer last saw this project at its source
        public DateTime? LastFetchedAt { get; set; }

        // ---------- Awards (shown on the home page) ----------
        public bool IsAward { get; set; }

        [StringLength(200)]
        public string? AwardText { get; set; }

        // Required foreign keys: every project belongs to a department and to a university
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        public int UniversityId { get; set; }
        public University University { get; set; } = null!;

        public List<SimilarityMatch> Matches { get; set; } = new();
    }
}