using Core.Entities;
using Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    // Sifre gereksinimleri
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;

    // Hesap onaylama gereksinimi (email onayi gibi)
    options.SignIn.RequireConfirmedAccount = false; // Simdilik devre disi

    // Hesap kilitleme ayarlari
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(10); // Basarisiz giris denemelerinden sonra kilitleme s�resi
    options.Lockout.MaxFailedAccessAttempts = 1; // Ka� basarisiz denemeden sonra kilitlenecegi
    options.Lockout.AllowedForNewUsers = true;   // Yeni kullanicilarin kilitlenmesine izin ver

    // Kullanici adi ayarlari
    options.User.RequireUniqueEmail = true; // Her email adresinin benzersiz olmasi gerekir
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
})
.AddEntityFrameworkStores<DatabaseContext>()
.AddRoles<AppRole>()
.AddDefaultTokenProviders();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
  name: "admin",
  pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
