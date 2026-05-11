namespace LibraryManagementSystem.Models;

public enum TransactionStatus
{
    CheckedOut,
    Returned,
    Overdue
}

public sealed class LoanTransaction
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int PatronId { get; set; }
    public DateTime CheckoutDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);
    public DateTime? ReturnDate { get; set; }
    public decimal FineAmount { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.CheckedOut;
    public Book? Book { get; set; }
    public Patron? Patron { get; set; }
    public bool IsReturned => ReturnDate.HasValue;
}
