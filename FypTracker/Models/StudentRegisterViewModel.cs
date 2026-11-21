using System.ComponentModel.DataAnnotations;

namespace FypTracker.Models
{
    public class StudentRegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(60, MinimumLength = 3, ErrorMessage = "Name must be 3 to 60 characters.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Roll number is required.")]
        [StringLength(20, ErrorMessage = "Roll number is too long.")]
        [Display(Name = "Roll number")]
        public string RollNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your department.")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(@"^(?=.*\d).{8,}$", ErrorMessage = "Password must be at least 8 characters and include a number.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}