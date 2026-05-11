using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.Win32;

namespace LibraryManagementSystem.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly DataStore store;
    private readonly LibraryService library;
    private string bookSearch = "";
    private string patronSearch = "";
    private string transactionSearch = "";
    private string reportSearch = "";
    private string userSearch = "";
    private string availabilityFilter = "All";
    private string membershipFilter = "All";
    private DateTime reportStartDate = DateTime.Today.AddMonths(-1);
    private DateTime reportEndDate = DateTime.Today;
    private Book? selectedBook;
    private Patron? selectedPatron;
    private Patron? selectedCheckoutPatron;
    private TransactionRow? selectedOpenTransaction;
    private FineRow? selectedFine;
    private string selectedLanguage = "English";
    private string pendingLanguage = "English";

    public MainViewModel(User currentUser)
        : this(currentUser, new DataStore())
    {
    }

    public MainViewModel(User currentUser, DataStore store)
    {
        CurrentUser = currentUser;
        this.store = store;
        library = new LibraryService(store);

        AddBookCommand = new RelayCommand(_ => AddBook());
        UpdateBookCommand = new RelayCommand(_ => SaveChanges());
        DeleteBookCommand = new RelayCommand(_ => DeleteBook());
        AddPatronCommand = new RelayCommand(_ => AddPatron());
        UpdatePatronCommand = new RelayCommand(_ => SaveChanges());
        DeletePatronCommand = new RelayCommand(_ => DeletePatron());
        CheckoutCommand = new RelayCommand(_ => Checkout());
        ReturnCommand = new RelayCommand(_ => ReturnSelected());
        AddUserCommand = new RelayCommand(_ => AddUser(), _ => IsAdmin);
        ResetPasswordCommand = new RelayCommand(_ => ResetPassword(), _ => IsAdmin);
        UpdateUserCommand = new RelayCommand(_ => SaveUserChanges(), _ => IsAdmin);
        DeleteUserCommand = new RelayCommand(_ => DeleteUser(), _ => IsAdmin);
        ExportReportCommand = new RelayCommand(_ => ExportReports());
        BackupCommand = new RelayCommand(_ => BackupData(), _ => IsAdmin);
        RestoreCommand = new RelayCommand(_ => RestoreData(), _ => IsAdmin);
        SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
        LogoutCommand = new RelayCommand(_ => Logout());
        MarkFinePaidCommand = new RelayCommand(_ => MarkSelectedFinePaid());

        CheckoutDate = DateTime.Today;
        DueDate = DateTime.Today.AddDays(14);
        NewUserRole = UserRole.Librarian;
        LoadSettings();
        ApplyLanguage();
        RefreshAll();
    }

    public User CurrentUser { get; }
    public bool IsAdmin => CurrentUser.Role == UserRole.Admin;
    public Visibility UserManagementVisibility => IsAdmin ? Visibility.Visible : Visibility.Collapsed;
    public string WindowTitle => "Library Management System";
    public string DisplayName => string.IsNullOrWhiteSpace(CurrentUser.FullName) ? CurrentUser.Username : CurrentUser.FullName;
    public string WelcomeMessage => SelectedLanguage == "Spanish" ? $"Bienvenido, {DisplayName}" : $"Welcome back, {DisplayName}";
    public string CurrentDateText => DateTime.Today.ToString("MMMM dd, yyyy | dddd");
    public string AccountText => CurrentUser.Role.ToString();
    public string ReportShowingText => SelectedLanguage == "Spanish"
        ? $"Mostrando 1 a {OverdueReport.Count} de {OverdueReport.Count} informes"
        : $"Showing 1 to {OverdueReport.Count} of {OverdueReport.Count} reports";
    public string UserFoundText => SelectedLanguage == "Spanish"
        ? $"{Users.Count} usuario{(Users.Count == 1 ? "" : "s")} encontrado{(Users.Count == 1 ? "" : "s")}"
        : $"{Users.Count} user{(Users.Count == 1 ? "" : "s")} found";

    public ObservableCollection<Book> Books { get; } = new();
    public ObservableCollection<Patron> Patrons { get; } = new();
    public ObservableCollection<TransactionRow> Transactions { get; } = new();
    public ObservableCollection<TransactionRow> OpenTransactions { get; } = new();
    public ObservableCollection<TransactionRow> PatronHistory { get; } = new();
    public ObservableCollection<ReportRow> OverdueReport { get; } = new();
    public ObservableCollection<ReportRow> CheckedOutReport { get; } = new();
    public ObservableCollection<PatronActivityRow> PatronActivityReport { get; } = new();
    public ObservableCollection<FineRow> Fines { get; } = new();
    public ObservableCollection<User> Users { get; } = new();

    public IEnumerable<string> AvailabilityOptions { get; } = new[] { "All", "Available", "Checked Out" };
    public IEnumerable<string> MembershipOptions { get; } = new[] { "All", "Standard", "Premium", "Active", "Inactive" };
    public IEnumerable<UserRole> UserRoles { get; } = Enum.GetValues<UserRole>();
    public IEnumerable<MembershipType> MembershipTypes { get; } = Enum.GetValues<MembershipType>();
    public IEnumerable<string> PatronStatuses { get; } = new[] { "Active", "Inactive" };
    public IEnumerable<string> LanguageOptions { get; } = new[] { "English", "Spanish" };

    public Book? SelectedBook
    {
        get => selectedBook;
        set { selectedBook = value; OnPropertyChanged(); }
    }
    public Patron? SelectedPatron
    {
        get => selectedPatron;
        set
        {
            selectedPatron = value;
            OnPropertyChanged();
            RefreshPatronHistory();
        }
    }
    public Book? SelectedCheckoutBook { get; set; }
    public Patron? SelectedCheckoutPatron
    {
        get => selectedCheckoutPatron;
        set
        {
            selectedCheckoutPatron = value;
            OnPropertyChanged();
            RefreshOpenTransactionsForSelectedPatron();
        }
    }

    public TransactionRow? SelectedOpenTransaction
    {
        get => selectedOpenTransaction;
        set { selectedOpenTransaction = value; OnPropertyChanged(); }
    }
    public User? SelectedUser { get; set; }
    public FineRow? SelectedFine
    {
        get => selectedFine;
        set { selectedFine = value; OnPropertyChanged(); }
    }

    public DateTime CheckoutDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime ReportStartDate
    {
        get => reportStartDate;
        set { reportStartDate = value.Date; OnPropertyChanged(); RefreshTransactions(); RefreshReports(); }
    }

    public DateTime ReportEndDate
    {
        get => reportEndDate;
        set { reportEndDate = value.Date; OnPropertyChanged(); RefreshTransactions(); RefreshReports(); }
    }

    public string NewUsername { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public UserRole NewUserRole { get; set; }
    public string ResetPasswordValue { get; set; } = "";
    public string SelectedLanguage
    {
        get => selectedLanguage;
        set
        {
            selectedLanguage = value;
            OnPropertyChanged();
        }
    }

    public string PendingLanguage
    {
        get => pendingLanguage;
        set { pendingLanguage = value; OnPropertyChanged(); }
    }

    public string DatabaseLocation => store.DatabasePath;
    public string RuntimeInfo => $".NET {Environment.Version} | {RuntimeInformation.ProcessArchitecture}";
    public string SettingsVisibilityNote => SelectedLanguage == "Spanish"
        ? IsAdmin ? "La configuración de administrador está habilitada." : "Cuenta bibliotecaria: el respaldo del sistema y la administración de usuarios están restringidos."
        : IsAdmin ? "Admin settings are enabled." : "Librarian account: system backup and user management are restricted.";
    private string SettingsFilePath => Path.Combine(Path.GetDirectoryName(store.DatabasePath)!, "settings.txt");

    public string BookSearch
    {
        get => bookSearch;
        set { bookSearch = value; OnPropertyChanged(); RefreshBooks(); }
    }

    public string PatronSearch
    {
        get => patronSearch;
        set { patronSearch = value; OnPropertyChanged(); RefreshPatrons(); }
    }

    public string TransactionSearch
    {
        get => transactionSearch;
        set { transactionSearch = value; OnPropertyChanged(); RefreshTransactions(); }
    }

    public string ReportSearch
    {
        get => reportSearch;
        set { reportSearch = value; OnPropertyChanged(); RefreshReports(); }
    }

    public string UserSearch
    {
        get => userSearch;
        set { userSearch = value; OnPropertyChanged(); RefreshUsers(); }
    }

    public string AvailabilityFilter
    {
        get => availabilityFilter;
        set { availabilityFilter = value; OnPropertyChanged(); RefreshBooks(); }
    }

    public string MembershipFilter
    {
        get => membershipFilter;
        set { membershipFilter = value; OnPropertyChanged(); RefreshPatrons(); }
    }

    public ICommand AddBookCommand { get; }
    public ICommand UpdateBookCommand { get; }
    public ICommand DeleteBookCommand { get; }
    public ICommand AddPatronCommand { get; }
    public ICommand UpdatePatronCommand { get; }
    public ICommand DeletePatronCommand { get; }
    public ICommand CheckoutCommand { get; }
    public ICommand ReturnCommand { get; }
    public ICommand AddUserCommand { get; }
    public ICommand ResetPasswordCommand { get; }
    public ICommand UpdateUserCommand { get; }
    public ICommand DeleteUserCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand BackupCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand MarkFinePaidCommand { get; }

    public int BookTitles => store.Data.Books.Count;
    public int TotalBooks => store.Data.Books.Sum(x => x.Quantity);
    public int AvailableBooks => store.Data.Books.Sum(x => x.AvailableCopies);
    public int TotalPatrons => store.Data.Patrons.Count(x => string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase));
    public int OpenLoans => store.Data.Transactions.Count(x => !x.IsReturned);
    public int OverdueLoans => store.Data.Transactions.Count(x => !x.IsReturned && x.DueDate.Date < DateTime.Today);
    public decimal TotalFines => store.Data.Transactions.Sum(x => x.FineAmount);
    public decimal UnpaidFines => store.Data.Fines.Where(x => !x.IsPaid).Sum(x => x.Amount);

    private void AddBook()
    {
        var nextId = store.NextBookId();
        var book = new Book
        {
            Id = nextId,
            Title = $"New Book {nextId}",
            Author = "Unknown",
            Isbn = $"TEMP-{nextId:0000}",
            Quantity = 1,
            PublishedYear = DateTime.Today.Year
        };
        store.Data.Books.Add(book);
        SelectedBook = book;
        BookSearch = "";
        AvailabilityFilter = "All";
        SaveChanges(book.Id);
    }

    private void DeleteBook()
    {
        if (SelectedBook is null) return;
        var selectedBookId = SelectedBook.Id;
        store.Load();
        if (store.Data.Transactions.Any(x => x.BookId == SelectedBook.Id && !x.IsReturned))
        {
            MessageBox.Show("Cannot delete a book with active checkouts.");
            return;
        }

        var book = store.Data.Books.FirstOrDefault(x => x.Id == selectedBookId);
        if (book is null) return;
        store.Data.Books.Remove(book);
        SaveChanges();
    }

    private void AddPatron()
    {
        var nextId = store.NextPatronId();
        var patron = new Patron
        {
            Id = nextId,
            FullName = $"New Patron {nextId}",
            MembershipId = $"M-{nextId:0000}",
            MembershipType = MembershipType.Standard,
            Status = "Active"
        };
        store.Data.Patrons.Add(patron);
        SelectedPatron = patron;
        PatronSearch = "";
        MembershipFilter = "All";
        SaveChanges(patronId: patron.Id);
    }

    private void DeletePatron()
    {
        if (SelectedPatron is null) return;
        var selectedPatronId = SelectedPatron.Id;
        store.Load();
        if (store.Data.Transactions.Any(x => x.PatronId == selectedPatronId && !x.IsReturned))
        {
            MessageBox.Show("Cannot delete a patron with active checkouts.");
            return;
        }

        var patron = store.Data.Patrons.FirstOrDefault(x => x.Id == selectedPatronId);
        if (patron is null) return;
        store.Data.Patrons.Remove(patron);
        SaveChanges();
    }

    private void Checkout()
    {
        var selectedBooks = Books.Where(x => x.IsSelectedForCheckout).ToList();
        if (selectedBooks.Count == 0 && SelectedCheckoutBook is not null)
        {
            selectedBooks.Add(SelectedCheckoutBook);
        }

        if (selectedBooks.Count == 0 || SelectedCheckoutPatron is null)
        {
            MessageBox.Show("Select a patron and at least one available book.");
            return;
        }

        if (DueDate.Date <= CheckoutDate.Date)
        {
            MessageBox.Show("Due date must be after checkout date.");
            return;
        }

        var checkedOut = 0;
        var messages = new List<string>();
        foreach (var book in selectedBooks)
        {
            if (library.CheckoutBook(book, SelectedCheckoutPatron, CheckoutDate, DueDate, out var message))
            {
                checkedOut++;
                book.IsSelectedForCheckout = false;
            }
            else
            {
                messages.Add($"{book.Title}: {message}");
            }
        }

        RefreshAll();
        MessageBox.Show(messages.Count == 0
            ? $"{checkedOut} book{(checkedOut == 1 ? "" : "s")} checked out successfully."
            : $"{checkedOut} book{(checkedOut == 1 ? "" : "s")} checked out.\n{string.Join("\n", messages)}");
    }

    private void ReturnSelected()
    {
        if (SelectedOpenTransaction is null) return;
        var transaction = store.Data.Transactions.First(x => x.Id == SelectedOpenTransaction.Id);
        var fine = library.ReturnBook(transaction, DateTime.Today);
        RefreshAll();
        MessageBox.Show(fine > 0 ? $"Book returned. Fine: ₱{fine:N2}" : "Book returned. No fine.");
    }

    private void AddUser()
    {
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword))
        {
            MessageBox.Show("Enter a username and password.");
            return;
        }

        if (store.Data.Users.Any(x => x.Username.Equals(NewUsername, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Username already exists.");
            return;
        }

        store.Data.Users.Add(new User
        {
            Id = store.NextUserId(),
            Username = NewUsername.Trim(),
            PasswordHash = PasswordService.Hash(NewPassword),
            Role = NewUserRole
        });
        NewUsername = "";
        NewPassword = "";
        SaveChanges();
        OnPropertyChanged(nameof(NewUsername));
        OnPropertyChanged(nameof(NewPassword));
    }

    private void ResetPassword()
    {
        if (SelectedUser is null || string.IsNullOrWhiteSpace(ResetPasswordValue))
        {
            MessageBox.Show("Select a user and enter a new password.");
            return;
        }

        SelectedUser.PasswordHash = PasswordService.Hash(ResetPasswordValue);
        ResetPasswordValue = "";
        SaveChanges();
        OnPropertyChanged(nameof(ResetPasswordValue));
        MessageBox.Show("Password reset successfully.");
    }

    private void SaveUserChanges()
    {
        if (SelectedUser is not null && WouldLeaveNoActiveAdmin(SelectedUser))
        {
            MessageBox.Show("At least one active admin account is required.");
            RefreshUsers();
            return;
        }

        SaveChanges();
        MessageBox.Show("User changes saved.");
    }

    private void DeleteUser()
    {
        if (SelectedUser is null) return;
        if (SelectedUser.Id == CurrentUser.Id)
        {
            MessageBox.Show("You cannot delete the account you are currently using.");
            return;
        }

        if (SelectedUser.Role == UserRole.Admin
            && SelectedUser.IsActive
            && !HasAnotherActiveAdmin(SelectedUser))
        {
            MessageBox.Show("At least one active admin account is required.");
            return;
        }

        store.Data.Users.Remove(SelectedUser);
        SaveChanges();
    }

    private void SaveChanges(int? bookId = null, int? patronId = null)
    {
        if (!ValidateEditableData())
        {
            return;
        }

        try
        {
            store.Save();
            RefreshAll();
            SelectBook(bookId);
            SelectPatron(patronId);
        }
        catch (Exception ex)
        {
            store.Load();
            RefreshAll();
            SelectBook(bookId);
            SelectPatron(patronId);
            MessageBox.Show($"Changes could not be saved: {GetErrorMessage(ex)}");
        }
    }

    private void SelectBook(int? bookId)
    {
        if (!bookId.HasValue)
        {
            return;
        }

        SelectedBook = Books.FirstOrDefault(x => x.Id == bookId.Value);
    }

    private void SelectPatron(int? patronId)
    {
        if (!patronId.HasValue)
        {
            return;
        }

        SelectedPatron = Patrons.FirstOrDefault(x => x.Id == patronId.Value);
    }

    private static string GetErrorMessage(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message) && !messages.Contains(current.Message))
            {
                messages.Add(current.Message);
            }
        }

        return string.Join(Environment.NewLine, messages);
    }

    private void ExportReports()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xls)|*.xls|PDF Files (*.pdf)|*.pdf",
            FileName = "library-report.csv"
        };
        if (dialog.ShowDialog() != true) return;

        var rows = new List<string[]> { new[] { "Report", "Book", "Patron", "Due Date", "Return Date", "Fine" } };
        rows.AddRange(BuildTransactionRows(GetDateFilteredTransactions(store.Data.Transactions)).Select(x => new[]
        {
            x.IsReturned ? "Transaction History" : "Checked Out",
            x.BookTitle,
            x.PatronName,
            x.DueDate.ToShortDateString(),
            x.ReturnDate?.ToShortDateString() ?? "",
            x.FineAmount.ToString("0.00")
        }));

        var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
        if (extension == ".xls")
        {
            CsvExportService.ExportExcel(dialog.FileName, rows);
        }
        else if (extension == ".pdf")
        {
            CsvExportService.ExportPdf(dialog.FileName, rows);
        }
        else
        {
            CsvExportService.Export(dialog.FileName, rows);
        }
        MessageBox.Show("Report exported successfully.");
    }

    private void MarkSelectedFinePaid()
    {
        if (SelectedFine is null)
        {
            MessageBox.Show("Select an unpaid fine first.");
            return;
        }

        if (SelectedFine.IsPaid)
        {
            MessageBox.Show("This fine is already marked as paid.");
            return;
        }

        library.MarkFinePaid(SelectedFine.Id);
        RefreshAll();
        MessageBox.Show("Fine marked as paid.");
    }

    private void SaveSettings()
    {
        SelectedLanguage = LanguageOptions.Contains(PendingLanguage) ? PendingLanguage : "English";
        ApplyLanguage();
        SaveSettingsFile();
        OnPropertyChanged(nameof(CurrentDateText));
        OnPropertyChanged(nameof(WelcomeMessage));
        OnPropertyChanged(nameof(ReportShowingText));
        OnPropertyChanged(nameof(UserFoundText));
        OnPropertyChanged(nameof(SettingsVisibilityNote));
        RefreshAll();
        MessageBox.Show(SelectedLanguage == "Spanish" ? "Configuración guardada." : "Settings saved.");
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return;
            }

            var language = File.ReadAllText(SettingsFilePath).Trim();
            if (!LanguageOptions.Contains(language))
            {
                return;
            }

            selectedLanguage = language;
            pendingLanguage = language;
            OnPropertyChanged(nameof(SelectedLanguage));
            OnPropertyChanged(nameof(PendingLanguage));
        }
        catch
        {
            selectedLanguage = "English";
            pendingLanguage = "English";
        }
    }

    private void SaveSettingsFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
        File.WriteAllText(SettingsFilePath, SelectedLanguage);
    }

    private void BackupData()
    {
        var dialog = new SaveFileDialog { Filter = "SQLite Database Backup (*.db)|*.db", FileName = $"library-backup-{DateTime.Today:yyyyMMdd}.db" };
        if (dialog.ShowDialog() != true) return;

        store.Save();
        File.Copy(store.DatabasePath, dialog.FileName, true);
        MessageBox.Show("Backup created successfully.");
    }

    private void RestoreData()
    {
        var dialog = new OpenFileDialog { Filter = "SQLite Database Backup (*.db)|*.db|All Files (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;

        File.Copy(dialog.FileName, store.DatabasePath, true);
        store.Load();
        RefreshAll();
        MessageBox.Show("Backup restored successfully.");
    }

    private void Logout()
    {
        var loginWindow = new LoginWindow();
        Application.Current.MainWindow = loginWindow;
        loginWindow.Show();

        foreach (Window window in Application.Current.Windows.Cast<Window>().ToList())
        {
            if (window is MainWindow)
            {
                window.Close();
                break;
            }
        }
    }

    private void RefreshAll()
    {
        RefreshBooks();
        RefreshPatrons();
        RefreshTransactions();
        RefreshPatronHistory();
        RefreshReports();
        RefreshFines();
        RefreshUsers();
        OnPropertyChanged(nameof(BookTitles));
        OnPropertyChanged(nameof(TotalBooks));
        OnPropertyChanged(nameof(AvailableBooks));
        OnPropertyChanged(nameof(TotalPatrons));
        OnPropertyChanged(nameof(OpenLoans));
        OnPropertyChanged(nameof(OverdueLoans));
        OnPropertyChanged(nameof(TotalFines));
        OnPropertyChanged(nameof(UnpaidFines));
    }

    private void RefreshBooks()
    {
        Books.Clear();
        var query = store.Data.Books.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(BookSearch))
        {
            query = query.Where(x => Contains(x.Title, BookSearch) || Contains(x.Author, BookSearch) || Contains(x.Isbn, BookSearch) || Contains(x.Genre, BookSearch));
        }

        if (AvailabilityFilter != "All")
        {
            query = AvailabilityFilter switch
            {
                "Available" => query.Where(x => x.AvailableCopies > 0),
                "Checked Out" => query.Where(x => x.CheckedOutCount > 0),
                _ => query
            };
        }

        foreach (var book in query.OrderBy(x => x.Title))
        {
            Books.Add(book);
        }
    }

    private void RefreshPatrons()
    {
        Patrons.Clear();
        var query = store.Data.Patrons.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(PatronSearch))
        {
            query = query.Where(x => Contains(x.FullName, PatronSearch) || Contains(x.MembershipId, PatronSearch) || Contains(x.Email, PatronSearch));
        }

        query = MembershipFilter switch
        {
            "Standard" => query.Where(x => x.MembershipType == MembershipType.Standard),
            "Premium" => query.Where(x => x.MembershipType == MembershipType.Premium),
            "Active" => query.Where(x => x.Status == "Active"),
            "Inactive" => query.Where(x => x.Status == "Inactive"),
            _ => query
        };

        foreach (var patron in query.OrderBy(x => x.FullName))
        {
            Patrons.Add(patron);
        }
    }

    private void RefreshTransactions()
    {
        Transactions.Clear();
        OpenTransactions.Clear();
        var rows = BuildTransactionRows(GetDateFilteredTransactions(store.Data.Transactions));
        if (!string.IsNullOrWhiteSpace(TransactionSearch))
        {
            rows = rows.Where(x => Contains(x.BookTitle, TransactionSearch) || Contains(x.PatronName, TransactionSearch) || Contains(x.CheckoutDate.ToShortDateString(), TransactionSearch));
        }

        foreach (var row in rows.OrderByDescending(x => x.CheckoutDate))
        {
            Transactions.Add(row);
            if (!row.IsReturned && IsOpenTransactionVisibleForSelectedPatron(row))
            {
                OpenTransactions.Add(row);
            }
        }

        SelectDefaultOpenTransaction();
    }

    private void RefreshPatronHistory()
    {
        PatronHistory.Clear();
        if (SelectedPatron is null)
        {
            return;
        }

        var rows = BuildTransactionRows(store.Data.Transactions.Where(x => x.PatronId == SelectedPatron.Id))
            .OrderByDescending(x => x.CheckoutDate);

        foreach (var row in rows)
        {
            PatronHistory.Add(row);
        }
    }

    private void RefreshOpenTransactionsForSelectedPatron()
    {
        store.Load();
        OpenTransactions.Clear();

        var rows = BuildTransactionRows(store.Data.Transactions)
            .Where(x => !x.IsReturned && IsOpenTransactionVisibleForSelectedPatron(x))
            .OrderBy(x => x.DueDate);

        foreach (var row in rows)
        {
            OpenTransactions.Add(row);
        }

        SelectDefaultOpenTransaction();
        RefreshReports();
        OnPropertyChanged(nameof(OpenLoans));
        OnPropertyChanged(nameof(OverdueLoans));
    }

    private void RefreshReports()
    {
        OverdueReport.Clear();
        CheckedOutReport.Clear();
        PatronActivityReport.Clear();
        var today = DateTime.Today;
        var openRows = BuildTransactionRows(GetDateFilteredTransactions(store.Data.Transactions).Where(x => !x.IsReturned))
            .Where(MatchesReportFilters)
            .OrderBy(x => x.DueDate);

        foreach (var row in openRows)
        {
            var currentFine = row.IsReturned ? row.FineAmount : library.CalculateFine(row.DueDate, today);
            var overdueDays = Math.Max(0, (today - row.DueDate.Date).Days);
            var report = new ReportRow(row.BookTitle, row.PatronName, row.DueDate, overdueDays, currentFine, overdueDays > 0 ? "Overdue" : "Open");
            CheckedOutReport.Add(report);
            if (row.DueDate.Date < today)
            {
                OverdueReport.Add(report);
            }
        }

        foreach (var patron in store.Data.Patrons.OrderBy(x => x.FullName))
        {
            var patronTransactions = GetDateFilteredTransactions(store.Data.Transactions).Where(x => x.PatronId == patron.Id).ToList();
            PatronActivityReport.Add(new PatronActivityRow(
                patron.FullName,
                patronTransactions.Count,
                patronTransactions.Count(x => !x.IsReturned && x.DueDate.Date < today),
                patronTransactions.Sum(x => x.FineAmount),
                store.Data.Fines.Where(x => x.PatronId == patron.Id && !x.IsPaid).Sum(x => x.Amount)));
        }

        OnPropertyChanged(nameof(ReportShowingText));
    }

    private void RefreshFines()
    {
        Fines.Clear();
        foreach (var fine in store.Data.Fines.OrderByDescending(x => x.DateApplied))
        {
            var patron = store.Data.Patrons.FirstOrDefault(x => x.Id == fine.PatronId);
            Fines.Add(new FineRow(fine.Id, fine.PatronId, patron?.FullName ?? "Unknown Patron", fine.Amount, fine.DateApplied, fine.IsPaid));
        }
    }

    private void RefreshUsers()
    {
        Users.Clear();
        var query = store.Data.Users.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(UserSearch))
        {
            query = query.Where(x => Contains(x.Username, UserSearch) || Contains(x.FullName, UserSearch) || Contains(x.Role.ToString(), UserSearch));
        }

        foreach (var user in query.OrderBy(x => x.Username))
        {
            Users.Add(user);
        }

        OnPropertyChanged(nameof(UserFoundText));
    }

    private IEnumerable<TransactionRow> BuildTransactionRows(IEnumerable<LoanTransaction> transactions)
    {
        return transactions.Select(x =>
        {
            var book = store.Data.Books.FirstOrDefault(b => b.Id == x.BookId);
            var patron = store.Data.Patrons.FirstOrDefault(p => p.Id == x.PatronId);
            return new TransactionRow(
                x.Id,
                x.BookId,
                x.PatronId,
                book?.Title ?? "Unknown Book",
                patron?.FullName ?? "Unknown Patron",
                x.CheckoutDate,
                x.DueDate,
                x.ReturnDate,
                x.FineAmount);
        });
    }

    private static bool Contains(string source, string value) =>
        source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private bool MatchesReportFilters(TransactionRow row)
    {
        return string.IsNullOrWhiteSpace(ReportSearch)
            || Contains(row.BookTitle, ReportSearch)
            || Contains(row.PatronName, ReportSearch)
            || Contains(row.DueDate.ToShortDateString(), ReportSearch);
    }

    private IEnumerable<LoanTransaction> GetDateFilteredTransactions(IEnumerable<LoanTransaction> transactions)
    {
        var start = ReportStartDate.Date;
        var end = ReportEndDate.Date;
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return transactions.Where(x => x.CheckoutDate.Date >= start && x.CheckoutDate.Date <= end);
    }

    private void ApplyLanguage()
    {
        var cultureName = SelectedLanguage == "Spanish" ? "es-ES" : "en-PH";
        var culture = new CultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        var dictionaryName = SelectedLanguage == "Spanish" ? "Strings.es.xaml" : "Strings.en.xaml";
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var currentDictionary = dictionaries.FirstOrDefault(x =>
            x.Source is not null
            && x.Source.OriginalString.StartsWith("Resources/Strings.", StringComparison.OrdinalIgnoreCase));

        var newDictionary = new ResourceDictionary
        {
            Source = new Uri($"Resources/{dictionaryName}", UriKind.Relative)
        };

        if (currentDictionary is null)
        {
            dictionaries.Insert(0, newDictionary);
        }
        else
        {
            var index = dictionaries.IndexOf(currentDictionary);
            dictionaries[index] = newDictionary;
        }
    }

    private bool ValidateEditableData()
    {
        foreach (var book in store.Data.Books)
        {
            book.Title = book.Title.Trim();
            book.Author = book.Author.Trim();
            book.Isbn = book.Isbn.Trim();
            book.Genre = book.Genre.Trim();
            book.Publisher = book.Publisher.Trim();
            book.Description = book.Description.Trim();

            if (string.IsNullOrWhiteSpace(book.Title) || string.IsNullOrWhiteSpace(book.Author) || string.IsNullOrWhiteSpace(book.Isbn))
            {
                MessageBox.Show("Each book must have a title, author, and ISBN.");
                return false;
            }

            if (book.PublishedYear < 1000 || book.PublishedYear > DateTime.Today.Year + 1)
            {
                MessageBox.Show($"Invalid published year for \"{book.Title}\".");
                return false;
            }

            if (book.Quantity < book.CheckedOutCount)
            {
                MessageBox.Show($"Copies for \"{book.Title}\" cannot be lower than the number currently borrowed.");
                return false;
            }

            if (book.Quantity < 0)
            {
                MessageBox.Show($"Copies for \"{book.Title}\" cannot be negative.");
                return false;
            }
        }

        var duplicateIsbn = store.Data.Books
            .GroupBy(x => x.Isbn, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicateIsbn is not null)
        {
            MessageBox.Show($"Duplicate ISBN found: {duplicateIsbn.Key}");
            return false;
        }

        foreach (var patron in store.Data.Patrons)
        {
            patron.FullName = patron.FullName.Trim();
            patron.MembershipId = patron.MembershipId.Trim();
            patron.Email = patron.Email.Trim();
            patron.PhoneNumber = patron.PhoneNumber.Trim();
            patron.Address = patron.Address.Trim();
            patron.Status = PatronStatuses.Contains(patron.Status) ? patron.Status : "Active";

            if (string.IsNullOrWhiteSpace(patron.FullName) || string.IsNullOrWhiteSpace(patron.MembershipId))
            {
                MessageBox.Show("Each patron must have a full name and membership ID.");
                return false;
            }
        }

        var duplicateMembership = store.Data.Patrons
            .GroupBy(x => x.MembershipId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicateMembership is not null)
        {
            MessageBox.Show($"Duplicate membership ID found: {duplicateMembership.Key}");
            return false;
        }

        foreach (var user in store.Data.Users)
        {
            user.Username = user.Username.Trim();
            user.FullName = user.FullName.Trim();
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                MessageBox.Show("Each user must have a username.");
                return false;
            }
        }

        var duplicateUsername = store.Data.Users
            .GroupBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicateUsername is not null)
        {
            MessageBox.Show($"Duplicate username found: {duplicateUsername.Key}");
            return false;
        }

        if (!store.Data.Users.Any(x => x.Role == UserRole.Admin && x.IsActive))
        {
            MessageBox.Show("At least one active admin account is required.");
            return false;
        }

        return true;
    }

    private bool WouldLeaveNoActiveAdmin(User editedUser)
    {
        return editedUser.Role != UserRole.Admin || !editedUser.IsActive
            ? !HasAnotherActiveAdmin(editedUser)
            : false;
    }

    private bool HasAnotherActiveAdmin(User user) =>
        store.Data.Users.Any(x => x.Id != user.Id && x.Role == UserRole.Admin && x.IsActive);

    private bool IsOpenTransactionVisibleForSelectedPatron(TransactionRow row) =>
        SelectedCheckoutPatron is null || row.PatronId == SelectedCheckoutPatron.Id;

    private void SelectDefaultOpenTransaction()
    {
        SelectedOpenTransaction = OpenTransactions.Count == 1 ? OpenTransactions[0] : null;
    }
}

public sealed record TransactionRow(int Id, int BookId, int PatronId, string BookTitle, string PatronName, DateTime CheckoutDate, DateTime DueDate, DateTime? ReturnDate, decimal FineAmount)
{
    public bool IsReturned => ReturnDate.HasValue;
    public string Status => IsReturned ? "Returned" : DueDate.Date < DateTime.Today ? "Overdue" : "Checked Out";
    public string DisplayReturnDate => ReturnDate.HasValue ? ReturnDate.Value.ToString("MMM d, yyyy") : "—";
    public string DisplayFine => FineAmount > 0 ? $"₱{FineAmount:N2}" : "₱0.00";
}

public sealed record ReportRow(string BookTitle, string PatronName, DateTime DueDate, int OverdueDays, decimal FineAmount, string Status = "Open");
public sealed record PatronActivityRow(string PatronName, int TotalCheckouts, int OpenOverdues, decimal TotalFines, decimal UnpaidFines);
public sealed record FineRow(int Id, int PatronId, string PatronName, decimal Amount, DateTime DateApplied, bool IsPaid)
{
    public string Status => IsPaid ? "Paid" : "Unpaid";
}
