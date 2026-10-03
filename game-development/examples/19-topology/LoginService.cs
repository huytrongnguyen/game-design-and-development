using System.Security.Cryptography;
using System.Text;

namespace Course.Topology;

/// <summary>
/// The login process: checks credentials, hands out a session token. It knows nothing about
/// characters or zones. (A real service would use a slow password hash such as Argon2.)
/// </summary>
public sealed class LoginService
{
    private readonly Dictionary<string, byte[]> _passwordHashes = new();
    private readonly SessionTokenService _tokens;
    private readonly long _tokenTtlSeconds;

    public LoginService(SessionTokenService tokens, long tokenTtlSeconds)
    {
        _tokens = tokens;
        _tokenTtlSeconds = tokenTtlSeconds;
    }

    public void Register(string accountId, string password) =>
        _passwordHashes[accountId] = Hash(password);

    public LoginResult Login(string accountId, string password)
    {
        if (!_passwordHashes.TryGetValue(accountId, out var stored)
            || !CryptographicOperations.FixedTimeEquals(stored, Hash(password)))
            return new LoginResult(false, null);
        return new LoginResult(true, _tokens.Issue(accountId, _tokenTtlSeconds));
    }

    private static byte[] Hash(string password) => SHA256.HashData(Encoding.UTF8.GetBytes(password));
}
