using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Student_Portal2.Models;
using Microsoft.EntityFrameworkCore;

namespace Student_Portal2.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public AccountApiController(UserManager<ApplicationUser> userManager) { _userManager = userManager; }

        [HttpGet("manage-users")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ManageUsers()
        {
            var currentUserId = _userManager.GetUserId(User);

            var users = await _userManager.Users
                .Where(u => u.Id != currentUserId)
                .ToListAsync();

            var userList = new List<RegisterViewModel>();

            foreach (var user in users)
            {
                userList.Add(new RegisterViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    IsAdmin = await _userManager.IsInRoleAsync(user, "Admin")
                });
            }

            return Ok(userList); // 🔥 MAIN CHANGE
        }
    }
}
