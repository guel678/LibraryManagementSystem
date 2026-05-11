using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

namespace LibraryManagementSystem.Services;

public sealed class DataStore
{
    public LibraryData Data { get; private set; } = new();

    public DataStore()
    {
        using var context = new LibraryDbContext();
        context.Database.EnsureCreated();
        SeedAdminIfNeeded(context);
        SeedBooksIfNeeded(context);
        ReconcileBookBorrowedCounts(context);
        Load();
    }

    public string DatabasePath => LibraryDbContext.DatabasePath;

    public void Load()
    {
        using var context = new LibraryDbContext();
        Data = new LibraryData
        {
            Users = context.Users.AsNoTracking().OrderBy(x => x.Id).ToList(),
            Books = context.Books.AsNoTracking().OrderBy(x => x.Id).ToList(),
            Patrons = context.Patrons.AsNoTracking().OrderBy(x => x.Id).ToList(),
            Transactions = context.Transactions.AsNoTracking().OrderBy(x => x.Id).ToList(),
            Fines = context.Fines.AsNoTracking().OrderBy(x => x.Id).ToList()
        };
    }

    public void Save()
    {
        using var context = new LibraryDbContext();
        using var transaction = context.Database.BeginTransaction();

        SyncUsers(context);
        SyncBooks(context);
        SyncPatrons(context);
        ReconcileBookBorrowedCounts(context);

        context.SaveChanges();
        transaction.Commit();
        Load();
    }

    public int NextUserId() => NextId(Data.Users.Select(x => x.Id));
    public int NextBookId() => NextId(Data.Books.Select(x => x.Id));
    public int NextPatronId() => NextId(Data.Patrons.Select(x => x.Id));
    public int NextTransactionId() => NextId(Data.Transactions.Select(x => x.Id));
    public int NextFineId() => NextId(Data.Fines.Select(x => x.Id));

    private static int NextId(IEnumerable<int> ids) => ids.DefaultIfEmpty(0).Max() + 1;

    private void SyncUsers(LibraryDbContext context)
    {
        var incomingIds = Data.Users.Select(x => x.Id).ToHashSet();
        context.Users.RemoveRange(context.Users.Where(x => !incomingIds.Contains(x.Id)));

        foreach (var user in Data.Users)
        {
            var current = context.Users.FirstOrDefault(x => x.Id == user.Id);
            if (current is null)
            {
                context.Users.Add(user);
                continue;
            }

            current.Username = user.Username.Trim();
            current.PasswordHash = user.PasswordHash;
            current.Role = user.Role;
            current.FullName = user.FullName.Trim();
            current.IsActive = user.IsActive;
        }
    }

