namespace LibraryManagementSystem.Models;

public enum MembershipType
{
    Standard,
    Premium
}

public enum PatronStatus
{
    Active,
    Inactive
}

public sealed class Patron
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string MembershipId { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-18);
    public MembershipType MembershipType { get; set; }
    public string Status { get; set; } = "Active";
    public List<LoanTransaction> Transactions { get; set; } = new();
    public List<Fine> Fines { get; set; } = new();

    public override string ToString() => string.IsNullOrWhiteSpace(FullName) ? "Unnamed patron" : FullName;
}
