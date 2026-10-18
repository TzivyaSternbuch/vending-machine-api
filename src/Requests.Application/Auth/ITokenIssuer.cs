using Requests.Domain.Users;

namespace Requests.Application.Auth;

public interface ITokenIssuer
{
    LoginResponse Issue(User user);
}
