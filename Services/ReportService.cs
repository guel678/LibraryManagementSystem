using LibraryManagementSystem.ViewModels;

namespace LibraryManagementSystem.Services;

public sealed class ReportService
{
    private readonly DataStore store;
    private readonly LibraryService library;

    public ReportService(DataStore store)
    {
        this.store = store;
        library = new LibraryService(store);
    }

    public IReadOnlyList<ReportRow> GetOverdueBooksReport() =>
        BuildCheckedOutReport().Where(x => x.OverdueDays > 0).ToList();

    public IReadOnlyList<ReportRow> GetCheckedOutBooksReport() => BuildCheckedOutReport();

    public IReadOnlyList<TransactionRow> GetTransactionHistoryReport(DateTime startDate, DateTime endDate) =>
        BuildTransactionRows(store.Data.Transactions.Where(x => x.CheckoutDate.Date >= startDate.Date && x.CheckoutDate.Date <= endDate.Date)).ToList();

    public IReadOnlyList<PatronActivityRow> GetPatronActivityReport(int? patronId = null)
    {
        var today = DateTime.Today;
        var patrons = patronId.HasValue
            ? store.Data.Patrons.Where(x => x.Id == patronId.Value)
            : store.Data.Patrons;

        return patrons.Select(patron =>
        {
            var transactions = store.Data.Transactions.Where(x => x.PatronId == patron.Id).ToList();
            return new PatronActivityRow(
                patron.FullName,
                transactions.Count,
                transactions.Count(x => !x.IsReturned && x.DueDate.Date < today),
                transactions.Sum(x => x.FineAmount),
                store.Data.Fines.Where(x => x.PatronId == patron.Id && !x.IsPaid).Sum(x => x.Amount));
        }).ToList();
    }

    public void ExportToCsv(string path, IEnumerable<string[]> rows) =>
        CsvExportService.Export(path, rows);

    public void ExportToExcel(string path, IEnumerable<string[]> rows) =>
        CsvExportService.ExportExcel(path, rows);

    public void ExportToPdf(string path, IEnumerable<string[]> rows) =>
        CsvExportService.ExportPdf(path, rows);

    private IReadOnlyList<ReportRow> BuildCheckedOutReport()
    {
        var today = DateTime.Today;
        return BuildTransactionRows(store.Data.Transactions.Where(x => !x.IsReturned))
            .Select(x => new ReportRow(
                x.BookTitle,
                x.PatronName,
                x.DueDate,
                Math.Max(0, (today - x.DueDate.Date).Days),
                library.CalculateFine(x.DueDate, today)))
            .ToList();
    }

    private IEnumerable<TransactionRow> BuildTransactionRows(IEnumerable<Models.LoanTransaction> transactions)
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
}
