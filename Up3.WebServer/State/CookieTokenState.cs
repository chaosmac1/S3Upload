namespace Up3.WebServer.State;


public record struct CookieTokenValue(ICookieToken? Token, bool IsCheckIsExistInBrowser);
public sealed class CookieTokenState: State<CookieTokenValue> {
    public async Task LoadCookieAsync() {
        throw new  NotImplementedException(nameof(LoadCookieAsync));
    }
    
    public async Task CreateAndSaveCookieAsync(IUserId userId) {
        var cookie = CreateCookie(userId);
        await SetCookieInBrowserAsync(cookie);
        this.Value = this.Value with { IsCheckIsExistInBrowser = true, Token = cookie };
    }

    
    public async Task DeleteCookieAsync(CancellationToken token) {
        await RemoveCookieInBrowserCookieAsync(token);
        this.Value = this.Value with { IsCheckIsExistInBrowser = false, Token = null};
    }
    
    private ICookieToken CreateCookie(IUserId userId) {
        throw new NotImplementedException(nameof(CreateCookie));
    }
    
    private async Task SetCookieInBrowserAsync(ICookieToken token) {
        throw new  NotImplementedException(nameof(SetCookieInBrowserAsync));
    }

    private async Task RemoveCookieInBrowserCookieAsync(CancellationToken token) {
        throw new  NotImplementedException(nameof(RemoveCookieInBrowserCookieAsync));
    }
}