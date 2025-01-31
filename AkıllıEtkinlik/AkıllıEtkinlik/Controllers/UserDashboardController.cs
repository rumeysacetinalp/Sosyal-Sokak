using AkilliEtkinlik.Models;

using AkıllıEtkinlik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AkıllıEtkinlik.Controllers
{
    public class UserDashboardController : Controller
    {


        private readonly AkıllıEtkinlikDbContext _context;

        public UserDashboardController(AkıllıEtkinlikDbContext context)
        {
            _context = context;
        }



        public IActionResult UserDashboard()
        {
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");

            if (kullanıcıID == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı oturum bilgisi bulunamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Home");
            }



            // Kullanıcıyı veritabanından bul
            var kullanıcı = _context.Kullanıcılar.FirstOrDefault(k => k.KullanıcıID == kullanıcıID);
            if (kullanıcı == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }



            ViewBag.KullanıcıLatitude = kullanıcı.Latitude ?? 41.0082m; 
            ViewBag.KullanıcıLongitude = kullanıcı.Longitude ?? 28.9784m; 


            // Kullanıcının konum bilgisi
            var userCity = _context.Kullanıcılar
                .Where(u => u.KullanıcıID == kullanıcıID.Value)
                .Select(u => u.Konum)
                .FirstOrDefault();

            // Kullanıcının ilgi alanlarını al
            var kullaniciIlgiAlanlari = _context.Kullanıcılar
                .Where(u => u.KullanıcıID == kullanıcıID.Value)
                .Select(u => u.IlgiAlanlari)
                .FirstOrDefault();

            // Etkinlikleri veritabanından listele (Sadece Onay // true olanlar)
            var etkinlikler = _context.Etkinlikler
                .Where(e => e.Onay == true && e.Tarih >= DateTime.Today)
                .ToList();




           
            var ilgiAlanlariListesi = LoadIlgiAlanlari(); 

         
            var kullaniciIlgiAlanlariList = kullaniciIlgiAlanlari?.Split(',').ToList() ?? new List<string>();

            // Kullanıcının katıldığı etkinlikleri alıyoruz
            var kullaniciKatildigiEtkinlikler = _context.Katılımcılar
                .Where(k => k.KullanıcıID == kullanıcıID.Value)
                .Select(k => k.EtkinlikID)
                .ToList();

            // Kullanıcının her katıldığı etkinlik için kategori bazında puan belirleme
            var kategoriPuanlari = new Dictionary<string, int>();

            foreach (var etkinlikID in kullaniciKatildigiEtkinlikler)
            {
                var etkinlik = _context.Etkinlikler
                    .FirstOrDefault(e => e.EtkinlikID == etkinlikID);

                if (etkinlik != null)
                {
                    if (!kategoriPuanlari.ContainsKey(etkinlik.Kategori))
                    {
                        kategoriPuanlari[etkinlik.Kategori] = 0; 
                    }

                    kategoriPuanlari[etkinlik.Kategori] += 5; 
                }
            }

            // Etkinlikleri ilgi alanlarına ve katılım geçmişine göre puanla ve sıralayarak getir
            var onerilenEtkinlikler = etkinlikler
                .Select(etkinlik =>
                {
                    int puan = 0;
                    double mesafe = double.MaxValue;

                    // İlgi Alanı Uyum Kuralı
                    foreach (var kategoriIlgi in ilgiAlanlariListesi)
                    {
                        if (etkinlik.Kategori.Equals(kategoriIlgi.Kategori, StringComparison.OrdinalIgnoreCase))
                        {
                            if (kategoriIlgi.IlgiAlanlari.Any(ilgi =>
                                kullaniciIlgiAlanlariList.Any(kullaniciIlgi =>
                                    ilgi.IndexOf(kullaniciIlgi, StringComparison.OrdinalIgnoreCase) >= 0)))
                            {
                                puan += 100;
                            }
                        }
                    }

                    // Katılım Geçmişi Kuralı
                    if (kategoriPuanlari.ContainsKey(etkinlik.Kategori))
                    {
                        puan += kategoriPuanlari[etkinlik.Kategori];
                    }

                    // Coğrafi Konum Mesafe Hesabı
                    if (etkinlik.Latitude.HasValue && etkinlik.Longitude.HasValue)
                    {
                        var kullanici = _context.Kullanıcılar.FirstOrDefault(u => u.KullanıcıID == kullanıcıID.Value);
                        if (kullanici != null && kullanici.Latitude.HasValue && kullanici.Longitude.HasValue)
                        {
                            mesafe = CalculateDistance(
                                (double)kullanici.Latitude.Value,
                                (double)kullanici.Longitude.Value,
                                (double)etkinlik.Latitude.Value,
                                (double)etkinlik.Longitude.Value);
                        }
                    }

                    etkinlik.Puan = puan;
                    etkinlik.Mesafe = mesafe; // Mesafeyi etkinliğe ata
                    return etkinlik;
                })
                .OrderByDescending(e => e.Puan) // Puan sıralaması
                .ThenBy(e => e.Mesafe)         // Eşit puanlı etkinlikleri mesafeye göre sıralama
                .ToList();


            // Veritabanındaki mevcut şehirleri alın
            var cities = _context.Etkinlikler
                .Where(e => e.Onay == true && e.Tarih >= DateTime.Today) // Sadece gelecekteki etkinliklerden şehirler
                .Select(e => e.Konum)
                .Distinct()
                .ToList();





            ViewBag.UserCity = userCity; // Kullanıcının konum bilgisi
            ViewBag.Cities = cities; // Mevcut şehirlerin listesi


            ViewBag.EventsJson = JsonConvert.SerializeObject(etkinlikler);



            return View(onerilenEtkinlikler);
        }



        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Dünya yarıçapı

            var lat = (lat2 - lat1) * (Math.PI / 180);
            var lon = (lon2 - lon1) * (Math.PI / 180);

            var a = Math.Sin(lat / 2) * Math.Sin(lat / 2) +
                    Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                    Math.Sin(lon / 2) * Math.Sin(lon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Mesafeyi kilometre olarak döndür
        }



        private List<KategoriIlgi> LoadIlgiAlanlari()
        {
            var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "ilgiAlanlari.json");
            var jsonData = System.IO.File.ReadAllText(jsonFilePath);

           
            var ilgiAlanlariListesi = JsonConvert.DeserializeObject<Dictionary<string, List<KategoriIlgi>>>(jsonData);

           
            return ilgiAlanlariListesi["ilgiAlanlari"];
        }

        private List<Etkinlik> GetRecommendedEvents(List<Etkinlik> etkinlikler, List<KategoriIlgi> ilgiAlanlariListesi, List<string> kullaniciIlgiAlanlari)
        {
            var recommendedEvents = etkinlikler.Select(etkinlik =>
            {
                int puan = 0;

                // JSON'daki kategorilerle karşılaştırma yap
                foreach (var kategoriIlgi in ilgiAlanlariListesi)
                {
                    if (etkinlik.Kategori.Equals(kategoriIlgi.Kategori, StringComparison.OrdinalIgnoreCase))
                    {
                        // Kullanıcının ilgisini içeren etkinlikler
                        if (kategoriIlgi.IlgiAlanlari.Any(ilgi => kullaniciIlgiAlanlari.Contains(ilgi)))
                        {
                            puan += 10; 
                        }
                    }
                }

                etkinlik.Puan = puan;
                return etkinlik;
            })
            .OrderByDescending(e => e.Puan) // Puanlara göre sıralayın
            .ToList();

            return recommendedEvents;
        }

        public IActionResult SosyalSokakEn()
        {
            // Kullanıcıları ve toplam puanlarını çek
            var kullanıcılar = _context.Kullanıcılar
                .Select(k => new
                {
                    k.KullanıcıID,
                    k.Ad,
                    k.Soyad,
                    k.ProfilFotografi, // Profil fotoğrafı varsa
                    ToplamPuan = _context.Puanlar
                        .Where(p => p.KullanıcıID == k.KullanıcıID)
                        .Sum(p => p.PuanDeğeri) // Kullanıcının toplam puanı
                })
                .OrderByDescending(k => k.ToplamPuan) // Puana göre sırala
                .Take(10) // İlk 10 kullanıcıyı al
                .ToList();

          
            ViewData["Kullanıcılar"] = kullanıcılar;

            return View();
        }


        public IActionResult Profil()
        {
            // Oturumdan kullanıcının ID'sini al
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");

            if (kullanıcıID == null)
            {
                // Eğer kullanıcı giriş yapmadıysa, login sayfasına yönlendir
                TempData["ErrorMessage"] = "Kullanıcı oturum bilgisi bulunamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Home");
            }

            // Kullanıcı bilgilerini çek
            var user = _context.Kullanıcılar.FirstOrDefault(u => u.KullanıcıID == kullanıcıID);

            if (user == null)
            {
                return NotFound();
            }

            // Puan hesaplaması için katılım ve oluşturma sayısı
            int etkinlikKatılımSayısı = _context.Katılımcılar.Count(k => k.KullanıcıID == kullanıcıID);
            int etkinlikOluşturmaSayısı = _context.Etkinlikler.Count(e => e.KullanıcıID == kullanıcıID);

            // Kullanıcının puanlarını topla
            int toplamPuan = _context.Puanlar
                .Where(p => p.KullanıcıID == kullanıcıID)
                .Sum(p => p.PuanDeğeri);

            var oluşturduğumEtkinlikler = _context.Etkinlikler
                .Where(e => e.KullanıcıID == kullanıcıID)
                .ToList();

           
            var katıldığımEtkinlikler = (from k in _context.Katılımcılar
                                         join e in _context.Etkinlikler on k.EtkinlikID equals e.EtkinlikID
                                         where k.KullanıcıID == kullanıcıID
                                         select e)
                             .ToList();



            ViewData["User"] = user;
            ViewData["Puan"] = toplamPuan;
            ViewData["OluşturduğumEtkinlikler"] = oluşturduğumEtkinlikler;
            ViewData["KatıldığımEtkinlikler"] = katıldığımEtkinlikler;

            ViewData["Latitude"] = user.Latitude ?? 41.0082m;
            ViewData["Longitude"] = user.Longitude ?? 28.9784m;


            return View();
        }




        [HttpPost]
        public async Task<IActionResult> UpdateProfile(Kullanıcı model, IFormFile ProfilFotografi)
        {

            // Latitude ve Longitude tam sayı olarak geldiyse, onları ondalıklı hale getirelim
            if (model.Latitude > 1000000 && model.Longitude > 1000000)
            {
                model.Latitude = ConvertToDecimal((int)model.Latitude);
                model.Longitude = ConvertToDecimal((int)model.Longitude);
            }

            // Kullanıcının ID'sini oturumdan al
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");
            if (kullanıcıID == null)
            {
                TempData["ErrorMessage"] = "Güncelleme yapılırken oturum bilgisi kayboldu. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Home");
            }

            // Kullanıcıyı veritabanından bul
            var user = _context.Kullanıcılar.FirstOrDefault(u => u.KullanıcıID == kullanıcıID);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Aynı kullanıcı adına sahip başka bir kullanıcı olup olmadığını kontrol et
            var existingUser = _context.Kullanıcılar.FirstOrDefault(u => u.KullanıcıAdı == model.KullanıcıAdı && u.KullanıcıID != kullanıcıID);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Bu kullanıcı adı başka bir kullanıcı tarafından alınmış. Lütfen başka bir kullanıcı adı seçin.";
                return RedirectToAction("Profil"); // Aynı sayfaya geri dön
            }



            // Modelden gelen verilerle kullanıcı bilgilerini güncelle
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

            // Şifre güncellemesi
            if (!string.IsNullOrWhiteSpace(model.Sifre))
            {
                // Yeni şifre girilmişse, hashleme işlemi yapılır
                user.Sifre = HashHelper.HashPassword(model.Sifre); 
            }

            // Veritabanında güncellemeyi kaydedin
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profil başarıyla güncellendi.";
            return RedirectToAction("Profil");
        }


        // Etkinlik Detay Sayfası
        [HttpGet]
        public IActionResult EtkinlikKatılım(int id)
        {
            // Etkinliği bul
            var etkinlik = _context.Etkinlikler.FirstOrDefault(e => e.EtkinlikID == id);
            if (etkinlik == null)
            {
                return NotFound();
            }

            etkinlik.Latitude ??= 41.0082m; 
            etkinlik.Longitude ??= 28.9784m; 

            
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");
            if (kullanıcıID == null)
            {
                return RedirectToAction("Login", "Home"); // Kullanıcı giriş yapmamışsa giriş sayfasına yönlendir
            }

            // Kullanıcıyı veritabanından bul
            var kullanıcı = _context.Kullanıcılar.FirstOrDefault(k => k.KullanıcıID == kullanıcıID);
            if (kullanıcı == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Kullanıcının etkinliğe katılıp katılmadığını kontrol et
            bool etkinligeKatildiMi = _context.Katılımcılar.Any(k => k.KullanıcıID == kullanıcıID && k.EtkinlikID == id);

            ViewBag.EtkinligeKatildiMi = etkinligeKatildiMi; // Frontend'de kullanılacak


            // Etkinliğe ait mesajları getir
            var mesajlar = _context.Mesajlar
                .Where(m => m.EtkinlikID == id)
                .Include(m => m.Gonderici)
                .OrderBy(m => m.GonderimZamani)
                .ToList();


            // Kullanıcı konumunu ViewBag ile View'a aktar
            ViewBag.KullanıcıLatitude = kullanıcı.Latitude;
            ViewBag.KullanıcıLongitude = kullanıcı.Longitude;

            ViewBag.Mesajlar = mesajlar;
            ViewBag.EtkinlikID = id;

            return View(etkinlik);
        }


        [HttpPost]
        public IActionResult MesajGonderKatılım(int etkinlikID, string mesajMetni)
        {
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");

            // Kullanıcıyı kontrol et
            var user = _context.Kullanıcılar.FirstOrDefault(u => u.KullanıcıID == kullanıcıID);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Kullanıcının etkinliğe katılıp katılmadığını kontrol et
            bool etkinligeKatildiMi = _context.Katılımcılar.Any(k => k.KullanıcıID == kullanıcıID && k.EtkinlikID == etkinlikID);

            if (!etkinligeKatildiMi)
            {
                // Kullanıcı etkinliğe katılmamışsa mesaj gönderemez
                TempData["ErrorMessage"] = "Mesaj göndermek için etkinliğe katılmanız gerekiyor.";
                return RedirectToAction("EtkinlikKatılım", new { id = etkinlikID });
            }

            // Yeni mesaj oluştur ve kaydet
            var yeniMesaj = new Mesaj
            {
                GondericiID = kullanıcıID.Value,
                EtkinlikID = etkinlikID,
                MesajMetni = mesajMetni,
                GonderimZamani = DateTime.Now
            };

            _context.Mesajlar.Add(yeniMesaj);
            _context.SaveChanges();

            return RedirectToAction("EtkinlikKatılım", new { id = etkinlikID });
        }



        [HttpPost]
        public IActionResult GeriBildirimEkle(int etkinlikID, int puan, string yorum)
        {
            // Kullanıcı oturumu kontrol et
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");
            if (kullanıcıID == null)
            {
                return RedirectToAction("Login", "Home");
            }

            // Geri bildirimi oluştur
            var geriBildirim = new GeriBildirimler
            {
                KullanıcıID = kullanıcıID.Value,
                EtkinlikID = etkinlikID,
                Puan = puan,
                Yorum = yorum
            };

            // Veritabanına kaydet
            _context.GeriBildirim.Add(geriBildirim);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Geri bildiriminiz kaydedildi.";
            return RedirectToAction("EtkinlikKatılım", new { id = etkinlikID });
        }

        [HttpGet]
        public IActionResult GetEventParticipants(int etkinlikID)
        {
            var etkinlik = _context.Etkinlikler.Find(etkinlikID);
            if (etkinlik == null) return NotFound();

            var organizator = _context.Kullanıcılar
     .Where(k => k.KullanıcıID == etkinlik.KullanıcıID)
     .Select(k => new { Ad = k.Ad, Soyad = k.Soyad, Role = "Organizatör" })
     .FirstOrDefault();

            var katılımcılar = _context.Katılımcılar
                .Where(k => k.EtkinlikID == etkinlikID)
                .Join(_context.Kullanıcılar,
                      katılımcı => katılımcı.KullanıcıID,
                      kullanıcı => kullanıcı.KullanıcıID,
                      (katılımcı, kullanıcı) => new
                      {
                          Ad = kullanıcı.Ad,
                          Soyad = kullanıcı.Soyad,
                          Role = "Katılımcı"
                      })
                .ToList();

            Console.WriteLine($"Organizatör: {organizator?.Ad} {organizator?.Soyad}");
            foreach (var katılımcı in katılımcılar)
            {
                Console.WriteLine($"Katılımcı: {katılımcı.Ad} {katılımcı.Soyad}");
            }

            var result = new List<object> { organizator };
            result.AddRange(katılımcılar);

            return Ok(result);
        }



        public IActionResult Etkinlik()
        {
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");

            if (kullanıcıID == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı oturum bilgisi bulunamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Home");
            }

            var etkinlikler = _context.Etkinlikler
                .Where(e => e.KullanıcıID == kullanıcıID.Value)
                .ToList();

            return View(etkinlikler ?? new List<Etkinlik>());
        }






        public IActionResult EtkinlikDetay(int id)
        {
            var etkinlik = _context.Etkinlikler.FirstOrDefault(e => e.EtkinlikID == id);
            if (etkinlik == null)
            {
                return NotFound();
            }

            etkinlik.Latitude ??= 41.0082m; // İstanbul enlemi
            etkinlik.Longitude ??= 28.9784m; // İstanbul boylamı

            return View(etkinlik);
        }





        // Latitude ve Longitude değerlerini tam sayıdan ondalıklı değere dönüştüren fonksiyon
        private decimal ConvertToDecimal(int value)
        {
            string valueStr = value.ToString();

            // En az 3 basamaklı bir sayı geldiğinden emin olun
            if (valueStr.Length < 3)
            {
                throw new ArgumentException("Değer, en az 3 basamaklı bir sayı olmalıdır.");
            }

            // İlk iki basamağı tam sayı kısmı, geri kalanı ondalık kısmı olarak ayırıyoruz
            string integerPart = valueStr.Substring(0, 2); // İlk iki basamak
            string decimalPart = valueStr.Substring(2);

            // Tam ve ondalık kısmı birleştirip decimal olarak döndürüyoruz
            string result = $"{integerPart}.{decimalPart}";

            // Ondalık olarak parse edelim
            return decimal.Parse(result, System.Globalization.CultureInfo.InvariantCulture);
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EtkinlikEkle(Etkinlik yeniEtkinlik, IFormFile EtkinlikResmi)
        {
            try
            {
                string? filePath = null;

               

                // Dosya yükleme işlemi
                if (EtkinlikResmi != null && EtkinlikResmi.Length > 0)
                {
                    var fileName = Path.GetFileName(EtkinlikResmi.FileName);
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                  
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    filePath = Path.Combine("/uploads", fileName);
                    var fullPath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await EtkinlikResmi.CopyToAsync(stream);
                    }

                    // Dosya yolunu Etkinlik modelindeki ImagePath alanına atanıyor
                    yeniEtkinlik.ImagePath = filePath;
                    Console.WriteLine("Kaydedilen dosya yolu: " + filePath); // Hata ayıklama için
                }



                // Latitude ve Longitude tam sayı olarak geldiyse, onları ondalıklı hale getirelim
                if (yeniEtkinlik.Latitude > 1000000 && yeniEtkinlik.Longitude > 1000000)
                {
                    yeniEtkinlik.Latitude = ConvertToDecimal((int)yeniEtkinlik.Latitude);
                    yeniEtkinlik.Longitude = ConvertToDecimal((int)yeniEtkinlik.Longitude);
                }

                



                int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");

                if (kullanıcıID == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı oturum bilgisi bulunamadı. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Home");
                }

                // KullanıcıID'yi etkinlik modeline ata
                yeniEtkinlik.KullanıcıID = kullanıcıID.Value;

                // Veritabanına etkinliği kaydet
                _context.Etkinlikler.Add(yeniEtkinlik);
                await _context.SaveChangesAsync();

                // Kullanıcıya puan ekleme
                var yeniPuan = new Puan
                {
                    KullanıcıID = kullanıcıID.Value,
                    PuanDeğeri = 15,
                    KazanılanTarih = DateTime.Now
                };
                _context.Puanlar.Add(yeniPuan);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Etkinlik başarıyla eklendi!";
                return RedirectToAction("Etkinlik");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Etkinlik eklerken bir hata oluştu: " + ex.Message;
                return View("Etkinlik");
            }
        }





        [HttpPost]
        public async Task<IActionResult> EtkinlikGuncelle(Etkinlik etkinlik, IFormFile EtkinlikResmi)
        {
            // Latitude ve Longitude tam sayı olarak geldiyse, onları ondalıklı hale getirelim
            if (etkinlik.Latitude > 1000000 && etkinlik.Longitude > 1000000)
            {
                etkinlik.Latitude = ConvertToDecimal((int)etkinlik.Latitude);
                etkinlik.Longitude = ConvertToDecimal((int)etkinlik.Longitude);
            }

            // Veritabanında güncellenmesi gereken mevcut etkinliği bul
            var mevcutEtkinlik = _context.Etkinlikler.FirstOrDefault(e => e.EtkinlikID == etkinlik.EtkinlikID);
            if (mevcutEtkinlik != null)
            {
                // Mevcut etkinliğin diğer alanlarını güncelle
                mevcutEtkinlik.EtkinlikAdı = etkinlik.EtkinlikAdı;
                mevcutEtkinlik.Açıklama = etkinlik.Açıklama;
                mevcutEtkinlik.Tarih = etkinlik.Tarih;
                mevcutEtkinlik.Saat = etkinlik.Saat;
                mevcutEtkinlik.Konum = etkinlik.Konum;
                mevcutEtkinlik.Süre = etkinlik.Süre;
                mevcutEtkinlik.Kategori = etkinlik.Kategori;
                mevcutEtkinlik.Latitude = etkinlik.Latitude;
                mevcutEtkinlik.Longitude = etkinlik.Longitude;

                // Yeni bir etkinlik resmi yüklendiyse, dosyayı kaydet ve mevcut etkinliğin ImagePath'ini güncelle
                if (EtkinlikResmi != null && EtkinlikResmi.Length > 0)
                {
                    var fileName = Path.GetFileName(EtkinlikResmi.FileName);
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                    // "uploads" klasörü yoksa oluştur
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }

                    // Dosya yolunu ayarla
                    var fullPath = Path.Combine(uploads, fileName);
                    var relativePath = $"/uploads/{fileName}";

                    // Dosyayı kaydet
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await EtkinlikResmi.CopyToAsync(stream);
                    }

                    // Yeni dosya yolunu mevcut etkinliğin ImagePath'ine ata
                    mevcutEtkinlik.ImagePath = relativePath;
                }

                // Veritabanında değişiklikleri kaydet
                await _context.SaveChangesAsync();
            }

            // Güncelleme işlemi tamamlandıktan sonra EtkinlikDetay sayfasına yönlendir
            return RedirectToAction("EtkinlikDetay", new { id = etkinlik.EtkinlikID });
        }






        [HttpPost]
        public IActionResult EtkinlikSil(int EtkinlikID)
        {
            // Etkinliği ve katılımcılarını kontrol et
            var etkinlik = _context.Etkinlikler.FirstOrDefault(e => e.EtkinlikID == EtkinlikID);
            var katilimcilar = _context.Katılımcılar.Where(k => k.EtkinlikID == EtkinlikID).ToList();

            if (etkinlik != null)
            {
                if (katilimcilar.Any())
                {
                    // Katılımcısı olan etkinlik silinemez uyarısı ver
                    TempData["SilmeHatasi"] = "Bu etkinlik silinemiyor çünkü etkinliğe katılımcılar bulunmaktadır.";
                    return RedirectToAction("Etkinlik"); // Kullanıcıyı etkinlik listesine geri yönlendir
                }

                // Eğer katılımcı yoksa etkinliği sil
                _context.Etkinlikler.Remove(etkinlik);
                _context.SaveChanges();
            }

            return RedirectToAction("Etkinlik");
        }


        [HttpPost]
        public IActionResult EtkinliğeKatıl(int EtkinlikID)
        {
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");

            if (kullanıcıID == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı oturum bilgisi bulunamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Home");
            }

            // Aynı etkinliğe daha önce katılıp katılmadığını kontrol edin
            bool dahaOnceKatildiMi = _context.Katılımcılar
                .Any(k => k.KullanıcıID == kullanıcıID.Value && k.EtkinlikID == EtkinlikID);

            if (dahaOnceKatildiMi)
            {
                TempData["ErrorMessage"] = "Bu etkinliğe zaten katıldınız!";
                return RedirectToAction("UserDashboard", new { id = EtkinlikID });
            }

            // Kullanıcının katıldığı etkinliklerin tarih ve saat bilgilerini alıyoruz
            var kullaniciKatildigiEtkinlikler = _context.Katılımcılar
                .Where(k => k.KullanıcıID == kullanıcıID.Value)
                .Select(k => k.EtkinlikID)
                .ToList();

            // Etkinlikleri tarih ve saat bilgilerine göre kontrol et
            var etkinlik = _context.Etkinlikler
                .FirstOrDefault(e => e.EtkinlikID == EtkinlikID);

            if (etkinlik == null)
            {
                TempData["ErrorMessage"] = "Etkinlik bulunamadı!";
                return RedirectToAction("UserDashboard");
            }

            // Çakışma kontrolü: Kullanıcının katıldığı etkinlikler ile yeni etkinliğin tarihi ve saati çakışıyor mu?
            var cakisiyorMu = kullaniciKatildigiEtkinlikler
                .Any(katilanEtkinlikID =>
                {
                    var katilanEtkinlik = _context.Etkinlikler
                        .FirstOrDefault(e => e.EtkinlikID == katilanEtkinlikID);

                    if (katilanEtkinlik == null) return false;

                    // Katıldığı etkinliğin bitiş saatini hesapla
                    var katilanEtkinlikBitis = katilanEtkinlik.Tarih
                        .Add(katilanEtkinlik.Saat)
                        .AddMinutes(katilanEtkinlik.Süre);

                    // Yeni etkinliğin başlangıç saatini hesapla
                    var yeniEtkinlikBaslangic = etkinlik.Tarih.Add(etkinlik.Saat);

                    // Çakışma olup olmadığını kontrol et
                    return yeniEtkinlikBaslangic < katilanEtkinlikBitis &&
                           yeniEtkinlikBaslangic >= katilanEtkinlik.Tarih.Add(katilanEtkinlik.Saat);
                });

            if (cakisiyorMu)
            {
                // Alternatif etkinliklerin sadece ID’lerini kaydet
                var alternatifEtkinlikler = _context.Etkinlikler
                    .Where(e => e.EtkinlikID != EtkinlikID
                                && !kullaniciKatildigiEtkinlikler.Contains(e.EtkinlikID)
                                && e.Tarih > etkinlik.Tarih
                                && e.Onay == true)
                    .OrderBy(e => e.Tarih)
                    .ThenBy(e => e.Saat)
                    .Select(e => e.EtkinlikID) // Sadece ID'ler alınır
                    .ToList();

                TempData["AlternatifEtkinlikler"] = string.Join(",", alternatifEtkinlikler);

                TempData["ErrorMessage"] = "Bu etkinlik ile çakışan bir etkinlik bulundu. Alternatifler aşağıda listelenmiştir.";
                return RedirectToAction("AlternatifEtkinlikler", "UserDashboard");
            }

            try
            {
                // Katılımcı tablosuna ekleme
                var katılımcı = new Katılımcı
                {
                    KullanıcıID = kullanıcıID.Value,
                    EtkinlikID = EtkinlikID
                };
                _context.Katılımcılar.Add(katılımcı);

                // Kullanıcıya puan ekleme, ilk etkinlik katılımında 20 puan, diğer katılımlarda 10 puan
                bool ilkKatılımMi = !_context.Katılımcılar
                    .Any(k => k.KullanıcıID == kullanıcıID.Value);

                int puanDegeri = ilkKatılımMi ? 20 : 10;
                var puan = new Puan
                {
                    KullanıcıID = kullanıcıID.Value,
                    PuanDeğeri = puanDegeri,
                    KazanılanTarih = DateTime.Now
                };
                _context.Puanlar.Add(puan);

                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Etkinliğe başarıyla katıldınız ve {puanDegeri} puan kazandınız!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Etkinliğe katılırken bir hata oluştu: {ex.Message}";
                return RedirectToAction("UserDashboard", new { id = EtkinlikID });
            }

            return RedirectToAction("UserDashboard", new { id = EtkinlikID });
        }




        public IActionResult AlternatifEtkinlikler()
        {
            var alternatifEtkinlikIDler = TempData["AlternatifEtkinlikler"]?.ToString();
            if (string.IsNullOrEmpty(alternatifEtkinlikIDler))
            {
                TempData["ErrorMessage"] = "Alternatif etkinlik bulunamadı!";
                return RedirectToAction("UserDashboard");
            }

            // Alternatif etkinlikleri ID’lere göre yükle
            var ids = alternatifEtkinlikIDler.Split(',').Select(int.Parse).ToList();
            var alternatifEtkinlikler = _context.Etkinlikler
                .Where(e => ids.Contains(e.EtkinlikID))
                .ToList();

            return View(alternatifEtkinlikler);
        }



        [HttpGet]
        public IActionResult Mesaj(int etkinlikID)
        {
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");

            if (kullanıcıID == null)
            {
                return Unauthorized();
            }

            // Kullanıcının katıldığı etkinlikler
            var katildigiEtkinlikler = _context.Katılımcılar
                .Where(k => k.KullanıcıID == kullanıcıID)
                .Select(k => k.Etkinlik)
                .ToList();

            // Her etkinlik için okunmamış mesaj sayısını hesapla
            var okunmamisMesajlar = _context.Bildirimler
                .Where(b => b.KullaniciID == kullanıcıID && !b.OkunduMu)
                .GroupBy(b => b.Mesaj.EtkinlikID)
                .Select(g => new { EtkinlikID = g.Key, OkunmamisMesajSayisi = g.Count() })
                .ToDictionary(x => x.EtkinlikID, x => x.OkunmamisMesajSayisi);

            // Mesajlar
            var mesajlar = _context.Mesajlar
                .Where(m => m.EtkinlikID == etkinlikID)
                .Include(m => m.Gonderici)
                .OrderBy(m => m.GonderimZamani)
                .ToList();

            // ViewBag'e verileri ekle
            ViewBag.Etkinlikler = katildigiEtkinlikler;
            ViewBag.OkunmamisMesajlar = okunmamisMesajlar;
            ViewBag.SeciliEtkinlik = _context.Etkinlikler.FirstOrDefault(e => e.EtkinlikID == etkinlikID);
            ViewBag.Mesajlar = mesajlar;

            return View("Mesaj");
        }



        [HttpPost]
        public IActionResult MesajGoruntulendi([FromBody] int etkinlikID)
        {
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");

            if (kullanıcıID == null)
            {
                return Unauthorized();
            }

            // Bildirimler tablosundaki okunmamış mesajları bulun
            var bildirimler = _context.Bildirimler
                .Where(b => b.KullaniciID == kullanıcıID && b.Mesaj.EtkinlikID == etkinlikID && !b.OkunduMu)
                .ToList();

            if (!bildirimler.Any())
            {
                return NotFound("Okunmamış bildirim bulunamadı.");
            }

            // Okundu bilgilerini güncelle
            foreach (var bildirim in bildirimler)
            {
                bildirim.OkunduMu = true;
            }

            // Değişiklikleri veritabanına kaydet
            _context.SaveChanges();

            return Ok(); // Başarılı dönüş
        }





        [HttpPost]
        public IActionResult MesajGonder(int etkinlikID, string mesajMetni)
        {
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");

            var user = _context.Kullanıcılar.FirstOrDefault(u => u.KullanıcıID == kullanıcıID);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Yeni mesaj oluştur ve kaydet
            var yeniMesaj = new Mesaj
            {
                GondericiID = kullanıcıID.Value,
                EtkinlikID = etkinlikID,
                MesajMetni = mesajMetni,
                GonderimZamani = DateTime.Now
            };

            _context.Mesajlar.Add(yeniMesaj);
            _context.SaveChanges();

            // Etkinliğe katılan diğer kullanıcılar için bildirim ekle
            var katilimcilar = _context.Katılımcılar
                .Where(k => k.EtkinlikID == etkinlikID && k.KullanıcıID != kullanıcıID)
                .Select(k => k.KullanıcıID)
                .ToList();

            // Gönderen kullanıcı için bildirim (OkunduMu = true)
            var gondericiBildirim = new Bildirim
            {
                MesajID = yeniMesaj.MesajID,
                KullaniciID = kullanıcıID.Value,
                OkunduMu = true
            };
            _context.Bildirimler.Add(gondericiBildirim);

            // Diğer kullanıcılar için bildirimler (OkunduMu = false)
            foreach (var katilimciID in katilimcilar)
            {
                var bildirim = new Bildirim
                {
                    MesajID = yeniMesaj.MesajID,
                    KullaniciID = katilimciID,
                    OkunduMu = false
                };
                _context.Bildirimler.Add(bildirim);
            }

            _context.SaveChanges();

            return RedirectToAction("Mesaj", new { etkinlikID });
        }


        public override void OnActionExecuting(ActionExecutingContext context)
        {
            int? kullanıcıID = HttpContext.Session.GetInt32("KullanıcıID");

            if (kullanıcıID != null)
            {
                // Kullanıcının okunmamış bir mesajı var mı?
                var okunmamisMesajVarMi = _context.Bildirimler
                    .Any(b => b.KullaniciID == kullanıcıID && !b.OkunduMu);

                // ViewBag'e aktar
                ViewBag.OkunmamisMesajVarMi = okunmamisMesajVarMi;
            }

            base.OnActionExecuting(context);
        }




    }
}
