using AkıllıEtkinlik.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkilliEtkinlik.Models
{
    public class Mesaj
    {
        [Key]
        public int MesajID { get; set; }

        [ForeignKey("Kullanici")]
        public int GondericiID { get; set; }
        public Kullanıcı? Gonderici { get; set; }  // Mesajı gönderen kullanıcı bilgisi

        [ForeignKey("Etkinlik")]
        public int EtkinlikID { get; set; }
        public Etkinlik? Etkinlik { get; set; }  // Mesajın ait olduğu etkinlik bilgisi

        [Required]
        public string MesajMetni { get; set; } = string.Empty;

        [Required]
        public DateTime GonderimZamani { get; set; }



    }
}
