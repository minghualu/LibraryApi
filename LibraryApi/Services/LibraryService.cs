using Grpc.Core;
using LibraryApi.Data;
using LibraryApi.Grpc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Services
{
    public class LibraryServiceLogic : LibraryService.LibraryServiceBase
    {
        private readonly LibraryDbContext _db;
        public LibraryServiceLogic(LibraryDbContext db)
        {             
            _db = db;
        }

        // Get the most borrowed books, ordered by the number of times they were borrowed, and return the numberOfBooks
        public override async Task<BooksResponse> GetMostBorrowedBooksAsync(Empty request, ServerCallContext context)
        {
            int numberOfBooks = 3;
            // Find number of times each book was borrowed and order them by that number
            List<BookProto> mostBorrowedBooks = await _db.Borrows
                .GroupBy(borrow => new 
                {
                    borrow.BookId,
                    borrow.Book.BookTitle, 
                    borrow.Book.NumPages    
                })
                .Select(group => new BookProto
                { 
                    Id = group.Key.BookId,
                    Title = group.Key.BookTitle,
                    Pages = group.Key.NumPages,
                    BorrowCount = group.Count()
                })
                .OrderByDescending(book => book.BorrowCount)
                .Take(numberOfBooks)
                .ToListAsync();

            BooksResponse response = new BooksResponse();
            response.Books.AddRange(mostBorrowedBooks);

            return response;
        }
        public override async Task<BookAvailabilityResponse> GetBookAvailabilityAsync(BookRequest request, ServerCallContext context)
        {
            // Find the book with the given id and save the borrow info to calculate availability
            var book = await _db.Books
                .Include(book => book.BookBorrowInfo)
                .FirstOrDefaultAsync(book => book.Id == request.BookId);

            if (book == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Book not found"));

            // Find how many copies of the book are currently borrowed and available
            DateTime timeNow = DateTime.UtcNow;
            int numCurrentlyBorrowed = book.BookBorrowInfo.Count(b =>
                b.BorrowDate <= timeNow && b.ReturnDate >= timeNow
            );

            int numAvailable = book.NumCopies - numCurrentlyBorrowed;
            BookAvailabilityResponse response = new BookAvailabilityResponse 
                { 
                    Available = numAvailable, 
                    Borrowed = numCurrentlyBorrowed 
                };
            return response;
        }

        public override async Task<UsersResponse>GetTopBorrowerOfBookAsync(TimeFrameRequest request, ServerCallContext context)
        {
            DateTime startDate = request.StartDate.ToDateTime();
            DateTime endDate = request.EndDate.ToDateTime();

            int numberOfUsers = 3;
            // Filter current borrows and find user with the most borrows in the given time frame
            var topBorrowers = await _db.Borrows
                .Where(borrow => borrow.BorrowDate >= startDate && borrow.BorrowDate <= endDate)
                .GroupBy(borrow => new { borrow.UserId, borrow.User.UserName })
                .Select(group => new
                {
                    UserId = group.Key.UserId,
                    UserName = group.Key.UserName,
                    BorrowCount = group.Count()
                })
                .OrderByDescending(user => user.BorrowCount)
                .Take(numberOfUsers)
                .ToListAsync();

            UsersResponse response = new UsersResponse();
            response.Users.AddRange(
                topBorrowers.Select(user => new UserProto
                {
                    Id = user.UserId,
                    Name = user.UserName,
                    BorrowCount = user.BorrowCount
                })
            );

            return response;
        }
    }
}
