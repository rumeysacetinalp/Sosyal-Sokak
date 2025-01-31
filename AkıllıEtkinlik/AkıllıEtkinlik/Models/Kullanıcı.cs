using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkıllıEtkinlik.Models
{
    public class Kullanıcı
    {
        [Key]
        public int KullanıcıID { get; set; }

        [Column("KullanıcıAdı")]
        public string? KullanıcıAdı { get; set; }

        [Column("Şifre")]
        public string? Sifre { get; set; }

        [Column("Eposta")]
        public string? Eposta { get; set; }

        [Column("Konum")]
        public string? Konum { get; set; }

        [Column("İlgiAlanları")]
        public string? IlgiAlanlari { get; set; }

        [Column("Ad")]
        public string? Ad { get; set; }

        [Column("Soyad")]
        public string? Soyad { get; set; }

        [Column("DoğumTarihi")]
        public DateTime? DogumTarihi { get; set; }

        [Column("Cinsiyet")]
        public string? Cinsiyet { get; set; }


        [Column("TelefonNumarası")]
        public string? TelefonNumarasi { get; set; }

        [Column("ProfilFotoğrafı")]
        public string? ProfilFotografi { get; set; }


        [Column("Latitude")]
        public decimal? Latitude { get; set; }

        [Column("Longitude")]
        public decimal? Longitude { get; set; }


        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
    }
}
