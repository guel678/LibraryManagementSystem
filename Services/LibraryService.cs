using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public sealed class LibraryService
{
    private const decimal FinePerDay = 10m;
    private readonly DataStore store;

    public LibraryService(DataStore store)
    {
        this.store = store;
    }

    public decimal CalculateFine(DateTime dueDate, DateTime returnDate)
    {
        var overdueDays = Math.Max(0, (returnDate.Date - dueDate.Date).Days);
        return overdueDays * FinePerDay;
    }

    public bool CheckoutBook(Book book, Patron patron, DateTime checkoutDate, DateTime dueDate, out string message)
    {
        using var context = new LibraryDbContext();
        using var dbTransaction = context.Database.BeginTransaction();

        var currentBook = context.Books.FirstOrDefault(x => x.Id == book.Id);
        var currentPatron = context.Patrons.FirstOrDefault(x => x.Id == patron.Id);
        if (currentBook is null || currentPatron is null)
        {
            message = "Select a valid book and patron.";
            return false;
        }

        if (currentBook.AvailableCopies <= 0)
        {
            message = "This book has no available copies.";
            return false;
        }

        if (!string.Equals(currentPatron.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            message = "Only active patrons can check out books.";
            return false;
        }

        var nextTransactionId = context.Transactions.Select(x => (int?)x.Id).Max() ?? 0;
        context.Transactions.Add(new LoanTransaction
        {
            Id = nextTransactionId + 1,
            BookId = currentBook.Id,
            PatronId = currentPatron.Id,
            CheckoutDate = checkoutDate.Date,
            DueDate = dueDate.Date,
            Status = dueDate.Date < DateTime.Today ? TransactionStatus.Overdue : TransactionStatus.CheckedOut
        });
        currentBook.CheckedOutCount++;
        context.SaveChanges();
        dbTransaction.Commit();

        store.Load();
        message = "Book checked out successfully.";
        return true;
    }

    public decimal ReturnBook(LoanTransaction transaction, DateTime returnDate)
    {
        using var context = new LibraryDbContext();
        using var dbTransaction = context.Database.BeginTransaction();

        var currentTransaction = context.Transactions.FirstOrDefault(x => x.Id == transaction.Id);
        if (currentTransaction is null)
        {
            store.Load();
            return 0m;
        }

        if (currentTransaction.IsReturned)
        {
            store.Load();
            return currentTransaction.FineAmount;
        }

        currentTransaction.ReturnDate = returnDate.Date;
        currentTransaction.FineAmount = CalculateFine(currentTransaction.DueDate, returnDate);
        currentTransaction.Status = TransactionStatus.Returned;
        var book = context.Books.FirstOrDefault(x => x.Id == currentTransaction.BookId);
        if (book is not null && book.CheckedOutCount > 0)
        {
            book.CheckedOutCount--;
        }

        if (currentTransaction.FineAmount > 0)
        {
            var nextFineId = context.Fines.Select(x => (int?)x.Id).Max() ?? 0;
            context.Fines.Add(new Fine
            {
                Id = nextFineId + 1,
                PatronId = currentTransaction.PatronId,
                Amount = currentTransaction.FineAmount,
                DateApplied = returnDate.Date,
                IsPaid = false
            });
        }

        context.SaveChanges();
        dbTransaction.Commit();

        store.Load();
        return currentTransaction.FineAmount;
    }

    public void MarkFinePaid(int fineId)
    {
        using var context = new LibraryDbContext();
        var fine = context.Fines.FirstOrDefault(x => x.Id == fineId);
        if (fine is null)
        {
            store.Load();
            return;
        }

        fine.IsPaid = true;
        context.SaveChanges();
        store.Load();
    }
}
