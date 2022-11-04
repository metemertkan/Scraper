using Microsoft.EntityFrameworkCore;
using Shared.Db;

namespace Scraper
{
    public class ShowDbContext : DbContext
    {
        public DbSet<DbCast> Casts { get; set; }
        public DbSet<DbShow> Shows { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        {
            optionsBuilder.UseMySQL("server=db;database=shows3;user=root;password=password");
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }
}
