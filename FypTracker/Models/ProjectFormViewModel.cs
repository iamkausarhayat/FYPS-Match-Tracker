using System.ComponentModel.DataAnnotations;

namespace FypTracker.Models
{
    public class ProjectFormViewModel
    {
        public int Id { get; set; }                       // 0 = new project, otherwise editing
        public bool IsEdit => Id != 0;

        [Required(ErrorMessage = "Project title is required.")]
        [StringLength(300, MinimumLength = 5, ErrorMessage = "Title must be 5 to 300 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a university.")]
        public string University { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a department.")]
        public string Department { get; set; } = string.Empty;

        // "string?" zaroori hai: bina ? ke ASP.NET is field ko required samajhta hai.
        // Supervisor optional hai, kyunke bohat si universities ye publish nahi karti.
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Supervisor name must be 3 to 100 characters.")]
        [Display(Name = "Supervisor name (optional)")]
        public string? SupervisorName { get; set; }

        [Range(1990, 2035, ErrorMessage = "Enter a year between 1990 and 2035.")]
        public int Year { get; set; }

        // Khali chhor sakte hain (sirf title wala project, "Title only" ka nishaan lagega)
        [StringLength(3000, MinimumLength = 50, ErrorMessage = "Description must be 50 to 3000 characters, or leave it empty if only the title is known.")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "The link is too long.")]
        [Url(ErrorMessage = "Enter a full link that starts with http:// or https://")]
        [Display(Name = "Link to the original project (optional)")]
        public string? SourceUrl { get; set; }

        [StringLength(100, ErrorMessage = "Area must be 100 characters or less.")]
        [Display(Name = "Area (optional)")]
        public string? Area { get; set; }

        [Display(Name = "This project won an award")]
        public bool IsAward { get; set; }

        [StringLength(200, ErrorMessage = "Award text must be 200 characters or less.")]
        [Display(Name = "Award (optional)")]
        public string? AwardText { get; set; }
    }
}