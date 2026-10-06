using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dugunsalonu.Models
{
    public class GuestEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid EventId { get; set; }

        [ForeignKey("EventId")]
        public WeddingEvent? WeddingEvent { get; set; }

        [Display(Name = "Misafir Adı")]
        public string? GuestName { get; set; }

        [Display(Name = "Mesaj/Anı")]
        public string? Message { get; set; }

        public string? PhotoPath { get; set; } // Yüklenen fotoğrafın yolu

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false; // Moderasyon için varsayılan false

        public string? GuestSessionId { get; set; } // Hangi tarayıcıdan yüklendiğini takip etmek için
    }
}
