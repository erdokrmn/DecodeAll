using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// MVC servisi ekliyoruz
builder.Services.AddControllersWithViews();

// Upload limit ayarý
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

var app = builder.Build();

// Static dosyalar
app.UseStaticFiles();

// Route doðrudan buraya
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Decode}/{action=Index}/{id?}");

app.Run();
