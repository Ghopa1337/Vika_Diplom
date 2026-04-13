namespace CargoTransport.Desktop.Services;

public sealed record AuthenticatedUser(uint Id, string Username, string FullName, string RoleCode, string RoleName);

public interface IAuthStateService
{
    bool IsAuthenticated { get; }
    AuthenticatedUser? CurrentUser { get; }
    event EventHandler? AuthStateChanged;
    void SignIn(AuthenticatedUser user);
    void SignOut();
}

public class AuthStateService : IAuthStateService
{
    public bool IsAuthenticated => CurrentUser is not null;
    public AuthenticatedUser? CurrentUser { get; private set; }
    public event EventHandler? AuthStateChanged;

    public void SignIn(AuthenticatedUser user)
    {
        CurrentUser = user;
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        CurrentUser = null;
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
