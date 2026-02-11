using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Data
{
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
            : base(options) { }

        public DbSet<BookData> Books => Set<BookData>();
        public DbSet<UserData> Users => Set<UserData>();
        public DbSet<BorrowData> Borrows => Set<BorrowData>();
    }
}
