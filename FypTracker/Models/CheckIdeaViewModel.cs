using System.ComponentModel.DataAnnotations;

namespace FypTracker.Models
{
    public class CheckIdeaViewModel
    {
        [Required(ErrorMessage = "Please upload your proposal file.")]
        [Display(Name = "Proposal file")]
        public IFormFile? Document { get; set; }

        [Required(ErrorMessage = "Project title is required.")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Title must be 5 to 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(3000, MinimumLength = 50, ErrorMessage = "Description must be 50 to 3000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Supervisor name is required.")]
        [StringLength(60, MinimumLength = 3, ErrorMessage = "Supervisor name must be 3 to 60 characters.")]
        [Display(Name = "Supervisor name")]
        public string SupervisorName { get; set; } = string.Empty;
    }
}