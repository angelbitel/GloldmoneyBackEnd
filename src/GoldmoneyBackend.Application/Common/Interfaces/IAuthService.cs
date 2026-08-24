using GoldmoneyBackend.Application.Auth.DTOs;

namespace GoldmoneyBackend.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthTokenDto?> LoginAsync(string userName, string password, CancellationToken cancellationToken);
}
