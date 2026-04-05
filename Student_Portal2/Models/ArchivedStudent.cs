using System.ComponentModel.DataAnnotations;

namespace Student_Portal2.Models
{
    public class ArchivedStudent
    {
        [Key]
        public int ArchiveId { get; set; }

        // AspNetUsers se jo data lena hai
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string? PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfileImage { get; set; }
        public string? Address { get; set; }
        public int? Age { get; set; }

        // Student table se jo data lena hai
        public int? DepartmentId { get; set; }
        public int? StudentId { get; set; }

        // Archive ki details
        public DateTime ArchivedDate { get; set; } = DateTime.Now;
    }
}
