using System;
using System.ComponentModel.DataAnnotations;

namespace dugunsalonu.Models
{
    public class WeddingEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Display(Name = "Çift İsimleri")]
        public string CoupleName { get; set; } = string.Empty; // Örn: Ahmet & Ayşe

        [Required]
        [Display(Name = "Düğün Tarihi")]
        public DateTime EventDate { get; set; }

        public int? SalonId { get; set; }

        [Required]
        public string Slug { get; set; } = string.Empty;

        public string ThemeColor { get; set; } = "#FFFFFF";

        // B2B White Label Özellikleri
        [Display(Name = "Salon/Firma Logosu")]
        public string? LogoUrl { get; set; } // Salon logosu URL'i

        [Display(Name = "Salon Adı")]
        public string? SalonName { get; set; } // Örn: Grand Hotel Balo Salonu

        public string? UserId { get; set; }
        
        public PlanType PlanType { get; set; } = PlanType.Free;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum PlanType
    {
        Free,
        Plus,
        Pro,
        SalonBusiness // Yeni B2B Planı
    }
}
