namespace FypTracker.Models
{
    // Ek project card ke liye zaroori cheezein (browse page aur home page dono par)
    public class BrowseItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string University { get; set; } = string.Empty;       // short naam, jaise "UMT"
        public string UniversityName { get; set; } = string.Empty;   // poora naam
        public int Year { get; set; }
        public string? Area { get; set; }
        public string Supervisor { get; set; } = string.Empty;
        public string? Authors { get; set; }
        public string? SourceUrl { get; set; }
        public bool IsAward { get; set; }
        public string? AwardText { get; set; }
        public bool IsTitleOnly { get; set; }

        public bool HasSourceUrl => !string.IsNullOrWhiteSpace(SourceUrl);

        // Card par chhota hissa (pehle 220 characters)
        public string Snippet => Description.Length <= 220 ? Description : Description.Substring(0, 220).TrimEnd() + "...";
        public bool HasMore => Description.Length > 220;
    }

    // Filter box ki ek choice: naam aur kitne projects
    public record BrowseFacet(string Name, int Count);

    public class BrowseViewModel
    {
        public const int PageSize = 12;

        // Jo student ne filter lagaya
        public string? Query { get; set; }
        public string? University { get; set; }
        public int? Year { get; set; }
        public string? Area { get; set; }
        public bool AwardsOnly { get; set; }
        public int Page { get; set; } = 1;

        // Nateeja
        public List<BrowseItem> Items { get; set; } = new();
        public int Total { get; set; }
        public int TotalPages { get; set; } = 1;

        // Filter box ki choices
        public List<BrowseFacet> Universities { get; set; } = new();
        public List<int> Years { get; set; } = new();
        public List<BrowseFacet> Areas { get; set; } = new();

        public string? Error { get; set; }

        public bool HasFilter =>
            !string.IsNullOrWhiteSpace(Query) || !string.IsNullOrWhiteSpace(University) ||
            Year.HasValue || !string.IsNullOrWhiteSpace(Area) || AwardsOnly;
    }
}