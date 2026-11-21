using System.ComponentModel.DataAnnotations;

namespace FypTracker.Entities
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required, StringLength(60, MinimumLength = 3)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(254), EmailAddress]
        public string? Email { get; set; }        // students log in with email

        [StringLength(20)]
        public string? Username { get; set; }     // admins log in with username

        [StringLength(20)]
        public string? RollNumber { get; set; }   // students only

        [Required]
        public string PasswordHash { get; set; } = string.Empty;   // never the real password

        public UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional foreign key: admins have no department
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public List<SimilarityCheck> Checks { get; set; } = new();
    }
}