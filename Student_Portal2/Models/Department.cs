using System.ComponentModel.DataAnnotations;

namespace Student_Portal2.Models
{
    public class Department
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "The FirstName is required")]
        public string DepartmentName { get; set; }

        public ICollection<Student> Students { get; set; } = new List<Student>();

    }
}
