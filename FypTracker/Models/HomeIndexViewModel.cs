namespace FypTracker.Models
{
    // Data for the landing page
    public class HomeIndexViewModel
    {
        public int ProjectCount { get; set; }
        public int UniversityCount { get; set; }

        // Top FYPs for the start of the page
        public List<BrowseItem> TopProjects { get; set; } = new();

        // true = these are award winners, false = no award is marked yet, so these are the latest projects
        public bool TopAreAwards { get; set; }
    }
}