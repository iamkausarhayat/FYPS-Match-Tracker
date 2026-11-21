namespace FypTracker.Models
{
    // DTO = Data Transfer Object.
    // Yeh woh "parchi" hai jo API bahar bhejti hai. Is mein sirf wohi cheezein hain
    // jo bahar dikhani hain. Meaning fingerprint (EmbeddingJson) is mein nahi,
    // wo bohat bari hai aur andar ki cheez hai.
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public int Year { get; set; }
    }

    // /api/projects/stats ka jawab
    public class ProjectStatsDto
    {
        public int TotalProjects { get; set; }
        public int DepartmentCount { get; set; }
        public int LatestYear { get; set; }

        // Dictionary: department ka naam -> us ke projects ki ginti
        public Dictionary<string, int> ByDepartment { get; set; } = new();
    }
}