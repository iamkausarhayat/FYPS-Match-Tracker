namespace FypTracker.Models
{
    // One FYP record, as the rest of the app sees it (the database entity is in /Entities).
    public class FypProject
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public int Year { get; set; }

        // University ka naam ya short naam (jaise "NUML"). Khali ho to default university lagti hai.
        public string University { get; set; } = string.Empty;

        // Link to the original project page (can be empty)
        public string? SourceUrl { get; set; }
        public string? Area { get; set; }

        // true = we only have the title of this project
        public bool IsTitleOnly { get; set; }

        // Student names as the university lists them (can be empty)
        public string? AuthorsText { get; set; }

        // Award winner (shown on the home page)
        public bool IsAward { get; set; }
        public string? AwardText { get; set; }
    }
}