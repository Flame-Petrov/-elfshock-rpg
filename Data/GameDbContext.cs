using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace RPG_Game_Elfshock.Data
{
    /// <summary>
    /// EF Core context backed by a local SQLite file (elfshock.db next to the app).
    /// Stores the hero and game logs.
    /// </summary>
    public class GameDbContext : DbContext
    {
        public DbSet<HeroRecord> Heroes => Set<HeroRecord>();
        public DbSet<GameRecord> Games => Set<GameRecord>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "elfshock.db");
            options.UseSqlite($"Data Source={dbPath}");
        }
    }
}
