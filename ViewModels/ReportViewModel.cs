using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LibraryManagementSystem.ViewModels;

public sealed class ReportViewModel : ViewModelBase
{
    public ObservableCollection<ReportRow> ReportRows { get; } = new();
    public ReportRow? SelectedReportRow { get; set; }
    public string SearchKeyword { get; set; } = "";
    public string ValidationMessage { get; set; } = "";
    public ICommand? SearchCommand { get; set; }
    public ICommand? SaveCommand { get; set; }
    public ICommand? ClearCommand { get; set; }
    public ICommand? RefreshCommand { get; set; }
}
