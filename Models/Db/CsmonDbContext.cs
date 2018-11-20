using Microsoft.EntityFrameworkCore;

namespace csmon.Models.Db
{
    // DB context for working with MS SQL database
    public class CsmonDbContext : DbContext
    {
        // Connection string
        public const string ConnectionString = "Data Source=localhost;Initial Catalog=csmon;Integrated Security=True";

        // Table for nodes data
        public DbSet<Node> Nodes { get; set; }

        // Table for locations data
        public DbSet<Location> Locations { get; set; }

        // Table for storing network speed measurements, that used for displaying chart on the "Transactions per second" page
        public DbSet<Tp> Tps { get; set; }

        //  A query for requesting TPS data, from the Tps page
        public DbQuery<Point> Points { get; set; }

        // Tables for tokens data
        public DbSet<Token> Tokens { get; set; }
        public DbSet<TokenProperty> TokensProperties { get; set; }

        public CsmonDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Settings.RemoteDatabase ? Config.ConnectionString : ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tp>().HasKey(p => new { p.Network, p.Time });
            modelBuilder.Entity<TokenProperty>().HasKey(tp => new { TokenId = tp.TokenAddress, tp.Property });
        }

        // Creates DB Connection
        public static CsmonDbContext Create()
        {
            return new CsmonDbContext(new DbContextOptions<CsmonDbContext>());
        }
    }
}
