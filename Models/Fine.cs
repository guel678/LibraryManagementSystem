namespace LibraryManagementSystem.Models;

public sealed class Fine
{
    public int Id { get; set; }
    public int PatronId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DateApplied { get; set; } = DateTime.Today;
    public bool IsPaid { get; set; }
    public Patron? Patron { get; set; }
}
