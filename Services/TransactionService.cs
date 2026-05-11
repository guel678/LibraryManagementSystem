using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public sealed class TransactionService
{
    private readonly DataStore store;
    private readonly LibraryService library;

    public TransactionService(DataStore store)
    {
        this.store = store;
        library = new LibraryService(store);
    }

    public bool CheckoutBook(int bookId, int patronId, DateTime checkoutDate, DateTime dueDate, out string message)
    {
        var book = store.Data.Books.FirstOrDefault(x => x.Id == bookId);
        var patron = store.Data.Patrons.FirstOrDefault(x => x.Id == patronId);
        if (book is null || patron is null)
        {
            message = "Select a valid book and patron.";
            return false;
        }

        return library.CheckoutBook(book, patron, checkoutDate, dueDate, out message);
    }

    public decimal ReturnBook(int transactionId, DateTime returnDate)
    {
        var transaction = store.Data.Transactions.First(x => x.Id == transactionId);
        return library.ReturnBook(transaction, returnDate);
    }

    public decimal CalculateFine(DateTime dueDate, DateTime returnDate) =>
        library.CalculateFine(dueDate, returnDate);

    public IReadOnlyList<LoanTransaction> GetCheckedOutBooks() =>
        store.Data.Transactions.Where(x => !x.IsReturned).OrderBy(x => x.DueDate).ToList();

    public IReadOnlyList<LoanTransaction> GetOverdueBooks() =>
        store.Data.Transactions.Where(x => !x.IsReturned && x.DueDate.Date < DateTime.Today).OrderBy(x => x.DueDate).ToList();

    public IReadOnlyList<LoanTransaction> GetTransactionHistory() =>
        store.Data.Transactions.OrderByDescending(x => x.CheckoutDate).ToList();
}
