using System.ComponentModel.DataAnnotations;

namespace Student_Portal2.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "The FirstName is required")]
        [Display(Name = "FirstName")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "The LastName is required")]
        [Display(Name = "LastName")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "The UserName is required")]
        //[EmailAddress(ErrorMessage = "Invalid UserName!!")]
        [Display(Name = "UserName")]
        public string UserName { get; set; }

        // The Email field is required and must be a valid email format.
        [Required(ErrorMessage = "The email address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        // The Password field is required, has a minimum length, and a specific display name.
        [Required(ErrorMessage = "The password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        // The Confirm Password field is required and must match the Password field.
        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        public string? ProfileImage { get; set; }
        public IFormFile? ImageFile { get; set; }

        public string? UserId { get; set; }
        public bool IsAdmin { get; set; }

        public string Token { get; set; }
    }
}
