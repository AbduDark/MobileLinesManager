
using System;
using System.Linq;
using MobileLinesManager.Models;
using Microsoft.EntityFrameworkCore;

namespace MobileLinesManager.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Apply migrations
            context.Database.Migrate();
            
            // Enable foreign keys
            context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
            
            // Check if operators already exist
            if (context.Operators.Any())
            {
                return; // DB has been seeded
            }

            // Seed operators
            var operators = new[]
            {
                new Operator 
                { 
                    Name = "فودافون",
                    ColorHex = "#E60000", 
                    IconPath = "vodafone.png",
                    //IsActive = true 
                },
                new Operator 
                { 
                    Name = "اتصالات",
                    ColorHex = "#00B140", 
                    IconPath = "etisalat.png",
                    //IsActive = true 
                },
                new Operator 
                { 
                    Name = "أورنج",
                    ColorHex = "#FF7900", 
                    IconPath = "orange.png",
                    //IsActive = true 
                },
                new Operator 
                { 
                    Name = "وي",
                    ColorHex = "#6C2C91", 
                    IconPath = "we.png",
                    //IsActive = true 
                }
            };

            context.Operators.AddRange(operators);
            context.SaveChanges();

            // Seed default admin user
            var admin = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                FullName = "مسؤول النظام",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);
            context.SaveChanges();
        }
    }
}
