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
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(ApplicationDBContext context, ILogger<CoursesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var isAdmin = User.IsInRole("Admin");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (isAdmin)
            {
                var courses = await _context.Courses.Where(x => !x.IsDeleted).ToListAsync();
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
            try
            {
                if (ModelState.IsValid)
                {
                    bool exists = await _context.Courses
                        .AnyAsync(c => c.CourseName == course.CourseName);

                    if (exists)
                    {
                        _logger.LogWarning("Attempt to create duplicate course {CourseName}", course.CourseName);

                        TempData["success"] = "Failed to Create Course";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.Add(course);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Course created: {CourseName}", course.CourseName);

                    TempData["success"] = "Course has been created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                return PartialView("_CreateCoursePartial", course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating course");

                TempData["error"] = "Something went wrong.";
                return RedirectToAction(nameof(Index));
            }
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
            try
            {
                if (id != course.Id)
                {
                    _logger.LogWarning("Course edit failed. Id mismatch {Id}", id);
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Course updated: {CourseId}", course.Id);

                    TempData["success"] = "Course has been updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                return PartialView("_EditCoursePartial", course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course {CourseId}", id);
                throw;
            }
        }


        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var course = await _context.Courses
                    .Include(d => d.Students)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (course == null)
                {
                    _logger.LogWarning("Delete attempt for non-existing course {CourseId}", id);
                    return NotFound();
                }

                if (course.Students.Any())
                {
                    _logger.LogWarning("Delete blocked. Course {CourseId} has {StudentCount} students",
                        id, course.Students.Count);

                    return Json(new
                    {
                        success = false,
                        message = $"Cannot delete. There are {course.Students.Count} students in this course."
                    });
                }

                course.IsDeleted = true;
                course.DeletedAt = DateTime.Now;

                _context.Courses.Update(course);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Course moved to recycle bin." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", id);
                return Json(new { success = false });
            }
        }




        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Enroll(List<int> courseId)
        {
            try
            {
                if (User.IsInRole("Admin"))
                    return Forbid();

                if (courseId == null || !courseId.Any())
                    return RedirectToAction("Index");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var student = await _context.Student
                    .Include(s => s.Courses)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                    return NotFound();

                var selectedCourses = await _context.Courses
                    .Where(c => courseId.Contains(c.Id))
                    .ToListAsync();

                foreach (var course in selectedCourses)
                {
                    if (!student.Courses.Any(c => c.Id == course.Id))
                    {
                        student.Courses.Add(course);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Student {StudentId} enrolled in courses {@CourseIds}",
                    student.Id, courseId);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student");
                throw;
            }
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
            if (studentIds == null || !studentIds.Any())
            {
                TempData["Warn"] = "Select atleast one Student!!!";
                return RedirectToAction("Dashboard"); 
            }
            try
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

            _logger.LogInformation("Successfully added {Count} students to Course ID: {CourseId}", students.Count, courseId);
            TempData["Success"] = $"{students.Count} students added successfully.";

            return RedirectToAction("Dashboard");
        }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Unexpected error in AddStudentsToCourse for Course {CourseId}", courseId);
             TempData["Error"] = "An unexpected error occurred.";
         }

            return RedirectToAction("Dashboard");
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveStudentFromCourse(int courseId, int studentId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Students)
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                if (course == null)
                {
                    _logger.LogWarning("Course not found while removing student {CourseId}", courseId);
                    return Json(new { success = false });
                }

                var student = course.Students.FirstOrDefault(s => s.Id == studentId);

                if (student == null)
                {
                    _logger.LogWarning("Student {StudentId} not found in course {CourseId}",
                        studentId, courseId);

                    return Json(new { success = false });
                }

                course.Students.Remove(student);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Student {StudentId} removed from course {CourseId}",
                    studentId, courseId);

                var totalStudents = await _context.Student
                 .Where(s => s.Courses.Any())
                 .CountAsync();

                return Json(new { success = true, totalStudents = totalStudents });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing student {StudentId} from course {CourseId}",
                    studentId, courseId);

                return Json(new { success = false });
            }
        }


    }
}
