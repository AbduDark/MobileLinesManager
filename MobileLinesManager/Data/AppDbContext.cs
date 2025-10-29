
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MobileLinesManager.Models;
using System;
using System.IO;

namespace MobileLinesManager.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Operator> Operators { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Line> Lines { get; set; }
        public DbSet<AssignmentLog> AssignmentLogs { get; set; }
        public DbSet<AlertRule> AlertRules { get; set; }
        public DbSet<AuditTrail> AuditTrails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MobileLinesManager"
                );
                
                Directory.CreateDirectory(appDataPath);
                
                string dbPath = Path.Combine(appDataPath, "mobile_lines.db");
                
                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = dbPath,
                    ForeignKeys = true
                }.ToString();
                
                optionsBuilder.UseSqlite(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure indices
            modelBuilder.Entity<Line>()
                .HasIndex(l => l.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<Line>()
                .HasIndex(l => l.CategoryId);

            modelBuilder.Entity<Line>()
                .HasIndex(l => l.AssignedToId);

            modelBuilder.Entity<Line>()
                .HasIndex(l => l.ExpectedReturnDate);

            modelBuilder.Entity<AssignmentLog>()
                .HasIndex(al => al.AssignedAt);

            // Configure relationships
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Operator)
                .WithMany(o => o.Categories)
                .HasForeignKey(c => c.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Line>()
                .HasOne(l => l.Category)
                .WithMany(c => c.Lines)
                .HasForeignKey(l => l.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Line>()
                .HasOne(l => l.AssignedTo)
                .WithMany()
                .HasForeignKey(l => l.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AssignmentLog>()
                .HasOne(al => al.Line)
                .WithMany()
                .HasForeignKey(al => al.LineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AlertRule>()
                .HasOne(ar => ar.Category)
                .WithMany()
                .HasForeignKey(ar => ar.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed operators
            modelBuilder.Entity<Operator>().HasData(
                new Operator { Id = 1, Name = "اتصالات", ColorHex = "#008000", IconPath = null },
                new Operator { Id = 2, Name = "فودافون", ColorHex = "#FF0000", IconPath = null },
                new Operator { Id = 3, Name = "وي", ColorHex = "#800080", IconPath = null },
                new Operator { Id = 4, Name = "أورانج", ColorHex = "#FFA500", IconPath = null }
            );

            // Seed default admin user
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, FullName = "المسؤول", Role = "Admin", IsActive = true }
            );
        }
    }
}
