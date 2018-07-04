using Microsoft.EntityFrameworkCore;

namespace csmon.Models.Db
{
    public class CsmonDbContext : DbContext
    {
        public const string ConnectionString = "Data Source=csmon.db";
        public DbSet<Node> Nodes { get; set; }

        public CsmonDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(ConnectionString);
        }
    }
}
