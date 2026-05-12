using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using PatientPortalLite.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PatientPortalDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("PatientPortal")
                  ?? "Data Source=patientportal.db"));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/auth/login";
        options.LogoutPath       = "/auth/logout";
        options.AccessDeniedPath = "/auth/login";
        options.ExpireTimeSpan   = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.Name      = "patientportal.auth";
    });

builder.Services.AddAuthorization(opt =>
{
    opt.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(10);
    o.Cookie.IsEssential = true;
    o.Cookie.Name = "patientportal.session";
});

builder.Services.AddControllersWithViews(opt =>
{
    opt.Filters.Add(new AuthorizeFilter());
});
builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PatientPortalDbContext>();
    await DbSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
