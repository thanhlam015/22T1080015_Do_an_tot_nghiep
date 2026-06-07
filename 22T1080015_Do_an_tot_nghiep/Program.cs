using Microsoft.EntityFrameworkCore;
using _22T1080015_Do_an_tot_nghiep.Models;
using _22T1080015_Do_an_tot_nghiep.Services;
using Microsoft.AspNetCore.Authentication.Cookies;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DoAnTotNghiepContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SV22t1080015CommerceBD")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "CookieAuth";
    options.DefaultChallengeScheme = "CookieAuth";
    options.DefaultSignInScheme = "CookieAuth";
})
.AddCookie("CookieAuth", options =>
{
    options.LoginPath = "/Admin/Account/Login";
    options.AccessDeniedPath = "/Admin/Account/AccessDenied";
    options.LogoutPath = "/Admin/Account/Logout";

    options.Cookie.Name = "TravelBotAdminAuth";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddCookie("UserCookie", options =>
{
    options.LoginPath = "/Users/Auth";
    options.AccessDeniedPath = "/Users/Auth";
    options.LogoutPath = "/Users/Logout";

    options.Cookie.Name = "TravelBotUserAuth";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin", "Staff");
    });
});
builder.Services.Configure<AISettings>(
    builder.Configuration.GetSection("AISettings"));

builder.Services.Configure<WeatherSettings>(
    builder.Configuration.GetSection("WeatherSettings"));
builder.Services.Configure<ChatbotSettings>(
    builder.Configuration.GetSection("ChatbotSettings"));

builder.Services.AddHttpClient();

builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IRagIndexingService, RagIndexingService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(6);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Đảm bảo có dòng này để chạy CSS/JS
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .RequireAuthorization("AdminOnly");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Users}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
