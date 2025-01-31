using AkıllıEtkinlik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;

namespace AkıllıEtkinlik.Controllers
{
    public class HomeController : Controller
    {
        private readonly AkıllıEtkinlikDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(AkıllıEtkinlikDbContext context, ILogger<HomeController> logger , IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }


 


        public IActionResult Login()
        {
            return View();
        }


      



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        private decimal ConvertToDecimal(int value)
        {
            string valueStr = value.ToString();


            if (valueStr.Length < 3)
            {
                throw new ArgumentException("Değer, en az 3 basamaklı bir sayı olmalıdır.");
            }

        
            string integerPart = valueStr.Substring(0, 2); 
            string decimalPart = valueStr.Substring(2); 

            // Tam ve ondalık kısmı birleştirip decimal olarak döndürüyoruz
            string result = $"{integerPart}.{decimalPart}";


            return decimal.Parse(result, System.Globalization.CultureInfo.InvariantCulture);
        }


        [HttpPost]
        public async Task<IActionResult> kayitOl(Kullanıcı model, IFormFile ProfilFotografi)
        {
            try
            {

                if (model.Latitude > 1000000 && model.Longitude > 1000000)
                {
                    model.Latitude = ConvertToDecimal((int)model.Latitude);
                    model.Longitude = ConvertToDecimal((int)model.Longitude);
                }


                _logger.LogInformation($"Converted Latitude: {model.Latitude}");
                _logger.LogInformation($"Converted Longitude: {model.Longitude}");
                Console.WriteLine($"Converted Latitude: {model.Latitude}, Converted Longitude: {model.Longitude}");




                model.Sifre = HashHelper.HashPassword(model.Sifre);
                string? filePath = null;

                // Profil fotoğrafı yükleme işlemi
                if (ProfilFotografi != null && ProfilFotografi.Length > 0)
                {
                    var fileName = Path.GetFileName(ProfilFotografi.FileName);
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    filePath = Path.Combine("/uploads", fileName);
                    var fullPath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await ProfilFotografi.CopyToAsync(stream);
                    }
                }

                // Kullanıcıyı veritabanına ekleme
                string sql = @"
        INSERT INTO Kullanıcılar (KullanıcıAdı, Şifre, Eposta, Konum, İlgiAlanları, Ad, Soyad, DoğumTarihi, Cinsiyet, TelefonNumarası, ProfilFotoğrafı, Latitude, Longitude)
        VALUES (@KullanıcıAdı, @Şifre, @Eposta, @Konum, @IlgiAlanları, @Ad, @Soyad, @DoğumTarihi, @Cinsiyet, @TelefonNumarası, @ProfilFotoğrafı, @Latitude, @Longitude)";

                await _context.Database.ExecuteSqlRawAsync(sql,
                    new SqlParameter("@KullanıcıAdı", model.KullanıcıAdı ?? (object)DBNull.Value),
                    new SqlParameter("@Şifre", model.Sifre ?? (object)DBNull.Value),
                    new SqlParameter("@Eposta", model.Eposta ?? (object)DBNull.Value),
                    new SqlParameter("@Konum", model.Konum ?? (object)DBNull.Value),
                    new SqlParameter("@IlgiAlanları", model.IlgiAlanlari ?? (object)DBNull.Value),
                    new SqlParameter("@Ad", model.Ad ?? (object)DBNull.Value),
                    new SqlParameter("@Soyad", model.Soyad ?? (object)DBNull.Value),
                    new SqlParameter("@DoğumTarihi", model.DogumTarihi ?? (object)DBNull.Value),
                    new SqlParameter("@Cinsiyet", model.Cinsiyet ?? (object)DBNull.Value),
                    new SqlParameter("@TelefonNumarası", model.TelefonNumarasi ?? (object)DBNull.Value),
                    new SqlParameter("@ProfilFotoğrafı", filePath ?? (object)DBNull.Value),
                    new SqlParameter("@Latitude", model.Latitude.HasValue ? model.Latitude.Value : (object)DBNull.Value),
                    new SqlParameter("@Longitude", model.Longitude.HasValue ? model.Longitude.Value : (object)DBNull.Value));

                var yeniKullanıcı = await _context.Kullanıcılar.FirstOrDefaultAsync(u => u.KullanıcıAdı == model.KullanıcıAdı);

                if (yeniKullanıcı != null)
                {
                  
                    string puanSql = @"
            INSERT INTO Puanlar (KullanıcıID, PuanDeğeri, KazanılanTarih)
            VALUES (@KullanıcıID, @PuanDeğeri, @KazanılanTarih)";

                    await _context.Database.ExecuteSqlRawAsync(puanSql,
                        new SqlParameter("@KullanıcıID", yeniKullanıcı.KullanıcıID),
                        new SqlParameter("@PuanDeğeri", 20), 
                        new SqlParameter("@KazanılanTarih", DateTime.Now));
                }

                
                TempData["SuccessMessage"] = "Kayıt başarılı! İlk katılım bonus puanınız hesabınıza eklendi. Lütfen giriş yapın.";
            }
            catch (Exception ex)
            {
                
                TempData["ErrorMessage"] = "Kayıt sırasında bir hata oluştu: " + ex.Message;
            }

           
            return RedirectToAction("Login", "Home");
        }







        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                
                var user = await _context.Kullanıcılar.FirstOrDefaultAsync(u => u.KullanıcıAdı == username);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                }

                // Şifre doğrulama
                bool isPasswordValid = HashHelper.VerifyPassword(password, user.Sifre);

                if (!isPasswordValid)
                {
                    TempData["ErrorMessage"] = "Şifre yanlış.";
                    return Json(new { success = false, message = "Şifre yanlış." });
                }

                
                HttpContext.Session.SetInt32("KullanıcıID", user.KullanıcıID);

                
                return Json(new { success = true, redirectUrl = "/UserDashboard/UserDashboard" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Giriş sırasında bir hata oluştu: " + ex.Message;
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> AdminLogin(string username, string password)
        {
            try
            {
                // Admin tablosundan kullanıcıyı sorgula
                var admin = await _context.AdminPanel.FirstOrDefaultAsync(a => a.KullaniciAdi == username);

                if (admin == null)
                {
                    TempData["ErrorMessage"] = "Admin bulunamadı.";
                    return Json(new { success = false, message = "Admin bulunamadı." });
                }

                // Şifre doğrulama 
                bool isPasswordValid = HashHelper.VerifyPassword(password, admin.Sifre);

                if (!isPasswordValid)
                {
                    TempData["ErrorMessage"] = "Şifre yanlış.";
                    return Json(new { success = false, message = "Şifre yanlış." });
                }

                // Admin bilgileri
                HttpContext.Session.SetInt32("AdminID", admin.ID);

                
                return Json(new { success = true, redirectUrl = "/AdminDashboard/AdminDashboard" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Giriş sırasında bir hata oluştu: " + ex.Message;
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }


        // Şifre sıfırlama 
        private async Task SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                
                var smtpHost = _configuration["EmailSettings:Host"];
                var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
                var smtpUsername = _configuration["EmailSettings:Username"];
                var smtpPassword = _configuration["EmailSettings:Password"];

                using (var smtpClient = new SmtpClient(smtpHost))
                {
                    smtpClient.Port = smtpPort;
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpUsername),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false,
                    };
                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"E-posta gönderim hatası: {ex.Message}");
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] Dictionary<string, string> request)
        {
            if (request == null || !request.ContainsKey("email"))
            {
                return Json(new { success = false, message = "E-posta adresi sağlanamadı." });
            }

            string email = request["email"];
            var user = await _context.Kullanıcılar.FirstOrDefaultAsync(u => u.Eposta == email);

            if (user == null)
            {
                return Json(new { success = false, message = "Bu e-posta adresiyle kayıtlı kullanıcı bulunamadı." });
            }

            // Token oluşr
            string token = Guid.NewGuid().ToString();

            // Token ve süresi
            user.ResetPasswordToken = token;
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            // Şifre sıfırlama linki
            string resetLink = Url.Action("ResetPassword", "Home", new { token }, Request.Scheme);



            // E-posta gönderimi
            string subject = "Şifre Sıfırlama Talebi";
            string body = $"Merhaba {user.Ad},\n\nŞifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:\n\n{resetLink}\n\nBu bağlantı 1 saat boyunca geçerlidir.";
            await SendEmail(email, subject, body);

            return Json(new { success = true, message = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi." });
        }


        [HttpGet]
        [Route("ResetPassword")]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Geçersiz veya eksik token.";
                return RedirectToAction("Login", "Home");
            }

            // Tokenı doğrula
            var user = _context.Kullanıcılar.FirstOrDefault(u => u.ResetPasswordToken == token && u.ResetPasswordTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Token geçersiz veya süresi dolmuş.";
                return RedirectToAction("Login", "Home");
            }


            return View(new ResetPasswordViewModel { Token = token });
        }


        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _context.Kullanıcılar.FirstOrDefault(u => u.ResetPasswordToken == model.Token && u.ResetPasswordTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Token geçersiz veya süresi dolmuş.";
                return RedirectToAction("Login", "Home");
            }

            // Yeni şifreyi hashleyip kaydet
            user.Sifre = HashHelper.HashPassword(model.NewPassword);
            user.ResetPasswordToken = null; // tokenı sıfırla
            user.ResetPasswordTokenExpiry = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Şifre başarıyla sıfırlandı. Giriş yapabilirsiniz.";
            return RedirectToAction("Login", "Home");
        }




    }
}
