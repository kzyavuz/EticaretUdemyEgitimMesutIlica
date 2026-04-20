using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Service.Container;
using Service.Extensions;
using Service.Service;
using System.Threading.RateLimiting;
using WebUI.Helper;
using WebUI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure ────────────────────────────────────────────────────────────

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = "Eticaret.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.IOTimeout = TimeSpan.FromMinutes(10);
});

var mvcBuilder = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment())
    mvcBuilder.AddRazorRuntimeCompilation();

// ── Database & Identity ───────────────────────────────────────────────────────

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(10);
    options.Lockout.MaxFailedAccessAttempts = 1;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<DatabaseContext>()
.AddDefaultTokenProviders();

// ── Authentication & Authorization ───────────────────────────────────────────

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/giris";
    options.AccessDeniedPath = "/404";
    options.Cookie.Name = "Account.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;

    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            if (context.Request.Path.ToString().ToLower().StartsWith("/admin"))
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            }

            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/giris?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect("/404");
            return Task.CompletedTask;
        },
        OnValidatePrincipal = async context =>
        {
            var issuedUtc = context.Properties.IssuedUtc;
            if (issuedUtc.HasValue && (DateTimeOffset.UtcNow - issuedUtc.Value) > TimeSpan.FromHours(3))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            }
        }
    };
});

builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", p => p.RequireRole("Admin", "SuperAdmin", "Editor"));
    options.AddPolicy("UserPolicy", p => p.RequireRole("User", "Admin", "SuperAdmin", "Editor"));
});

// ── Rate Limiter ──────────────────────────────────────────────────────────────

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("LoginPolicy", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

// ── Application Services ──────────────────────────────────────────────────────

builder.Services.ContainerDependencies();
builder.Services.AddScoped<ExcelImportHelper>();
builder.Services.AddScoped<FileHelper>();

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
    var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
    FunctionHelper.Configure(accessor, cartService);
}

// ── Database Seeding ──────────────────────────────────────────────────────────

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
    string[] roles = ["Admin", "SuperAdmin", "Editor", "User"];

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new AppRole { Name = role });
    }
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Routes & Endpoints ────────────────────────────────────────────────────────

app.MapHub<ImportProgressHub>("/importProgress");
app.MapStaticAssets();

app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

app.Run();