using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public sealed class AuthService
{
    private readonly DataStore store;

    public AuthService(DataStore store)
    {
        this.store = store;
    }

    public User? CurrentUser { get; private set; }

    public User? Login(string username, string password)
    {
        CurrentUser = store.Data.Users.FirstOrDefault(x =>
            x.IsActive &&
            x.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase) &&
            VerifyPassword(password, x.PasswordHash));
        return CurrentUser;
    }

    public void Logout() => CurrentUser = null;

    public string HashPassword(string password) => PasswordService.Hash(password);

    public bool VerifyPassword(string password, string hash) =>
        PasswordService.Verify(password, hash);

    public bool IsAdmin() => CurrentUser?.Role == UserRole.Admin;

    public bool IsLibrarian() => CurrentUser?.Role == UserRole.Librarian;

    public void ResetPassword(User user, string newPassword)
    {
        user.PasswordHash = HashPassword(newPassword);
        store.Save();
    }
}
