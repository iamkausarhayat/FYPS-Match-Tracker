using System.ComponentModel.DataAnnotations;

namespace FypTracker.Entities
{
    // One university whose final year projects we compare against.
    // Projects from all universities live in the same FypProjects table.
    public class University
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Short name shown on result cards, for example "UMT"
        [Required, StringLength(30)]
        public string ShortName { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string Country { get; set; } = string.Empty;

        // Home page of the repository, for example https://escholar.umt.edu.pk
        [StringLength(200)]
        public string? Website { get; set; }

        public UniversitySourceType SourceType { get; set; } = UniversitySourceType.Manual;

        // Only for DSpace universities, for example https://escholar.umt.edu.pk/server
        [StringLength(200)]
        public string? ApiBaseUrl { get; set; }

        public List<FypProject> Projects { get; set; } = new();
    }
}