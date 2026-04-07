using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Student_Portal2.Data;
using Student_Portal2.Middleware;
using Student_Portal2.Models;
using Student_Portal2.Services;
using System.Data;
using System.Security.Claims;
var builder = WebApplication.CreateBuilder(args);
var columnOptions = new ColumnOptions();

columnOptions.AdditionalColumns = new List<SqlColumn>
{
    new SqlColumn { ColumnName = "UserName", DataType = SqlDbType.NVarChar, DataLength = 100 },
    new SqlColumn { ColumnName = "Role", DataType = SqlDbType.NVarChar, DataLength = 100 },
    new SqlColumn { ColumnName = "TargetUser", DataType = SqlDbType.NVarChar, DataLength = 100 },
    new SqlColumn { ColumnName = "Action", DataType = SqlDbType.NVarChar, DataLength = 100 },
    new SqlColumn { ColumnName = "PreviousRole", DataType = SqlDbType.NVarChar, DataLength = 50 },
    new SqlColumn { ColumnName = "CurrentRole", DataType = SqlDbType.NVarChar, DataLength = 50 },
    new SqlColumn { ColumnName = "PerformedBy", DataType = SqlDbType.NVarChar, DataLength = 100 }
};

columnOptions.TimeStamp.ColumnName = "TimeStamp";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.MSSqlServer(
    connectionString: builder.Configuration.GetConnectionString("Student_Portal2Context"),
    sinkOptions: new MSSqlServerSinkOptions
    {
        TableName = "Logs",
        AutoCreateSqlTable = true
    },
    columnOptions: columnOptions
)

    .CreateLogger();
builder.Host.UseSerilog();


builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Student_Portal2Context")));

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDBContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        googleOptions.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(
                context.RedirectUri + "&prompt=select_account"
            );
            return Task.CompletedTask;
        };


    })
     .AddGitHub(options =>
     {
         options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"];
         options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
         // Optional: Request user email address
         options.Scope.Add("user:email");
         options.Events = new OAuthEvents
         {
             OnRedirectToAuthorizationEndpoint = context =>
             {
                 // Append the 'prompt=login' parameter to the redirect URI.
                 // This instructs GitHub to display the authentication form 
                 // regardless of the user's current login status in the browser.
                 context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
                 return Task.CompletedTask;
             }
         };
     });

// 👇 Authorization Policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Now looks for the "Student" role in the AspNetRoles table
    options.AddPolicy("StudentOnly", policy =>
        policy.RequireRole("Student"));
});



builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1); 
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

});

builder.Services.AddControllersWithViews();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    if (!await roleManager.RoleExistsAsync("Student"))
    {
        await roleManager.CreateAsync(new IdentityRole("Student"));
    }

    string adminEmail = "admin@system.com";
    string adminPassword = "Admin@123";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();


app.UseAuthorization();
app.Use(async (context, next) =>
{
    var routeData = context.GetRouteData();


    var userName = context.User?.Identity?.Name ?? "Anonymous";
    string role = "Anonymous";

    if (context.User.Identity?.IsAuthenticated == true)
    {
        if (context.User.IsInRole("Admin"))
            role = "Admin";
        else if (context.User.IsInRole("Student"))
            role = "Student";
    }
    using (Serilog.Context.LogContext.PushProperty("UserName", userName))
    using (Serilog.Context.LogContext.PushProperty("Role", role))
    {
        await next();
    }
});

app.UseSession();

app.UseMiddleware<SessionTimeoutMiddleware>();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
