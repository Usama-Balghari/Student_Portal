using System.ComponentModel.DataAnnotations;

namespace Student_Portal2.Models
{
    public class Course
    {
        public int Id { get; set; }
        [Required]
        public string CourseName { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
