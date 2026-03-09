using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Student_Portal2.Models
{
    public class UserEditViewModel
    {
        public int Id { get; set; }

        // User fields
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

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
        public string? Address { get; set; }
        public int? Age { get; set; }
        public int? DepartmentId { get; set; }

        public bool IsAdmin { get; set; }
    }

}
