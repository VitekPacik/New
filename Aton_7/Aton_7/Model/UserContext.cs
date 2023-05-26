using Microsoft.EntityFrameworkCore;
namespace Aton_7.Model
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options)
        : base(options)
        {
        }
        public DbSet<User> Users { get; set; } = null!;
    }
}
