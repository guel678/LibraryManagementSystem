using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public sealed class BookService
{
    private readonly DataStore store;

    public BookService(DataStore store)
    {
        this.store = store;
    }

    public void AddBook(Book book)
    {
        book.Id = book.Id == 0 ? store.NextBookId() : book.Id;
        store.Data.Books.Add(book);
        store.Save();
    }

    public void UpdateBook(Book book) => store.Save();

    public bool DeleteBook(int bookId)
    {
        var book = store.Data.Books.FirstOrDefault(x => x.Id == bookId);
        if (book is null || store.Data.Transactions.Any(x => x.BookId == bookId && !x.IsReturned))
        {
            return false;
        }

        store.Data.Books.Remove(book);
        store.Save();
        return true;
    }

    public IReadOnlyList<Book> GetAllBooks() => store.Data.Books.OrderBy(x => x.Title).ToList();

    public IReadOnlyList<Book> SearchBooks(string keyword) =>
        store.Data.Books
            .Where(x => Contains(x.Title, keyword) || Contains(x.Author, keyword) || Contains(x.Isbn, keyword) || Contains(x.Genre, keyword))
            .OrderBy(x => x.Title)
            .ToList();

    public IReadOnlyList<Book> GetAvailableBooks() =>
        store.Data.Books.Where(x => x.AvailableCopies > 0).OrderBy(x => x.Title).ToList();

    private static bool Contains(string source, string value) =>
        source.Contains(value, StringComparison.OrdinalIgnoreCase);
}
