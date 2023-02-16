using CloudplayWebApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CloudplayWebApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        //DbContextOptions<ApplicationDbContext> contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
        //    .UseSqlServer(@"Server=vm-managerv2.database.windows.net;Database=vm-databasev2")
        //    .Options;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> contextOptions)
            : base(contextOptions)
        {
        }
        public DbSet<CustomVM> CustomVMs { get; set; }
    }
}