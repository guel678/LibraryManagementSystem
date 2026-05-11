using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LibraryManagementSystem.ViewModels;

public sealed class TransactionViewModel : ViewModelBase
{
    public ObservableCollection<TransactionRow> Transactions { get; } = new();
    public TransactionRow? SelectedTransaction { get; set; }
    public string SearchKeyword { get; set; } = "";
    public string ValidationMessage { get; set; } = "";
    public ICommand? AddCommand { get; set; }
    public ICommand? EditCommand { get; set; }
    public ICommand? DeleteCommand { get; set; }
    public ICommand? SearchCommand { get; set; }
    public ICommand? SaveCommand { get; set; }
    public ICommand? ClearCommand { get; set; }
    public ICommand? RefreshCommand { get; set; }
}
