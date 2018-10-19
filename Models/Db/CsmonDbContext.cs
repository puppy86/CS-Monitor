using Microsoft.EntityFrameworkCore;

namespace csmon.Models.Db
{
    /// <summary>
    /// DB context for working with MS SQL database
    /// </summary>
    public class CsmonDbContext : DbContext
    {
        // Connection string
        public const string ConnectionString = "Data Source=localhost;Initial Catalog=csmon;Integrated Security=True";

        // Table for nodes data (Country, Location, etc.)
        public DbSet<Node> Nodes { get; set; }

        // A list of pre-defined smart contract addresses
        public DbSet<Smart> Smarts { get; set; }

        // Table for storing network speed measurements, that used for displaying chart on the "Transactions per second" page
        public DbSet<Tp> Tps { get; set; }

        //  A query for requesting TPS data, from the Tps page
        public DbQuery<Point> Points { get; set; }

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
        }
    }
}
