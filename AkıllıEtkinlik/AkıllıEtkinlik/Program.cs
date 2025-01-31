using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string'i appsettings.json dosyasýndan alýyoruz
var connectionString = builder.Configuration.GetConnectionString("AkýllýEtkinlikDb");

// DbContext sýnýfýný dependency injection (baðýmlýlýk enjeksiyonu) ile ekleyin
builder.Services.AddDbContext<AkýllýEtkinlikDbContext>(options =>
    options.UseSqlServer(connectionString));

// Yetkilendirme servisini ekleyin
builder.Services.AddAuthorization();

// Session desteðini ekleyin
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturumun süresi 30 dakika
    options.Cookie.HttpOnly = true; // Güvenlik için sadece HTTP üzerinden eriþim
    options.Cookie.IsEssential = true; // Oturum çerezi önemli olarak iþaretlendi
});

// Controller ve View desteðini ekleyin
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Uygulama yapýlandýrmalarý
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Kimlik doðrulama ve yetkilendirme middleware'lerini ekleyin
app.UseAuthentication();
app.UseAuthorization();

// Session middleware'ini ekleyin
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
