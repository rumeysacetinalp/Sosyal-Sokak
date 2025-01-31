using System.ComponentModel.DataAnnotations;

namespace AkıllıEtkinlik.Models
{
    public class Admin
    {
        [Key] // Primary Key olduğunu belirtiyor
        public int ID { get; set; }
      
        public string KullaniciAdi { get; set; }

        public string Sifre { get; set; }
    }
}
