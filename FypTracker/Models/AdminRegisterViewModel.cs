using System.ComponentModel.DataAnnotations;

namespace FypTracker.Models
{
    public class AdminRegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(60, MinimumLength = 3, ErrorMessage = "Name must be 3 to 60 characters.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [RegularExpression(@"^[A-Za-z0-9_]{4,20}$", ErrorMessage = "Username must be 4 to 20 letters, numbers or underscores.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "The admin access code is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Admin access code")]
        public string AccessCode { get; set; } = string.Empty;

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