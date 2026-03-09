using System.ComponentModel.DataAnnotations;

namespace Student_Portal2.Models;

public class Student
{
    public int Id { get; set; }

    public int? DepartmentId { get; set; }

    public string? UserId { get; set; }   // FK to AspNetUsers
    public ApplicationUser? User { get; set; }
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public Department? Department { get; set; } = null!;


}

