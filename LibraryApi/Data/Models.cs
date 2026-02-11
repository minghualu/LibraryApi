using System.Collections.Generic;

namespace LibraryApi.Data
{
    public class BookData
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public int NumPages { get; set; }
        public int NumCopies { get; set; }
        public ICollection<BorrowData> BookBorrowInfo { get; set; } = [];
    }

    public class UserData
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public ICollection<BorrowData> UserBorrowInfo { get; set; } = [];
    }
    public class BorrowData
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public BookData Book { get; set; } = null!;
        public int UserId { get; set; }
        public UserData User { get; set; } = null!;
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
    }
}
