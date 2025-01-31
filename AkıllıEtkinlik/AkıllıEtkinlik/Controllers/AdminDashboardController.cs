using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using System.Linq;
using AkıllıEtkinlik.Models;

using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;
using System.Globalization;
using AkilliEtkinlik.Models;


namespace AkıllıEtkinlik.Controllers
{
    public class AdminDashboardController : Controller
    {

        private readonly AkıllıEtkinlikDbContext _context;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(AkıllıEtkinlikDbContext context, ILogger<AdminDashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult AdminDashboard()
        {
            return View();
        }

        public IActionResult EtkinlikOnay()
        {

            var bekleyenEtkinlikler = _context.Etkinlikler
                .Include(e => e.Kullanıcı) 
                .Where(e => e.Onay == false)
                .ToList();

            return View(bekleyenEtkinlikler);
        }

        [HttpPost]
        public IActionResult EtkinlikOnayla(int etkinlikID)
        {
            // Belirtilen etkinliği bul
            var etkinlik = _context.Etkinlikler.FirstOrDefault(e => e.EtkinlikID == etkinlikID);

            if (etkinlik != null)
            {
                // Etkinliği onayla (Onay / true)
                etkinlik.Onay = true;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Etkinlik başarıyla onaylandı.";
            }
            else
            {
                TempData["ErrorMessage"] = "Etkinlik bulunamadı.";
            }


            return RedirectToAction("EtkinlikOnay");
        }


        [HttpPost]
        public IActionResult EtkinlikSil(int etkinlikID)
        {

            var etkinlik = _context.Etkinlikler.FirstOrDefault(e => e.EtkinlikID == etkinlikID);

            if (etkinlik != null)
            {
                // Etkinliği veritabanından sil
                _context.Etkinlikler.Remove(etkinlik);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Etkinlik başarıyla kaldırıldı.";
            }
            else
            {
                TempData["ErrorMessage"] = "Silinmek istenen etkinlik bulunamadı.";
            }


            return RedirectToAction("EtkinlikOnay");
        }



        [HttpGet]
        public IActionResult Geribildirim()
        {
            var geriBildirimler = _context.GeriBildirim
                .Include(g => g.Kullanıcı) 
                .Include(g => g.Etkinlik) 
                .ToList();

            return View(geriBildirimler);
        }


        [HttpPost]
        public IActionResult GeriBildirimSil(int geriBildirimID)
        {
            var geriBildirim = _context.GeriBildirim.FirstOrDefault(g => g.GeriBildirimID == geriBildirimID);

            if (geriBildirim != null)
            {
                _context.GeriBildirim.Remove(geriBildirim);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Geri bildirim başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Geri bildirim bulunamadı.";
            }

            return RedirectToAction("Geribildirim");
        }


        [HttpGet]
        public IActionResult Rapor()
        {
            return View();
        }


        [HttpGet]
        public JsonResult GetReportData()
        {
            try
            {
                // Kullanıcı 
                var totalUsers = _context.Kullanıcılar.Count();
                Console.WriteLine($"Total Users: {totalUsers}");

                // Aktif kullanıcı
                var activeUsers = _context.Kullanıcılar
                    .Where(k => _context.Katılımcılar.Any(kat => kat.KullanıcıID == k.KullanıcıID) ||
                                _context.Etkinlikler.Any(et => et.KullanıcıID == k.KullanıcıID))
                    .Count();
                Console.WriteLine($"Active Users: {activeUsers}");

                // Onaylanmış etkinlik
                var approvedEvents = _context.Etkinlikler.Count(e => e.Onay);
                var pendingEvents = _context.Etkinlikler.Count(e => !e.Onay);
                Console.WriteLine($"Approved Events: {approvedEvents}, Pending Events: {pendingEvents}");

                // Kategori baz
                var eventCategories = _context.Etkinlikler
                    .GroupBy(e => e.Kategori ?? "Bilinmiyor")
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToList();
                Console.WriteLine("Event Categories:");
                foreach (var category in eventCategories)
                {
                    Console.WriteLine($"Category: {category.Category}, Count: {category.Count}");
                }

                // Popüler etkinlikler
                var popularEvents = _context.Katılımcılar
                    .GroupBy(k => k.EtkinlikID)
                    .Select(g => new
                    {
                        EventName = _context.Etkinlikler
                            .Where(e => e.EtkinlikID == g.Key)
                            .Select(e => e.EtkinlikAdı)
                            .FirstOrDefault() ?? "Bilinmiyor",
                        Count = g.Count()
                    })
                    .OrderByDescending(e => e.Count)
                    .Take(5)
                    .ToList();
                Console.WriteLine("Popular Events Retrieved");
                foreach (var evt in popularEvents)
                {
                    Console.WriteLine($"EventName: {evt.EventName}, Count: {evt.Count}");
                }


                // En yüksek puanlı kullanıcılar
                var topUsers = _context.Puanlar
                    .GroupBy(p => p.KullanıcıID)
                    .Select(g => new
                    {
                        UserName = _context.Kullanıcılar
                            .Where(u => u.KullanıcıID == g.Key)
                            .Select(u => u.KullanıcıAdı)
                            .FirstOrDefault() ?? "Bilinmiyor",
                        TotalPoints = g.Sum(p => p.PuanDeğeri)
                    })
                    .OrderByDescending(u => u.TotalPoints)
                    .Take(5)
                    .ToList();
                Console.WriteLine("Top Users Retrieved");
                foreach (var user in topUsers)
                {
                    Console.WriteLine($"UserName: {user.UserName}, TotalPoints: {user.TotalPoints}");
                }


                // Json çıktısı 
                return Json(new
                {
                    totalUsers,
                    activeUsersRatio = totalUsers > 0 ? (activeUsers * 100) / totalUsers : 0,
                    approvedEvents,
                    pendingEvents,
                    eventCategories,
                    popularEvents,
                    topUsers
                });
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Hata: {ex.Message}");
                return Json(new { error = "Bir hata oluştu. Detaylar loglarda." });
            }
        }


        [HttpPost]
        public IActionResult EtkinlikSilme(int EtkinlikID, bool adminOnay = false)
        {

            var etkinlik = _context.Etkinlikler.FirstOrDefault(e => e.EtkinlikID == EtkinlikID);
            var katilimcilar = _context.Katılımcılar.Where(k => k.EtkinlikID == EtkinlikID).ToList();

            if (etkinlik != null)
            {
                if (katilimcilar.Any())
                {
                    if (!adminOnay)
                    {

                        TempData["SilmeHatasi"] = "Bu etkinliğin katılımcıları bulunmaktadır. Eğer silmek istiyorsanız onay veriniz.";
                        TempData["SilmeOnay"] = true; 
                        TempData["EtkinlikID"] = EtkinlikID; 
                        return RedirectToAction("EtkinlikEkleme"); 
                    }

                    // Admin onayladıysa  katılımcıları sil
                    _context.Katılımcılar.RemoveRange(katilimcilar);
                }

                // Etkinliği sil
                _context.Etkinlikler.Remove(etkinlik);
                _context.SaveChanges();
            }

            TempData["SilmeBasarili"] = "Etkinlik ve katılımcıları başarıyla silindi.";
            return RedirectToAction("EtkinlikEkleme");
        }





        [HttpGet]
        public IActionResult DownloadReport()
        {
            var pdfContent = GeneratePdf();
            return File(pdfContent, "application/pdf", "Rapor.pdf");
        }

        private byte[] GeneratePdf()
        {
            using (var stream = new MemoryStream())
            {
                var document = new PdfDocument();
                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);

         
                var titleFont = new XFont("Verdana", 20, XFontStyle.Bold);
                var contentFont = new XFont("Verdana", 14, XFontStyle.Regular);


                gfx.DrawString("Raporlama Paneli", titleFont, XBrushes.Black, new XRect(0, 0, page.Width, 50), XStringFormats.TopCenter);

                // Genel Bilgiler
                gfx.DrawString($"Toplam Kullanıcı Sayısı: {_context.Kullanıcılar.Count()}", contentFont, XBrushes.Black, new XRect(40, 80, page.Width - 80, 20), XStringFormats.TopLeft);
                gfx.DrawString($"Onaylanmış Etkinlikler: {_context.Etkinlikler.Count(e => e.Onay)}", contentFont, XBrushes.Black, new XRect(40, 120, page.Width - 80, 20), XStringFormats.TopLeft);
                gfx.DrawString($"Onay Bekleyen Etkinlikler: {_context.Etkinlikler.Count(e => !e.Onay)}", contentFont, XBrushes.Black, new XRect(40, 160, page.Width - 80, 20), XStringFormats.TopLeft);


                gfx.DrawString("En Yüksek Puanlı Kullanıcılar:", contentFont, XBrushes.Black, new XRect(40, 200, page.Width - 80, 20), XStringFormats.TopLeft);
                int startY = 240;
                var topUsers = _context.Puanlar
                    .GroupBy(p => p.KullanıcıID)
                    .Select(g => new
                    {
                        UserName = _context.Kullanıcılar
                            .Where(u => u.KullanıcıID == g.Key)
                            .Select(u => u.KullanıcıAdı)
                            .FirstOrDefault() ?? "Bilinmiyor",
                        TotalPoints = g.Sum(p => p.PuanDeğeri)
                    })
                    .OrderByDescending(u => u.TotalPoints)
                    .Take(5)
                    .ToList();

                foreach (var user in topUsers)
                {
                    gfx.DrawString($"{user.UserName}: {user.TotalPoints} puan", contentFont, XBrushes.Black, new XRect(40, startY, page.Width - 80, 20), XStringFormats.TopLeft);
                    startY += 30; 
                }

                // Pdf kaydet
                document.Save(stream, false);
                return stream.ToArray();
            }
        }


