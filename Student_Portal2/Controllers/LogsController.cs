using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Portal2.Data;
using Student_Portal2.Models;
using System.Security.Claims;

namespace Student_Portal2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LogsController : Controller
    {
        private readonly ApplicationDBContext _context;

        public LogsController(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.UserLogs
                  .Where(x => x.Action != null)
                  .OrderByDescending(x => x.TimeStamp)
                  .ToListAsync();

            return View("~/Views/logs/index.cshtml",logs);
        }
    }

}
