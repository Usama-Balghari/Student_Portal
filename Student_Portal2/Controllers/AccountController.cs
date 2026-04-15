using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Student_Portal2.Data;
using Student_Portal2.Models;
using System.Net;
using System.Security.Claims;

namespace Student_Portal2.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ApplicationUser> _logger;
        private readonly ApplicationDBContext _context;
        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 IEmailSender emailSender,
                                 ILogger<ApplicationUser> logger,
                                 ApplicationDBContext context
                                 )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _context = context;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_CreatePartial");
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (ModelState.IsValid)
                {
                    // 1. Create the login account (ApplicationUser)
                    var user = new ApplicationUser
                    {
                        UserName = model.UserName,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, model.NewPassword);
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                            ModelState.AddModelError("", error.Description);

                        return PartialView("_CreatePartial");
                    }
                    if (result.Succeeded)
                    {
                        var student = new Student { UserId = user.Id };

                        var roleName = model.IsAdmin ? "Admin" : "Student";
                        await _userManager.AddToRoleAsync(user, roleName);

                        _context.Add(student);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        TempData["success"] = "Student and User account created!";

                        return Json(new { success = true, url = Url.Action(nameof(ManageUsers)) });
                    }

                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);
                }

            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "An error occurred while creating the student.");
            }

            return PartialView("_CreatePartial");
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var currentUserId = _userManager.GetUserId(User);
                if (userId == currentUserId)
                {
                    return Json(new { success = false, message = "You cannot delete your own account." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                var student = await _context.Student.FirstOrDefaultAsync(s => s.UserId == userId);

                // 🔹 1. ARCHIVE MEIN MOVE  (Pehle save karein phir delete)
                var archivedData = new ArchivedStudent
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PasswordHash = user.PasswordHash, // Restore ke liye lazmi hai
                    SecurityStamp = user.SecurityStamp,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ProfileImage = user.ProfileImage,
                    Address = user.Address,
                    Age = user.Age,
                    DepartmentId = student?.DepartmentId, // Agar student null nahi hai
                    StudentId = student?.Id,
                    ArchivedDate = DateTime.Now
                };

                _context.ArchivedStudents.Add(archivedData);
                await _context.SaveChangesAsync();

                // 🔹 2. ORIGINAL TABLES SE DELETE KAREIN
                if (student != null)
                {
                    _context.Student.Remove(student);
                    await _context.SaveChangesAsync();
                }

                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync(); // Agar Identity delete fail ho jaye
                    return Json(new { success = false, message = "Delete failed." });
                }

                // 🔹 3. LOG SAVE
                var log = new UserLog
                {
                    TargetUser = user.UserName,
                    Action = "User Moved to Recycle Bin",
                    PreviousRole = "",
                    CurrentRole = "",
                    PerformedBy = User.Identity.Name,
                    TimeStamp = DateTime.Now
                };

                _context.UserLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Json(new { success = true, message = "User moved to Recycle Bin." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }
        //restore User/Student 
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> RestoreUser(int archiveId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Archive table se data fetch karein
                var archive = await _context.ArchivedStudents.FirstOrDefaultAsync(a => a.ArchiveId == archiveId);
                if (archive == null)
                {
                    return Json(new { success = false, message = "Record not found in Recycle Bin." });
                }

                // 2. AspNetUser (Identity User) 
                var user = new ApplicationUser
                {
                    Id = archive.UserId, 
                    UserName = archive.UserName,
                    Email = archive.Email,
                    NormalizedUserName = archive.UserName.ToUpper(),
                    NormalizedEmail = archive.Email.ToUpper(),
                    PasswordHash = archive.PasswordHash,
                    SecurityStamp = archive.SecurityStamp,
                    FirstName = archive.FirstName,
                    LastName = archive.LastName,
                    ProfileImage = archive.ProfileImage,
                    Address = archive.Address,
                    Age = archive.Age,
                    EmailConfirmed = true 
                };

                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    // 3. Student table mein entry wapis (agar wo student tha)
                    if (archive.DepartmentId.HasValue)
                    {
                        var student = new Student
                        {
                            UserId = user.Id,
                            DepartmentId = archive.DepartmentId.Value
                        };
                        _context.Student.Add(student);
                    }

                    // 4. Archive table se record delete kar den
                    _context.ArchivedStudents.Remove(archive);
                    await _context.SaveChangesAsync();

                    // 5. LOG SAVE
                    var log = new UserLog
                    {
                        TargetUser = user.UserName,
                        Action = "User Restored",
                        PerformedBy = User.Identity.Name,
                        TimeStamp = DateTime.Now
                    };
                    _context.UserLogs.Add(log);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return RedirectToAction("Index", "Recycle");
                }

                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Restore failed at Identity level." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        //yahn Khatum User/Student restore wala
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ModelState.Remove("Token");
            ModelState.Remove("Password");
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
               
                var user = new ApplicationUser
                {
                    FirstName = model.FirstName
                    ,
                    LastName = model.LastName
                    ,
                    UserName = model.UserName
                    ,
                    Email = model.Email
                };
                var result = await _userManager.CreateAsync(user, model.NewPassword);

                if (!result.Succeeded)
                {

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);

                }
                if (result.Succeeded)
                {
                    var student = new Student { UserId = user.Id };

                    await _userManager.AddToRoleAsync(user, "Student");

                    _context.Add(student);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();


                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action(
                        nameof(ConfirmEmail),
                        "Account",
                        new { token, email = user.Email },
                        Request.Scheme
                        );
                    await _emailSender.SendEmailAsync(
                        user.Email,
                        "Confirmation email link",
                        $"To Confirm Email: <a href='{confirmationLink}'>Click here</a>"
                        );
                    //await _userManager.AddToRoleAsync(user, "Visitor");
                    TempData["SuccessRegistration"] = "Registration successful!!\nPlease check your email to confirm your email first!!";
                    return RedirectToAction("Login");
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "An error occurred while creating the User.");
            }
            return RedirectToAction("Register", model);
        }


        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return View("Error");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            TempData["ConfirmEmail"] = " Thank you for confirming your email.";
            return RedirectToAction(result.Succeeded ? nameof(Login) : "Error");
        }
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (returnUrl != null)
            {
                ViewBag.Message = "You must login first to access that page";
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(RegisterViewModel model, string returnUrl = null)
        {
            try
            {
                ModelState.Remove("ConfirmPassword");
                ModelState.Remove("NewPassword");
                ModelState.Remove("UserName");
                ModelState.Remove("FirstName");
                ModelState.Remove("LastName");
                ModelState.Remove("Token");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Login failed - invalid model state for Email {Email}", model.Email);
                    return View(model);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, false, false);

                    if (result.Succeeded)
                    {
                        var role = await _userManager.IsInRoleAsync(user, "Admin") ? "Admin" : "Student";

                        using (Serilog.Context.LogContext.PushProperty("UserName", user.UserName))
                        using (Serilog.Context.LogContext.PushProperty("Role", role))
                        {
                            _logger.LogInformation("User {Email} logged in successfully", model.Email);
                        }

                        if (!string.IsNullOrEmpty(returnUrl))
                            return LocalRedirect(returnUrl);

                        HttpContext.Session.SetString("UserEmail", user.Email);

                        if (role == "Admin")
                            return RedirectToAction("Dashboard", "Courses");

                        return RedirectToAction("Index", "Courses");
                    }


                    if (!user.EmailConfirmed)
                    {
                        _logger.LogWarning("Login attempt with unconfirmed email {Email}", model.Email);

                        ModelState.AddModelError(string.Empty, "You must have a confirmed email.");
                        return View(model);
                    }

                    _logger.LogWarning("Invalid login attempt for Email {Email}", model.Email);

                    ModelState.AddModelError("", "Invalid Login Attempt");
                    return View(model);
                }

                _logger.LogWarning("Login failed - email not found {Email}", model.Email);

                ModelState.AddModelError(string.Empty, "The Email or Password is incorrect.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during login for Email {Email}", model.Email);

                ModelState.AddModelError("", "An error occurred while processing your request.");
                return View(model);
            }
        }



        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ManageUsers()
        {
            var currentUserId = _userManager.GetUserId(User);

            var users = await _userManager.Users
                .Where(u => u.Id != currentUserId)   // logged in admin remove
                .ToListAsync();

            var userList = new List<RegisterViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userList.Add(new RegisterViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    IsAdmin = await _userManager.IsInRoleAsync(user, "Admin")
                });
            }

            return View(userList);
        }




        //----------------------------
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ViewUserProfile(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var model = new RegisterViewModel
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UserName = user.UserName,
                ProfileImage = user.ProfileImage
            };
            return PartialView("_ViewProfilePartial", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ViewUserProfile(RegisterViewModel model)
        {
            ModelState.Remove("Token");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("Password");
            ModelState.Remove("NewPassword");
            if (!ModelState.IsValid) return PartialView("_ViewProfilePartial", model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // Update Basic Info
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;

            // Handle Profile Image Upload
            if (model.ImageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ImageFile.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }
                user.ProfileImage = "images/" + fileName;
            }
            else
            {
                user.ProfileImage = model.ProfileImage;
            }

            // Handle Password Reset (Admins don't need the old password)
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
                    return PartialView("_ViewProfilePartial",model);
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                TempData["Success"] = "User profile updated successfully!";
                return Json(new { success = true, url = Url.Action("ManageUsers") });
            }

            return PartialView("_ViewProfilePartial",model);
        }
        //----------------------------

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleAdmin(string userId, bool makeAdmin)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // 1. Previous role check
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            string previousRole = isAdmin ? "Admin" : "Student";
            string currentRole;

            // 2. Role change logic
            if (makeAdmin)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["Success"] = "A user has been promoted to admin.";
                currentRole = "Admin";
            }
            else
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                TempData["Warning"] = "A user has been removed from admin.";
                currentRole = "Student";
            }

            using (Serilog.Context.LogContext.PushProperty("TargetUser", user.UserName))
            using (Serilog.Context.LogContext.PushProperty("Action", "Role Updated"))
            using (Serilog.Context.LogContext.PushProperty("PreviousRole", previousRole))
            using (Serilog.Context.LogContext.PushProperty("CurrentRole", currentRole))
            using (Serilog.Context.LogContext.PushProperty("PerformedBy", User.Identity.Name))
            {
                _logger.LogInformation("Role updated for {UserName}");
            }

            return RedirectToAction("ManageUsers");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile( )
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login");
            }
            if (!User.IsInRole("Admin"))
            {
                var departmentName = await _context.Student
                    .Where(s => s.UserId == user.Id)
                    .Select(s => s.Department.DepartmentName)
                    .FirstOrDefaultAsync();

                ViewBag.DepartmentName = departmentName ?? "No Department Assigned";
            }
            var model = new RegisterViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                ProfileImage = user.ProfileImage
                
            };

            return View(model);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Profile(RegisterViewModel model)
        {
            ModelState.Remove("Token");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("NewPassword");
            if (!ModelState.IsValid)
            {
                ViewBag.DepartmentName = Request.Form["DepartmentName"];
                return View(model);
            }
            var user = await _userManager.GetUserAsync(User);
            
            if (user == null)
                return RedirectToAction("Login");

            
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.UserName = model.UserName;
            user.Email = model.Email;

            if (model.ImageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ImageFile.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }
                user.ProfileImage = "images/" + fileName;
            }
            else
            {
                user.ProfileImage = model.ProfileImage;
            }

            await _userManager.UpdateAsync(user);
            if (model.Password != null)
            {
                bool isPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.Password);
                if (isPasswordCorrect)
                {
                    if (!string.IsNullOrWhiteSpace(model.NewPassword))
                    {

                        var result = await _userManager.ChangePasswordAsync(
                        user,
                        model.Password,        // old password input 
                        model.NewPassword // new password
                    );
                    if (result.Succeeded)
                    {
                        await _signInManager.RefreshSignInAsync(user);
                    }
                    }

                }
                if (!isPasswordCorrect)
                {
                    TempData["Success"] = "Current Password is incorrect!!!";
                    return RedirectToAction("Profile");
                }
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
            }
            TempData["Success"] = "Current Password is required!";
            return RedirectToAction("Profile");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> RegisteredEmail()
        { 
            return  View();
        }
        [HttpPost]
        public async Task<IActionResult> RegisteredEmail(RegisterViewModel model)
        {
            ModelState.Remove("UserName");
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("NewPassword");
            ModelState.Remove("FirstName");
            ModelState.Remove("LastName");
            ModelState.Remove("Token");
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                TempData["NoData"] = "User Not Found";
                return View(model);
            }
            if (user !=null && !user.EmailConfirmed)
            {
                ModelState.AddModelError("", "You must have a confirmed email.");
                var triggerUrl = Url.Action(
                    "SendConfirmationAndRedirect",
                    "Account",
                    new { email = user.Email }
                    );

                // Corrected string interpolation ($) to insert the URL
                TempData["ResetPassword"] = $"To Confirm Your Email Now!!! <a href='{triggerUrl}'>Click here</a>";

                return View();
            }

            // 4️⃣ Email ownership confirm (TOKEN)
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);

            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { email = user.Email, token = encodedToken },
                Request.Scheme
            );

            await _emailSender.SendEmailAsync(
                user.Email,
                "Reset your password",
                $"Click to reset password: <a href='{resetLink}'>Reset</a>"
            );
            TempData["ConfirmToResetPassword"] = "Check Your Email to Reset Passsword";
            return View(model);
        }
        
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                ModelState.AddModelError("", "Invalid Credential");
                return View();
            }
                return View(new RegisterViewModel
                {
                    Email = email,
                    Token = token
                });
            }


        [HttpPost]
        public async Task<IActionResult> ResetPassword(RegisterViewModel model)
        {
            ModelState.Remove("UserName");
            ModelState.Remove("FirstName");
            ModelState.Remove("LastName");
            ModelState.Remove("Password");
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction("Login");

            var decodedToken = WebUtility.UrlDecode(model.Token);

            var result = await _userManager.ResetPasswordAsync(
                user,
                decodedToken,
                model.NewPassword
            );
            if (!result.Succeeded)
            {

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);

            }
            if (result.Succeeded)
            {
                TempData["ConfirmedResetPassword"] = " Your Passsword is Updated!!";
                return RedirectToAction(nameof(Login));
            }
            return View(model);
        }
        //Google Login Wala Yahan sa la kar

        [HttpPost]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null,string source = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl,source});
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(
    string? returnUrl = null,
    string? remoteError = null,
    string source = null)
        {
            if (remoteError != null)
            {
                TempData["Error"] = $"External provider error: {remoteError}";
                return RedirectToAction("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction("Login");

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname);

            if (email == null)
            {
                TempData["Error"] = "Email not received from provider.";
                return RedirectToAction("Login");
            }

            // 🔹 1. Try sign-in if already linked
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false);

            if (signInResult.Succeeded)
                return RedirectToAction("Index", "Courses");

            // 🔹 2. Check existing user
            var user = await _userManager.FindByEmailAsync(email);

            // ================= LOGIN FLOW =================
            if (source == "Login")
            {
                if (user == null)
                {
                    TempData["RegisterFirst"] = "Account not found. Please register first.";
                    return RedirectToAction("Register");
                }

                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                {
                    TempData["Error"] = "Unable to link external login.";
                    return RedirectToAction("Login");
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Courses");
            }

            // ================= REGISTER FLOW =================
            if (source == "Register")
            {
                if (user != null)
                {
                    TempData["Error"] = "Account already exists. Please login.";
                    return RedirectToAction("Login");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 🔹 Create user
                    user = new ApplicationUser
                    {
                        UserName = email.Split('@')[0],
                        Email = email,
                        EmailConfirmed = true,
                        FirstName = firstName,
                        LastName = lastName
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                        throw new Exception("User creation failed");

                    // 🔹 Link Google login
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (!addLoginResult.Succeeded)
                        throw new Exception("External login linking failed");

                    // 🔹 Assign Student role
                    await _userManager.AddToRoleAsync(user, "Student");

                    // 🔹 Create Student record
                    var student = new Student
                    {
                        UserId = user.Id
                    };

                    _context.Student.Add(student);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["RegisterSuccess"] = "Registration successful. Please login.";
                    return RedirectToAction("Login");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Something went wrong during registration.";
                    return RedirectToAction("Register");
                }
            }

            return RedirectToAction("Login");
        }

       

        //Goolgle Login Wala Yahan tak
        public async Task<IActionResult> Logout()
        {
            try
            {
                // ✅ clear session first
                HttpContext.Session.Clear();

                // ✅ sign out user (cookie remove)
                await _signInManager.SignOutAsync();

                _logger.LogInformation("User {User} logged out successfully.", User?.Identity?.Name);

                TempData["Logout"] = "You have been logged out successfully!";

                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout.");

                TempData["Error"] = "Logout failed. Please try again.";

                return RedirectToAction("Dashboard", "Courses");
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> SendConfirmationAndRedirect(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new { token, email = user.Email },
                Request.Scheme
                );

            await _emailSender.SendEmailAsync(
                email,
                "Confirmation Link",
                $" To Confirm Email: <a  href='{confirmationLink}'>Click here</a>"
                );

            // After sending, take them to a "Check your email" page or Home
                TempData["AResetPassword"] = $"Please Check Your Email";
            return RedirectToAction("RegisteredEmail");
        }

       
    }
}