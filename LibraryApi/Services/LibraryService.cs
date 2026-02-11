using Grpc.Core;
using LibraryApi.Data;
using LibraryApi.Grpc;
using Microsoft.EntityFrameworkCore;
using System.Net;

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

        public override async Task<UsersResponse> GetTopBorrowerOfBookAsync(TimeFrameRequest request, ServerCallContext context)
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

        public override async Task<BooksResponse> GetUserBorrowedBooksAsync(UserTimeFrameRequest request, ServerCallContext context)
        {
            DateTime startDate = request.StartDate.ToDateTime();
            DateTime endDate = request.EndDate.ToDateTime();

            // Find books borrowed by this user in the given time frame
            var borrowedBooks = await _db.Borrows
                .Where(borrow => borrow.UserId == request.UserId && borrow.BorrowDate >= startDate && borrow.BorrowDate <= endDate)
                .Include(borrow => borrow.Book)
                .ToListAsync();

            // Calculate borrow count
            var groupedBorrows = borrowedBooks
                .GroupBy(borrow => new { borrow.BookId, borrow.Book.BookTitle, borrow.Book.NumPages })
                .Select(group => new BookProto
                {
                    Id = group.Key.BookId,
                    Title = group.Key.BookTitle,
                    Pages = group.Key.NumPages,
                    BorrowCount = group.Count()
                });

            BooksResponse response = new BooksResponse();
            response.Books.AddRange(groupedBorrows);

            return response;
        }

        public override async Task<BooksResponse> GetOtherBorrowedBooksAsync(BookRequest request, ServerCallContext context) 
        {
            // Find users who borrowed this book and then find other books they borrowed with joins
            var otherBorrowedBooks = await _db.Borrows
                .Where(borrow1 => _db.Borrows
                                .Where(borrow2 => borrow2.BookId == request.BookId)
                                .Select(borrow2 => borrow2.UserId)
                                .Contains(borrow1.UserId)
                           && borrow1.BookId != request.BookId)
                .Include(borrow => borrow.Book)
                .GroupBy(borrow => new { borrow.BookId, borrow.Book.BookTitle, borrow.Book.NumPages })
                .Select(group => new BookProto
                {
                    Id = group.Key.BookId,
                    Title = group.Key.BookTitle,
                    Pages = group.Key.NumPages,
                    BorrowCount = group.Count()
                })
                .ToListAsync();

            BooksResponse response = new BooksResponse();
            response.Books.AddRange(otherBorrowedBooks);

            return response;
        }

        public override async Task<ReadRateResponse> GetBookReadRateAsync(BookRequest request, ServerCallContext context) 
        {
            DateTime timeNow = DateTime.UtcNow;

            // Find the book and all of its borrows
            var book = await _db.Books
                .Include(book => book.BookBorrowInfo)
                .FirstOrDefaultAsync(book => book.Id == request.BookId);

            // Only include borrows where book is returned
            // var completedBorrows = book.BookBorrowInfo
            //    .Where(b => b.ReturnDate <= timeNow)
            //    .ToList();
            if (book == null) 
            {
                var earlyResponse = new ReadRateResponse();
                earlyResponse.PagesPerDay = 0;
                return earlyResponse;
            }

            // Calculate the read rate and average them. If book is returned same day, assume one day read time
            var readRates = book.BookBorrowInfo.Select(borrow =>
                {
                    var durationInDays = (borrow.ReturnDate - borrow.BorrowDate).TotalDays;
                    if (durationInDays <= 1) durationInDays = 1;
                    return book.NumPages / durationInDays;
                });

            var response = new ReadRateResponse();
            response.PagesPerDay = readRates.Average();

            return response;
        }
    }
}
