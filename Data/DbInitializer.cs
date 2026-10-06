using dugunsalonu.Models;
using System;
using System.Linq;

namespace dugunsalonu.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.WeddingEvents.Any())
            {
                return;   // DB has been seeded
            }

            var weddingEvents = new WeddingEvent[]
            {
                new WeddingEvent{CoupleName="Ahmet & Ayşe", EventDate=DateTime.Now.AddDays(7), Slug="ahmet-ayse", ThemeColor="#D4AF37", CreatedAt=DateTime.Now},
                new WeddingEvent{CoupleName="Mehmet & Zeynep", EventDate=DateTime.Now.AddDays(14), Slug="mehmet-zeynep", ThemeColor="#E6E6FA", CreatedAt=DateTime.Now}
            };

            foreach (var s in weddingEvents)
            {
                context.WeddingEvents.Add(s);
            }
            context.SaveChanges();
        }
    }
}
