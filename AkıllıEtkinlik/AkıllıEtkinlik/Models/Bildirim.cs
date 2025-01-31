using AkıllıEtkinlik.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkilliEtkinlik.Models
{
    public class Bildirim
    {
        [Key]
        public int BildirimID { get; set; }

        [ForeignKey("Mesaj")]
        public int MesajID { get; set; }
        public Mesaj Mesaj { get; set; } // Mesaj ile ilişkili

        [ForeignKey("Kullanici")]
        public int KullaniciID { get; set; }
        public Kullanıcı Kullanici { get; set; } // Kullanıcı ile ilişkili

        [Required]
        public bool OkunduMu { get; set; } = false; // Varsayılan olarak okunmamış
    }
}
