using Microsoft.EntityFrameworkCore;
using Shared.Db;

namespace WebApi.Model
{
    public class ShowsDbContext : DbContext
    {
        public ShowsDbContext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<DbCast> Casts { get; set; }
        public DbSet<DbShow> Shows { get; set; }
    }
}
