namespace AkıllıEtkinlik.Models
{
    public class Katılımcı
    {
        public int KatılımcıID { get; set; }
        public int KullanıcıID { get; set; }
        public int EtkinlikID { get; set; }
        // Kullanıcı ve Etkinlik ile ilişkileri tanımlamak için navigation property ekleyebilirsiniz


        public Etkinlik? Etkinlik { get; set; } // Etkinlik ile ilişki
        public Kullanıcı? Kullanıcı { get; set; } // Kullanıcı ile ilişki
    }

}
