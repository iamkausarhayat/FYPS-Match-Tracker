using System.ComponentModel.DataAnnotations;

namespace FypTracker.Entities
{
    // A research paper fetched from the external API (OpenAlex) and saved by us
    public class ExternalPaper
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string ExternalId { get; set; } = string.Empty;   // the paper's id at OpenAlex

        [Required, StringLength(500)]
        public string Title { get; set; } = string.Empty;

        public string? Abstract { get; set; }                    // some papers have no abstract

        public int? PublicationYear { get; set; }

        [StringLength(500)]
        public string? Url { get; set; }

        [StringLength(300)]
        public string? Authors { get; set; }

        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
        // The "meaning fingerprint", saved as JSON text. Filled by the app, never typed by hand.
        public string? EmbeddingJson { get; set; }

        // Which AI model made the fingerprint (if we ever change the model, we rebuild them all)
        [StringLength(100)]
        public string? EmbeddingModel { get; set; }     

        public List<SimilarityMatch> Matches { get; set; } = new();
    }
}