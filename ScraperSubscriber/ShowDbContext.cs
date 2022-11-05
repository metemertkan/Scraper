using Microsoft.EntityFrameworkCore;
using Shared.Db;

namespace ScraperSubscriber
{
    public class ShowDbContext : DbContext
    {
        public DbSet<DbCast> Casts { get; set; }
        public DbSet<DbShow> Shows { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("server=db;database=shows4;user=root;password=password");
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }
}