    private void SyncBooks(LibraryDbContext context)
    {
        var incomingIds = Data.Books.Select(x => x.Id).ToHashSet();
        var removedBookIds = context.Books
            .Where(x => !incomingIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToList();
        if (removedBookIds.Count > 0)
        {
            context.Transactions.RemoveRange(context.Transactions.Where(x => removedBookIds.Contains(x.BookId)));
        }

        var removableBooks = context.Books
            .Where(x => removedBookIds.Contains(x.Id))
            .ToList();
        context.Books.RemoveRange(removableBooks);

        foreach (var book in Data.Books)
        {
            var current = context.Books.FirstOrDefault(x => x.Id == book.Id);
            if (current is null)
            {
                context.Books.Add(new Book
                {
                    Id = book.Id,
                    Title = book.Title.Trim(),
                    Author = book.Author.Trim(),
                    Isbn = book.Isbn.Trim(),
                    Genre = book.Genre.Trim(),
                    Publisher = book.Publisher.Trim(),
                    PublishedYear = book.PublishedYear,
                    Quantity = Math.Max(0, book.Quantity),
                    Description = book.Description.Trim(),
                    CheckedOutCount = 0
                });
                continue;
            }

            current.Title = book.Title.Trim();
            current.Author = book.Author.Trim();
            current.Isbn = book.Isbn.Trim();
            current.Genre = book.Genre.Trim();
            current.Publisher = book.Publisher.Trim();
            current.PublishedYear = book.PublishedYear;
            current.Quantity = Math.Max(book.Quantity, current.CheckedOutCount);
            current.Description = book.Description.Trim();
            current.CheckedOutCount = Math.Clamp(current.CheckedOutCount, 0, current.Quantity);
        }
    }

    private void SyncPatrons(LibraryDbContext context)
    {
        var incomingIds = Data.Patrons.Select(x => x.Id).ToHashSet();
        var removedPatronIds = context.Patrons
            .Where(x => !incomingIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToList();
        if (removedPatronIds.Count > 0)
        {
            context.Transactions.RemoveRange(context.Transactions.Where(x => removedPatronIds.Contains(x.PatronId)));
            context.Fines.RemoveRange(context.Fines.Where(x => removedPatronIds.Contains(x.PatronId)));
        }

        var removablePatrons = context.Patrons
            .Where(x => removedPatronIds.Contains(x.Id))
            .ToList();
        context.Patrons.RemoveRange(removablePatrons);

        foreach (var patron in Data.Patrons)
        {
            var current = context.Patrons.FirstOrDefault(x => x.Id == patron.Id);
            if (current is null)
            {
                context.Patrons.Add(new Patron
                {
                    Id = patron.Id,
                    FullName = patron.FullName.Trim(),
                    MembershipId = patron.MembershipId.Trim(),
                    Email = patron.Email.Trim(),
                    PhoneNumber = patron.PhoneNumber.Trim(),
                    Address = patron.Address.Trim(),
                    DateOfBirth = patron.DateOfBirth.Date,
                    MembershipType = patron.MembershipType,
                    Status = patron.Status
                });
                continue;
            }

            current.FullName = patron.FullName.Trim();
            current.MembershipId = patron.MembershipId.Trim();
            current.Email = patron.Email.Trim();
            current.PhoneNumber = patron.PhoneNumber.Trim();
            current.Address = patron.Address.Trim();
            current.DateOfBirth = patron.DateOfBirth.Date;
            current.MembershipType = patron.MembershipType;
            current.Status = patron.Status;
        }
    }

    private static void SeedAdminIfNeeded(LibraryDbContext context)
    {
        if (context.Users.Any())
        {
            return;
        }

        context.Users.Add(new User
        {
            Id = 1,
            Username = "admin",
            FullName = "System Administrator",
            PasswordHash = PasswordService.Hash("admin123"),
            Role = UserRole.Admin,
            IsActive = true
        });
        context.SaveChanges();
    }

    private static void SeedBooksIfNeeded(LibraryDbContext context)
    {
        try
        {
            var docxPath = FindNearbyFile("50_books_library_table.docx");
            if (docxPath is null)
            {
                return;
            }

            var existingIsbns = context.Books
                .AsNoTracking()
                .Where(x => x.Isbn != "")
                .Select(x => x.Isbn)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingBooks = context.Books
                .AsNoTracking()
                .Select(x => $"{x.Title}|{x.Author}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var nextId = context.Books.Select(x => (int?)x.Id).Max() ?? 0;
            var books = ReadBooksFromDocx(docxPath)
                .Where(x => !existingIsbns.Contains(x.Isbn) && !existingBooks.Contains($"{x.Title}|{x.Author}"))
                .ToList();

            foreach (var book in books)
            {
                book.Id = ++nextId;
                context.Books.Add(book);
            }

            if (books.Count > 0)
            {
                context.SaveChanges();
            }
        }
        catch
        {
            // Startup should never fail because optional seed data could not be imported.
        }
    }

    private static string? FindNearbyFile(string fileName)
    {
        var directories = new[]
        {
            AppContext.BaseDirectory,
            Environment.CurrentDirectory
        };

        foreach (var start in directories)
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                var path = Path.Combine(directory.FullName, fileName);
                if (File.Exists(path))
                {
                    return path;
                }

                path = Path.Combine(directory.FullName, "..", fileName);
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }

                directory = directory.Parent;
            }
        }

        return null;
    }

    private static IEnumerable<Book> ReadBooksFromDocx(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var document = archive.GetEntry("word/document.xml");
        if (document is null)
        {
            yield break;
        }

        using var stream = document.Open();
        var xml = XDocument.Load(stream);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var rows = xml.Descendants(w + "tr").Skip(1);

        foreach (var row in rows)
        {
            var cells = row.Elements(w + "tc")
                .Select(cell => string.Concat(cell.Descendants(w + "t").Select(text => text.Value)).Trim())
                .ToList();

            if (cells.Count < 7 || !int.TryParse(cells[5], out var year) || !int.TryParse(cells[6], out var copies))
            {
                continue;
            }

            yield return new Book
            {
                Title = cells[0],
                Author = cells[1],
                Isbn = cells[2],
                Genre = cells[3],
                Publisher = cells[4],
                PublishedYear = year,
                Quantity = copies,
                CheckedOutCount = 0
            };
        }
    }

    private static void ReconcileBookBorrowedCounts(LibraryDbContext context)
    {
        var borrowedCounts = context.Transactions
            .Where(x => x.ReturnDate == null)
            .GroupBy(x => x.BookId)
            .Select(x => new { BookId = x.Key, Count = x.Count() })
            .ToDictionary(x => x.BookId, x => x.Count);

        var changed = false;
        foreach (var book in context.Books)
        {
            var realBorrowedCount = borrowedCounts.TryGetValue(book.Id, out var count) ? count : 0;
            realBorrowedCount = Math.Clamp(realBorrowedCount, 0, book.Quantity);
            if (book.CheckedOutCount == realBorrowedCount)
            {
                continue;
            }

            book.CheckedOutCount = realBorrowedCount;
            changed = true;
        }

        if (changed)
        {
            context.SaveChanges();
        }
    }
}
