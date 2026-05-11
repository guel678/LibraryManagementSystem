using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models;

public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Isbn { get; set; } = "";
    public string Genre { get; set; } = "";
    public string Publisher { get; set; } = "";
    public int PublishedYear { get; set; }
    public int Quantity { get; set; }
    public string Description { get; set; } = "";
    public int CheckedOutCount { get; set; }
    [NotMapped]
    public bool IsSelectedForCheckout { get; set; }
    public int AvailableCopies => Quantity - CheckedOutCount;
    public string Availability => AvailableCopies <= 0
        ? "Checked Out"
        : CheckedOutCount > 0
            ? "Partially Borrowed"
            : "Available";
    public List<LoanTransaction> Transactions { get; set; } = new();

    public override string ToString() => string.IsNullOrWhiteSpace(Title) ? "Untitled book" : Title;
}