        public IActionResult EtkinlikEkleme()
        {
            var etkinlikler = _context.Etkinlikler.ToList();
            return View(etkinlikler ?? new List<Etkinlik>());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EtkinlikEkle(Etkinlik yeniEtkinlik, IFormFile EtkinlikResmi)
        {
            try
            {
         
                if (EtkinlikResmi != null && EtkinlikResmi.Length > 0)
                {

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(EtkinlikResmi.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["ErrorMessage"] = "Geçersiz dosya formatı. Sadece jpg, jpeg, png veya gif yüklenebilir.";
                        return View("EtkinlikEkleme");
                    }


                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                    // Klasör yoksa oluştur
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    var filePath = Path.Combine("/uploads", fileName);
                    var fullPath = Path.Combine(uploads, fileName);


                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await EtkinlikResmi.CopyToAsync(stream);
                    }

                    yeniEtkinlik.ImagePath = filePath;
                }


                if (yeniEtkinlik.Latitude == null || yeniEtkinlik.Longitude == null)
                {
                    throw new ArgumentException("Latitude ve Longitude değerleri null olamaz.");
                }

                if (yeniEtkinlik.Latitude > 1000000 && yeniEtkinlik.Longitude > 1000000)
                {
                    yeniEtkinlik.Latitude = ConvertToDecimal((int)yeniEtkinlik.Latitude);
                    yeniEtkinlik.Longitude = ConvertToDecimal((int)yeniEtkinlik.Longitude);
                }
                else
                {

                    yeniEtkinlik.Latitude ??= 41.0082m;
                    yeniEtkinlik.Longitude ??= 28.9784m;
                }

               
                yeniEtkinlik.KullanıcıID = 1;
                yeniEtkinlik.Onay = true;

                // Yeni etkinliği veritabanına kaydet
                _context.Etkinlikler.Add(yeniEtkinlik);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Etkinlik başarıyla eklendi!";
                return RedirectToAction("EtkinlikEkleme");
            }
            catch (Exception ex)
            {
                // Hata günlüğü ve mesajı
                _logger.LogError(ex, "Etkinlik eklerken bir hata oluştu.");
                TempData["ErrorMessage"] = "Etkinlik eklerken bir hata oluştu: " + ex.Message;
                return View("EtkinlikEkleme");
            }
        }

