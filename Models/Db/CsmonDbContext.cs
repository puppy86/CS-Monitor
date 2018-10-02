using Microsoft.EntityFrameworkCore;

namespace csmon.Models.Db
{
    public class CsmonDbContext : DbContext
    {
        public const string ConnectionString = "Data Source=localhost;Initial Catalog=csmon;Integrated Security=True";
        public DbSet<Node> Nodes { get; set; }
        public DbSet<Smart> Smarts { get; set; }
        public DbSet<Tp> Tps { get; set; }

        public CsmonDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tp>().HasKey(p => new { p.Network, p.Time });
        }
    }
}
