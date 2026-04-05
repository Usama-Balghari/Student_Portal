using Student_Portal2.Data;
using Student_Portal2.Models;

namespace Student_Portal2
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ApplicationDBContext db)
        {
            // 🔹 Basic info
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            userId = userId ?? "Anonymous";
            var userName = context.User?.Identity?.Name ?? "Anonymous";

            string role = "Anonymous";
            if (context.User.Identity?.IsAuthenticated == true)
            {
                role = context.User.IsInRole("Admin") ? "Admin" : "Student";
            }

            var path = context.Request.Path;

            // 🔹 Request continue karo
            await _next(context);

            // 🔥 Controller se extra data uthao
            var action = context.Items["Action"]?.ToString();
            var targetUser = context.Items["TargetUser"]?.ToString();
            var previousRole = context.Items["PreviousRole"]?.ToString();
            var currentRole = context.Items["CurrentRole"]?.ToString();

            // ❗ Agar kuch important hai tabhi log karo
            if (!string.IsNullOrEmpty(action))
            {
                var log = new UserLog
                {
                    TargetUser = targetUser ?? userName,
                    Action = action,
                    PreviousRole = previousRole,
                    CurrentRole = currentRole ?? role,
                    PerformedBy = userName,
                    TimeStamp = DateTime.Now
                };

                db.UserLogs.Add(log);
                await db.SaveChangesAsync();
            }
        }
    }

}
