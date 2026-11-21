namespace FypTracker.Models
{
    // One FYP record kept by the department.
    // In B1 this becomes a real database entity with relationships.
    public class FypProject
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public int Year { get; set; }
    }
}