        // Latitude ve Longitude tam sayıdan ondalıklı değere dönüştürme fonksiyonu
        private decimal ConvertToDecimal(int value)
        {
            string valueStr = value.ToString();

            if (valueStr.Length < 3)
            {
                throw new ArgumentException("Değer, en az 3 basamaklı bir sayı olmalıdır.");
            }

            string integerPart = valueStr.Substring(0, 2); 
            string decimalPart = valueStr.Substring(2);    

            if (!int.TryParse(integerPart, out _) || !int.TryParse(decimalPart, out _))
            {
                throw new FormatException("Değer uygun bir formatta değil.");
            }

            return decimal.Parse($"{integerPart}.{decimalPart}", CultureInfo.InvariantCulture);
        }



        public IActionResult EtkinlikDetayAD(int id)
        {
            var etkinlik = _context.Etkinlikler.FirstOrDefault(e => e.EtkinlikID == id);
            if (etkinlik == null)
            {
                return NotFound();
            }

            etkinlik.Latitude ??= 41.0082m; 
            etkinlik.Longitude ??= 28.9784m; 

            return View(etkinlik);
        }







        // Kullanıcı Silme
        [HttpPost]
        public IActionResult KullanıcıSil(int id)
        {
            var user = _context.Kullanıcılar.FirstOrDefault(u => u.KullanıcıID == id);
            if (user != null)
            {
                _context.Kullanıcılar.Remove(user);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
            }

            return RedirectToAction("KullanıcıYönetimi");
        }




