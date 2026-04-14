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
            

            // 👇 session check
            var user = context.Session.GetString("UserEmail");


            await _next(context);
        }
    }

}
