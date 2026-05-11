using System.IO;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data;

public sealed class LibraryDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Patron> Patrons => Set<Patron>();
    public DbSet<LoanTransaction> Transactions => Set<LoanTransaction>();
    public DbSet<Fine> Fines => Set<Fine>();

    public static string DatabasePath
    {
        get
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            var folder = Path.Combine(localAppData, "LibraryManagementSystem");
            Directory.CreateDirectory(folder);
            ClearReadOnlyAttribute(folder);

            var databasePath = Path.Combine(folder, "library.db");
            ClearReadOnlyAttribute(databasePath);
            return databasePath;
        }
    }

    private static void ClearReadOnlyAttribute(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return;
        }

        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReadOnly) == 0)
        {
            return;
        }

        File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<Book>()
            .HasIndex(x => x.Isbn)
            .IsUnique();

        modelBuilder.Entity<Patron>()
            .HasIndex(x => x.MembershipId)
            .IsUnique();

        modelBuilder.Entity<Book>()
            .HasMany(x => x.Transactions)
            .WithOne(x => x.Book)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Patron>()
            .HasMany(x => x.Transactions)
            .WithOne(x => x.Patron)
            .HasForeignKey(x => x.PatronId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Patron>()
            .HasMany(x => x.Fines)
            .WithOne(x => x.Patron)
            .HasForeignKey(x => x.PatronId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
