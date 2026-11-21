using System.ComponentModel.DataAnnotations;

namespace FypTracker.Entities
{
    public class Department
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;

        // Navigation properties: one department has many projects and many users
        public List<FypProject> Projects { get; set; } = new();
        public List<AppUser> Users { get; set; } = new();
    }
}