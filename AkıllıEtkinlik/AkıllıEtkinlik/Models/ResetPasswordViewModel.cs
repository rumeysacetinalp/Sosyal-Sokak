using System.ComponentModel.DataAnnotations;

namespace AkıllıEtkinlik.Models
{
    public class ResetPasswordViewModel
    {
        public string Token { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; }
    }

}
