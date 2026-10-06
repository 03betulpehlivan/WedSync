using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using dugunsalonu.Models;
using Microsoft.AspNetCore.Identity;

namespace dugunsalonu.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WeddingEvent> WeddingEvents { get; set; }
        public DbSet<GuestEntry> GuestEntries { get; set; }
    }
}
