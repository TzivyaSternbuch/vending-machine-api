using Requests.Domain.Users;

namespace Requests.Application.Auth;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
}
