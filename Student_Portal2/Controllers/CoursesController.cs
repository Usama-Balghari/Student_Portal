using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Student_Portal2.Data;
using Student_Portal2.Models;
using System.Security.Claims;

namespace Student_Portal2.Controllers
{
    [Authorize]
    public class CoursesController : Controller
    {
        private readonly ApplicationDBContext _context;

        public CoursesController(ApplicationDBContext context)
        {
            _context = context;
        }
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var isAdmin = User.IsInRole("Admin");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (isAdmin)
            {
                var courses = await _context.Courses.ToListAsync();
                return View(courses);
            }
            else
            {
                var student = await _context.Student
                    .Include(s => s.Courses)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                    return NotFound();

                // Available Courses (Not Enrolled Yet)
                var enrolledIds = student.Courses.Select(c => c.Id).ToList();

                var availableCourses = await _context.Courses
                    .Where(c => !enrolledIds.Contains(c.Id))
                    .ToListAsync();

                ViewBag.AvailableCourses = new SelectList(
                    availableCourses,
                    "Id",
                    "CourseName"
                );

                return View(student.Courses); 
            }
        }


        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(Course course)
        {
            if (ModelState.IsValid)
            {
                // Check if the course already exists (e.g., by Name)
                bool exists = await _context.Courses.AnyAsync(c => c.CourseName == course.CourseName);

                if (exists)
                {
                    TempData["success"] = "Failed to Create Course";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _context.Add(course);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Course has been created successfully.";
                    return RedirectToAction(nameof(Index));
                }
            }
            // Returns the partial view with validation errors if the check fails
            return PartialView("_CreateCoursePartial", course);
        }

        // GET: Courses/Edit/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            return PartialView("_EditCoursePartial", course);
        }
        // POST: Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Course has been updated successfully.";
                }
            return RedirectToAction(nameof(Index));
            }
            return PartialView("_EditCoursePartial", course);
        }


        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses
                .Include(d => d.Students)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (course == null)
            {
                return NotFound();
            }
            if (course.Students.Any())
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot delete. There are {course.Students.Count} students in this courses."
                });
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "One record has been deleted." });
        }



      
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Enroll(List<int> courseId) // Renamed to courseIds in logic for clarity, but keeping your parameter name
        {
            // 1. Safety Check: Ensure the user isn't an Admin
            if (User.IsInRole("Admin"))
                return Forbid();

            if (courseId == null || !courseId.Any())
                return RedirectToAction("Index");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Load student and their current courses
            var student = await _context.Student
                .Include(s => s.Courses)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return NotFound();
            //student.Courses.Clear();
            // 3. Fetch all courses matching the IDs provided in the list
            var selectedCourses = await _context.Courses
                .Where(c => courseId.Contains(c.Id))
                .ToListAsync();

            // 4. Loop through selected courses and add new ones
            foreach (var course in selectedCourses)
            {
                // Only add if the student isn't already enrolled in this specific course
                if (!student.Courses.Any(c => c.Id == course.Id))
                {
                    student.Courses.Add(course);
                }
            }

            // 5. Save all changes to the join table at once
            await _context.SaveChangesAsync();

            // 6. Redirect back to the Students Index
            return RedirectToAction("Index");
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            // 1. Get total count of students who have at least one course
            ViewBag.TotalEnrolledStudents = await _context.Student
                .Where(s => s.Courses.Any())
                .CountAsync();

            // 2. Fetch courses including the Students and their linked Identity User profiles
            var coursesWithStudents = await _context.Courses
                .Include(c => c.Students)
                    .ThenInclude(s => s.User)
                .ToListAsync();

            return View(coursesWithStudents);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStudentsForCourse(int courseId)
        {
            var course = await _context.Courses
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound();

            var enrolledIds = course.Students.Select(s => s.Id).ToList();

            var students = await _context.Student
                .Where(s => !enrolledIds.Contains(s.Id))
                .Include(s => s.User)
                .ToListAsync();

            ViewBag.CourseId = courseId;

            return PartialView("_AddStudentPartial", students);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddStudentsToCourse(int courseId, List<int> studentIds)
        {
            var course = await _context.Courses
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound();

            var students = await _context.Student
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync();

            foreach (var student in students)
            {
                if (!course.Students.Any(s => s.Id == student.Id))
                {
                    course.Students.Add(student);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveStudentFromCourse(int courseId, int studentId)
        {
            var course = await _context.Courses
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return Json(new { success = false });

            var student = course.Students.FirstOrDefault(s => s.Id == studentId);

            if (student == null)
                return Json(new { success = false });

            course.Students.Remove(student);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

    }
}
