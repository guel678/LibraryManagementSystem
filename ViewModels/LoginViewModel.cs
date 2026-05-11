namespace LibraryManagementSystem.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    public string Username { get; set; } = "";
    public string ValidationMessage { get; set; } = "";
}
