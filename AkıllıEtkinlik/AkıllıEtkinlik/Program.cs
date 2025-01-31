using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string'i appsettings.json dosyas�ndan al�yoruz
var connectionString = builder.Configuration.GetConnectionString("Ak�ll�EtkinlikDb");

// DbContext s�n�f�n� dependency injection (ba��ml�l�k enjeksiyonu) ile ekleyin
builder.Services.AddDbContext<Ak�ll�EtkinlikDbContext>(options =>
    options.UseSqlServer(connectionString));

// Yetkilendirme servisini ekleyin
builder.Services.AddAuthorization();

// Session deste�ini ekleyin
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturumun s�resi 30 dakika
    options.Cookie.HttpOnly = true; // G�venlik i�in sadece HTTP �zerinden eri�im
    options.Cookie.IsEssential = true; // Oturum �erezi �nemli olarak i�aretlendi
});

// Controller ve View deste�ini ekleyin
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Uygulama yap�land�rmalar�
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Kimlik do�rulama ve yetkilendirme middleware'lerini ekleyin
app.UseAuthentication();
app.UseAuthorization();

// Session middleware'ini ekleyin
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
