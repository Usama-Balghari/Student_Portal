namespace Student_Portal2.Middleware
{
    public class SessionTimeoutMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionTimeoutMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path;

            // 👇 Login page aur static files skip karo
            if (!path.StartsWithSegments("/Account/Login") &&
                !path.StartsWithSegments("/Account/Register") &&
                !path.StartsWithSegments("/css") &&
                !path.StartsWithSegments("/js") &&
                !path.StartsWithSegments("/lib"))
            {
                var user = context.Session.GetString("UserEmail");

                if (string.IsNullOrEmpty(user))
                {
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }

            await _next(context);
        }
    }

}
