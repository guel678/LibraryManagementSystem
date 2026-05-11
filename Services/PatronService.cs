using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public sealed class PatronService
{
    private readonly DataStore store;

    public PatronService(DataStore store)
    {
        this.store = store;
    }

    public void AddPatron(Patron patron)
    {
        patron.Id = patron.Id == 0 ? store.NextPatronId() : patron.Id;
        store.Data.Patrons.Add(patron);
        store.Save();
    }

    public void UpdatePatron(Patron patron) => store.Save();

    public bool DeletePatron(int patronId)
    {
        var patron = store.Data.Patrons.FirstOrDefault(x => x.Id == patronId);
        if (patron is null || store.Data.Transactions.Any(x => x.PatronId == patronId && !x.IsReturned))
        {
            return false;
        }

        store.Data.Patrons.Remove(patron);
        store.Save();
        return true;
    }

    public IReadOnlyList<Patron> GetAllPatrons() => store.Data.Patrons.OrderBy(x => x.FullName).ToList();

    public IReadOnlyList<Patron> SearchPatrons(string keyword) =>
        store.Data.Patrons
            .Where(x => Contains(x.FullName, keyword) || Contains(x.MembershipId, keyword) || Contains(x.Email, keyword))
            .OrderBy(x => x.FullName)
            .ToList();

    public IReadOnlyList<LoanTransaction> GetPatronTransactions(int patronId) =>
        store.Data.Transactions.Where(x => x.PatronId == patronId).OrderByDescending(x => x.CheckoutDate).ToList();

    private static bool Contains(string source, string value) =>
        source.Contains(value, StringComparison.OrdinalIgnoreCase);
}
