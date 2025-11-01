
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
        public DbSet<Group> Groups { get; set; }
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
                .HasIndex(l => l.GroupId);

            modelBuilder.Entity<Line>()
                .HasIndex(l => l.AssignedToId);

            modelBuilder.Entity<Line>()
                .HasIndex(l => l.ExpectedReturnDate);

            modelBuilder.Entity<Line>()
                .HasIndex(l => l.NationalId);

            modelBuilder.Entity<Group>()
                .HasIndex(g => g.OperatorId);

            modelBuilder.Entity<Group>()
                .HasIndex(g => g.ValidityDate);

            modelBuilder.Entity<Group>()
                .HasIndex(g => g.ExpectedReturnDate);

            modelBuilder.Entity<AssignmentLog>()
                .HasIndex(al => al.AssignedAt);

            // Configure relationships
            modelBuilder.Entity<Group>()
                .HasOne(g => g.Operator)
                .WithMany()
                .HasForeignKey(g => g.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Line>()
                .HasOne(l => l.Group)
                .WithMany(g => g.Lines)
                .HasForeignKey(l => l.GroupId)
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

            // Seed operators
            modelBuilder.Entity<Operator>().HasData(
                new Operator { Id = 1, Name = "اتصالات", ColorHex = "#008000", IconPath = "Resources/Images/etisalat_logo.png" },
                new Operator { Id = 2, Name = "فودافون", ColorHex = "#E60000", IconPath = "Resources/Images/vodafone_logo.png" },
                new Operator { Id = 3, Name = "وي", ColorHex = "#8B008B", IconPath = "Resources/Images/we_logo.png" },
                new Operator { Id = 4, Name = "أورانج", ColorHex = "#FF8C00", IconPath = "Resources/Images/orange_logo.png" }
            );

            // Seed default admin user
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, FullName = "المسؤول", Role = "Admin", IsActive = true }
            );
        }
    }
}
