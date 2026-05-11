var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/home/error");

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
