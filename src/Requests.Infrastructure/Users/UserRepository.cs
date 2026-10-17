using Microsoft.EntityFrameworkCore;
using Requests.Application.Auth;
using Requests.Domain.Users;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure.Users;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
}
