namespace LibraryManagementSystem.Models;

public enum UserRole
{
    Admin,
    Librarian
}

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
