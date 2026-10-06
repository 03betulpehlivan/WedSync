using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace dugunsalonu.ViewModels
{
    public class GuestEntryViewModel
    {
        public Guid EventId { get; set; }
        
        public string CoupleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lütfen adınızı yazın.")]
        [Display(Name = "Adınız")]
        public string GuestName { get; set; }

        [Display(Name = "Notunuz / Dileğiniz")]
        public string? Message { get; set; }

        [Display(Name = "Fotoğraflar")]
        public List<IFormFile>? Photos { get; set; }

        /// <summary>Tek dosya için geriye dönük uyumluluk (eski formlar)</summary>
        public IFormFile? Photo { get; set; }
        
        public string? GuestSessionId { get; set; }
    }
}
