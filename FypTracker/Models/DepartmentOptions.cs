namespace FypTracker.Models
{
    // Temporary list. In B4 departments come from the database.
    public static class DepartmentOptions
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "Computer Science",
            "Software Engineering",
            "Artificial Intelligence",
            "Data Science"
        };
    }
}