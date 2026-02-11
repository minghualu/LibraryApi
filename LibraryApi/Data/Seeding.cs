using LibraryApi.Data;

namespace LibraryApi.Data
{
    public static class LibraryDbSeeder
    {
        public static void Seed(LibraryDbContext db)
        {
            // Do not populate the database if there are already data in it
            if (db.Books.Any())
                return;

            var books = new List<BookData>
        {
            new BookData { BookTitle = "Lord of the Rings", NumPages = 603, NumCopies = 5 },
            new BookData { BookTitle = "A Dance with Dragons", NumPages = 412, NumCopies = 3 },
            new BookData { BookTitle = "The Hobbit", NumPages = 310, NumCopies = 4 },
        };

            var users = new List<UserData>
        {
            new UserData { UserName = "Alice" },
            new UserData { UserName = "Bo" },
            new UserData { UserName = "Chenchen" },
        };

            db.Books.AddRange(books);
            db.Users.AddRange(users);
            db.SaveChanges();

            var borrows = new List<BorrowData>
        {
            new BorrowData
            {
                BookId = books[0].Id,
                UserId = users[0].Id,
                BorrowDate = DateTime.UtcNow.AddDays(-10),
                ReturnDate = DateTime.UtcNow.AddDays(-5)
            },
            new BorrowData
            {
                BookId = books[1].Id,
                UserId = users[0].Id,
                BorrowDate = DateTime.UtcNow.AddDays(-7),
                ReturnDate = DateTime.UtcNow.AddDays(-2)
            },
            new BorrowData
            {
                BookId = books[0].Id,
                UserId = users[1].Id,
                BorrowDate = DateTime.UtcNow.AddDays(-3),
                ReturnDate = DateTime.UtcNow.AddDays(-1)
            },
            new BorrowData
            {
                BookId = books[2].Id,
                UserId = users[2].Id,
                BorrowDate = DateTime.UtcNow.AddDays(-1),
                ReturnDate = DateTime.UtcNow.AddDays(4)
            },
            new BorrowData
            {
                BookId = books[1].Id,
                UserId = users[2].Id,
                BorrowDate = DateTime.UtcNow.AddDays(-2),
                ReturnDate = DateTime.UtcNow.AddDays(3)
            },
            new BorrowData
            {
                BookId = books[1].Id,
                UserId = users[1].Id,
                BorrowDate = DateTime.UtcNow.AddDays(-4),
                ReturnDate = DateTime.UtcNow.AddDays(1)
            }
        };

            db.Borrows.AddRange(borrows);
            db.SaveChanges();
        }
    }
}
