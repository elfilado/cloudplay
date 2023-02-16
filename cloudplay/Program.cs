using Microsoft.EntityFrameworkCore;
using cloudplay.Data;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<cloudplayContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("cloudplayContext") ?? throw new InvalidOperationException("Connection string 'cloudplayContext' not found.")));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