        public IActionResult KullanıcıYönetimi()
        {
            var kullanıcılar = _context.Kullanıcılar.ToList();



            return View(kullanıcılar);
        }





        // Kullanıcı Güncelleme
        [HttpPost]
        public async Task<IActionResult> KullanıcıGüncelleAsync(Kullanıcı model, IFormFile ProfilFotografi)
        {
            var user = await _context.Kullanıcılar.FirstOrDefaultAsync(u => u.KullanıcıID == model.KullanıcıID);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("KullanıcıYönetimi");
            }

            Console.WriteLine($"Latitude: {user.Latitude}, Longitude: {user.Longitude}");


            if (model.Latitude > 1000000 && model.Longitude > 1000000)
            {
                model.Latitude = ConvertToDecimal((int)model.Latitude);
                model.Longitude = ConvertToDecimal((int)model.Longitude);
            }


            ViewData["Latitude"] = user.Latitude ?? 41.0082m;
            ViewData["Longitude"] = user.Longitude ?? 28.9784m;

            // Bilgileri güncelle
            user.KullanıcıAdı = model.KullanıcıAdı;
            user.Ad = model.Ad;
            user.Soyad = model.Soyad;
            user.Eposta = model.Eposta;
            user.TelefonNumarasi = model.TelefonNumarasi;
            user.Konum = model.Konum;
            user.IlgiAlanlari = model.IlgiAlanlari;
            user.Latitude = model.Latitude;
            user.Longitude = model.Longitude;



            // Profil fotoğrafı yükleme işlemi
            if (ProfilFotografi != null && ProfilFotografi.Length > 0)
            {
                var fileName = Path.GetFileName(ProfilFotografi.FileName);
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }
                var fullPath = Path.Combine(uploads, fileName);
                var relativePath = $"/uploads/{fileName}";
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await ProfilFotografi.CopyToAsync(stream);
                }
                user.ProfilFotografi = relativePath;
            }



            if (!string.IsNullOrWhiteSpace(model.Sifre))
            {
                user.Sifre = HashHelper.HashPassword(model.Sifre); // Şifreyi hashleme
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Kullanıcı bilgileri başarıyla güncellendi.";
            return RedirectToAction("KullanıcıYönetimi");
        }


        [HttpGet]
        public IActionResult Mesaj(int etkinlikID)
        {

            var etkinlikler = _context.Etkinlikler.ToList();


            var mesajlar = _context.Mesajlar
                .Where(m => m.EtkinlikID == etkinlikID)
                .Include(m => m.Gonderici)
                .OrderBy(m => m.GonderimZamani)
                .ToList();

            ViewBag.Etkinlikler = etkinlikler;
            ViewBag.SeciliEtkinlik = _context.Etkinlikler.FirstOrDefault(e => e.EtkinlikID == etkinlikID);
            ViewBag.Mesajlar = mesajlar;

            return View("Mesaj"); 
        }


        [HttpPost]
        public IActionResult AdminMesajGonder(int etkinlikID, string mesajMetni)
        {
  
            const int adminID = 1;

            // Yeni mesaj oluştur ve kaydet
            var yeniMesaj = new Mesaj
            {
                GondericiID = adminID,
                EtkinlikID = etkinlikID,
                MesajMetni = mesajMetni,
                GonderimZamani = DateTime.Now
            };

            _context.Mesajlar.Add(yeniMesaj);
            _context.SaveChanges();

            return RedirectToAction("Mesaj", new { etkinlikID });
        }



    }
}
