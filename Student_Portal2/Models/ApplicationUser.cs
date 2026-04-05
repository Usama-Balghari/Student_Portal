using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Student_Portal2.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfileImage { get; set; }
        public Student Student { get; set; }
        public string? Address { get; set; }
        public int? Age { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
