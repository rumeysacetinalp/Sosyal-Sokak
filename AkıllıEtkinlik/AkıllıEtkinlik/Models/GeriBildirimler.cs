using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkıllıEtkinlik.Models
{
    public class GeriBildirimler
    {
        [Key]
        public int GeriBildirimID { get; set; }

        [Required]
        public int KullanıcıID { get; set; }

        [Required]
        public int EtkinlikID { get; set; }

        [Required]
        [Range(1, 5)]
        public int Puan { get; set; } // 1-5 yıldız puanı

        public string? Yorum { get; set; } // Opsiyonel metin geri bildirimi

       
        public Kullanıcı Kullanıcı { get; set; }
        public Etkinlik Etkinlik { get; set; }
    }
}
