using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Student_Portal2.Data;
using Student_Portal2.Models;

namespace Student_Portal2.Controllers
{
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDBContext _context;

        public DepartmentsController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: Departments
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Index()
        {
            var dept = await _context.Departments.Where(x => !x.IsDeleted).ToListAsync();
            return View(dept);
        }

        // GET: Departments/GetData (For DataTables)
        //public IActionResult GetData()
        //{
            
        //        var draw = Request.Query["draw"].FirstOrDefault();
        //        var start = Convert.ToInt32(Request.Query["start"].FirstOrDefault() ?? "0");
        //        var length = Convert.ToInt32(Request.Query["length"].FirstOrDefault() ?? "10");
        //        var searchValue = Request.Query["search[value]"].FirstOrDefault();
        //        var sortColumnIndex = Request.Query["order[0][column]"].FirstOrDefault();
        //        var sortDirection = Request.Query["order[0][dir]"].FirstOrDefault();
        //        var sortColumn = Request.Query[$"columns[{sortColumnIndex}][name]"].FirstOrDefault();

        //        var query = _context.Departments.Include(s=>s.Students).AsQueryable();
        //        int recordsTotal = query.Count();

        //        // Search
        //        if (!string.IsNullOrEmpty(searchValue))
        //        {
        //            query = query.Where(x => x.DepartmentName.Contains(searchValue));
        //        }

        //        int recordsFiltered = query.Count();

        //        // Sorting
        //        if (!string.IsNullOrEmpty(sortColumn))
        //        {
        //            query = sortDirection == "asc"
        //                ? query.OrderBy(x => EF.Property<object>(x, sortColumn))
        //                : query.OrderByDescending(x => EF.Property<object>(x, sortColumn));
        //        }
               

        //        // Pagination
        //        var data = query
        //            .Skip(start)
        //            .Take(length)
        //            .Select(x => new
        //            {
        //                id = x.Id,
        //                departmentName = x.DepartmentName,
        //                studentCount = x.Students.Count()
        //            })
        //            .ToList();

        //        return Json(new
        //        {
        //            draw = draw,
        //            recordsTotal = recordsTotal,
        //            recordsFiltered = recordsFiltered,
        //            data = data
        //        });
        //}
            
        

        // GET: Departments/Create
        public IActionResult Create()
        {
            return PartialView("_CreateDeptPartial");
        }

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                var deptExist = await _context.Departments.AnyAsync(d => d.DepartmentName == department.DepartmentName);
                if (deptExist)
                {
                    TempData["success"] = "Failed to Create Department";
                    return RedirectToAction(nameof(Index));
                }
                else
                {

                _context.Add(department);
                await _context.SaveChangesAsync();
                TempData["success"] = "Department has been created successfully.";
                return RedirectToAction(nameof(Index));
                }
            }
            return PartialView("_CreateDeptPartial", department);
        }

        // GET: Departments/Edit/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            return PartialView("_EditDeptPartial", department);
        }

        // POST: Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                //try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Department has been updated successfully.";
                }
                //catch (DbUpdateConcurrencyException)
                //{
                //    if (!DepartmentExists(department.Id))
                //    {
                //        return NotFound();
                //    }
                //    else
                //    {
                //        throw;
                //    }
                //}
                return RedirectToAction(nameof(Index));
            }
            return PartialView("_EditDeptPartial", department);
        }

        // GET: Departments/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var department = await _context.Departments
        //        .Include(d => d.Students)
        //        .FirstOrDefaultAsync(m => m.Id == id);

        //    if (department == null)
        //    {
        //        return NotFound();
        //    }

        //    return PartialView("_DeleteDeptPartial", department);
        //}

        // POST: Departments/DeleteConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Students)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return Json(new { success = false, message = "Department not found." });
            }

            if (department.Students.Any())
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot delete. There are {department.Students.Count} students in this department."
                });
            }

            department.IsDeleted = true;
            department.DeletedAt = DateTime.Now;

            _context.Departments.Update(department);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Department moved to recycle bin." });

        }

        //private bool DepartmentExists(int id)
        //{
        //    return _context.Departments.Any(e => e.Id == id);
        //}
    }
}