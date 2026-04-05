using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Portal2.Data;
using Student_Portal2.Models;

namespace Student_Portal2.Controllers
{
    public class RecycleController : Controller
    {
        private readonly ApplicationDBContext _context;

        public RecycleController(ApplicationDBContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new RecycleViewModel
            {
                AchrivedUser = _context.ArchivedStudents.ToList(),
                Departments = _context.Departments.Where(x => x.IsDeleted).ToList(),
                Courses = _context.Courses.Where(x => x.IsDeleted).ToList()
            };

            return View(model);
        }

        // --- RESTORE ACTIONS ---

        [HttpPost]
        public async Task<IActionResult> RestoreDepartment(int id) => await HandleRestore(_context.Departments, id);

        [HttpPost]
        public async Task<IActionResult> RestoreCourse(int id) => await HandleRestore(_context.Courses, id);


        // --- PERMANENT DELETE ACTIONS ---

        [HttpPost]
        public async Task<IActionResult> PermanentDeleteDepartment(int id) => await HandlePermanentDelete(_context.Departments, id);

        [HttpPost]
        public async Task<IActionResult> PermanentDeleteCourse(int id) => await HandlePermanentDelete(_context.Courses, id);


        // --- HELPER METHODS (To keep code clean) ---

        private async Task<IActionResult> HandleRestore<T>(DbSet<T> dbSet, int id) where T : class
        {
            var item = await dbSet.FindAsync(id);
            if (item == null) return Json(new { success = false });

            // Casting to dynamic to access properties
            dynamic dItem = item;
            dItem.IsDeleted = false;
            dItem.DeletedAt = null;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Restored successfully." });
        }

        private async Task<IActionResult> HandlePermanentDelete<T>(DbSet<T> dbSet, int id) where T : class
        {
            var item = await dbSet.FindAsync(id);
            if (item == null) return Json(new { success = false });

            dbSet.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Deleted permanently." });
        }
    }
}
