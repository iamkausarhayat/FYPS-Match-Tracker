namespace FypTracker.Models
{
    public class AdminProjectsViewModel
    {
        public List<FypProject> Projects { get; set; } = new();

        public int TotalProjects { get; set; }
        public int DepartmentCount { get; set; }
        public int LatestYear { get; set; }

        // department name -> number of projects (a Dictionary)
        public Dictionary<string, int> ByDepartment { get; set; } = new();

        public string? Query { get; set; }
        public string? SelectedDepartment { get; set; }
    }
}