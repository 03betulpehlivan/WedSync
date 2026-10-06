using System.ComponentModel.DataAnnotations;

namespace dugunsalonu.ViewModels
{
    public class PaymentViewModel
    {
        [Required]
        public string Plan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kart üzerindeki isim gereklidir.")]
        [Display(Name = "Kart Üzerindeki İsim")]
        public string CardHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kart numarası gereklidir.")]
        [Display(Name = "Kart Numarası")]
        [StringLength(19, MinimumLength = 15, ErrorMessage = "Geçerli bir kart numarası giriniz.")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Son kullanma tarihi gereklidir.")]
        [Display(Name = "Son Kullanma Tarihi")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "Geçerli bir tarih giriniz (AA/YY).")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVC kodu gereklidir.")]
        [Display(Name = "CVC")]
        [StringLength(4, MinimumLength = 3, ErrorMessage = "Geçerli bir CVC kodu giriniz.")]
        public string Cvc { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Ödeme koşullarını kabul etmelisiniz.")]
        public bool AcceptTerms { get; set; }

        public string GetPrice() => PlanConfig.GetPrice(Plan);
        public string GetPriceRaw() => PlanConfig.GetPriceRaw(Plan);
        public string GetOldPrice() => PlanConfig.GetOldPrice(Plan);
        public string GetUploadLimit() => PlanConfig.GetUploadLimit(Plan);
        public string GetStorageDays() => PlanConfig.GetStorageDays(Plan);
        public string GetUploadDays() => PlanConfig.GetUploadDays(Plan);
    }

    public static class PlanConfig
    {
        public static readonly string[] PaidPlans = { "Pro", "SalonBusiness" };

        public static string GetPrice(string plan) => plan switch
        {
            "Pro" => "2.000",
            "SalonBusiness" => "4.999",
            _ => "0"
        };

        public static string GetPriceRaw(string plan) => plan switch
        {
            "Pro" => "2000",
            "SalonBusiness" => "4999",
            _ => "0"
        };

        public static string GetOldPrice(string plan) => plan switch
        {
            "Pro" => "3.999",
            "SalonBusiness" => "7.999",
            _ => ""
        };

        public static string GetUploadLimit(string plan) => plan switch
        {
            "Pro" => "Sınırsız",
            "SalonBusiness" => "Sınırsız (etkinlik başına)",
            _ => "50"
        };

        public static string GetStorageDays(string plan) => plan switch
        {
            "Pro" => "365",
            "SalonBusiness" => "365 (etkinlik başına)",
            _ => "7"
        };

        public static string GetUploadDays(string plan) => plan switch
        {
            "Pro" => "30",
            "SalonBusiness" => "30 (etkinlik başına)",
            _ => "1"
        };

        /// <summary>Plan bazlı depolama süresi (gün). Albümde içerik bu süre boyunca görünür.</summary>
        public static int GetStorageDaysInt(dugunsalonu.Models.PlanType planType) => planType switch
        {
            dugunsalonu.Models.PlanType.Pro => 365,
            dugunsalonu.Models.PlanType.SalonBusiness => 365,
            _ => 7 // Free, Plus
        };

        /// <summary>Plan bazlı yükleme süresi (gün). Etkinlik tarihinden itibaren bu kadar gün yükleme yapılabilir.</summary>
        public static int GetUploadDaysInt(dugunsalonu.Models.PlanType planType) => planType switch
        {
            dugunsalonu.Models.PlanType.Pro => 30,
            dugunsalonu.Models.PlanType.SalonBusiness => 30,
            _ => 1
        };

        /// <summary>Plan bazlı yükleme limiti (adet).</summary>
        public static int GetUploadLimitInt(dugunsalonu.Models.PlanType planType) => planType switch
        {
            dugunsalonu.Models.PlanType.Pro => int.MaxValue,
            dugunsalonu.Models.PlanType.SalonBusiness => int.MaxValue,
            _ => 50
        };
    }
}
