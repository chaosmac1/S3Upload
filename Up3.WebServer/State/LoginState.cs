namespace BlazorApp1.State;

public record struct LoginValue(
    bool IsLogin,
    bool LoginError,
    string? UserMessageError
);

public interface ILoginState: IState<LoginValue> {
    public Task LoginUserAsync(string username, string password, CancellationToken token);
    public Task LogoutAsync(CancellationToken token);
}

public sealed class LoginState: State<LoginValue> {
    public async Task LoginUserAsync(string username, string password, CancellationToken token) {
        if (token.IsCancellationRequested) {
            await Task.FromCanceled(token);
            throw new OperationCanceledException(token);
        }
        
        throw new NotImplementedException(nameof(LoginState));
    }

    public async Task LogoutAsync(CancellationToken token) {
        throw new NotImplementedException(nameof(LogoutAsync));
    }
}