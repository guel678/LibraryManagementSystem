using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public sealed class LibraryData
{
    public List<User> Users { get; set; } = new();
    public List<Book> Books { get; set; } = new();
    public List<Patron> Patrons { get; set; } = new();
    public List<LoanTransaction> Transactions { get; set; } = new();
    public List<Fine> Fines { get; set; } = new();
}
