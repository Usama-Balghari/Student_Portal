using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Student_Portal2.Models;

namespace Student_Portal2.Data;
public class ApplicationDBContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
        : base(options)
    {
    }
    
    public DbSet<Student_Portal2.Models.Student> Student { get; set; } = default!;
    public DbSet<Student_Portal2.Models.Department> Departments { get; set; } = default!;
    public DbSet<Student_Portal2.Models.Course> Courses { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasOne(s => s.Department)
                  .WithMany(d => d.Students)
                  .HasForeignKey(s => s.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        }).Entity<Student>()
                  .HasMany(s => s.Courses)
                  .WithMany(c => c.Students)
                  .UsingEntity<Dictionary<string, object>>(
                      "CourseStudent", // Name of the join table
                      r => r.HasOne<Course>().WithMany().OnDelete(DeleteBehavior.Restrict),
                      l => l.HasOne<Student>().WithMany().OnDelete(DeleteBehavior.Cascade)
         );
        modelBuilder.Entity<Student>()
        .HasOne(s => s.User)
        .WithOne(u => u.Student)
        .HasForeignKey<Student>(s => s.UserId)
        .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }

}
