using System.ComponentModel.DataAnnotations.Schema;

namespace AkıllıEtkinlik.Models
{
    public class Etkinlik
    {
        public int EtkinlikID { get; set; }
        public string? EtkinlikAdı { get; set; }
        public string? Açıklama { get; set; }
        public DateTime Tarih { get; set; }
        public TimeSpan Saat { get; set; }
        public int Süre { get; set; }
        public string? Konum { get; set; }
        public string? Kategori { get; set; }
        public int KullanıcıID { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? ImagePath { get; set; }

        public bool Onay { get; set; } = false;

        [NotMapped] 
        public int Puan { get; set; }


        [NotMapped] // Bu özellik de veritabanında saklanmayacak
        public double Mesafe { get; set; }
        public Kullanıcı? Kullanıcı { get; set; }

      
    }


}
