using Microsoft.JSInterop;

namespace Client.Auth;

// Keeps the JWT in the browser's localStorage so the user stays signed in across reloads.
// The token is cached in memory to avoid a JS interop call on every HTTP request.
public class TokenStorage(IJSRuntime js)
{
    private const string Key = "innovaflow.token";

    private string? _token;
    private bool _loaded;

    public async ValueTask<string?> GetTokenAsync()
    {
        if (!_loaded)
        {
            _token = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            _loaded = true;
        }

        return _token;
    }

    public async ValueTask SetTokenAsync(string token)
    {
        await js.InvokeVoidAsync("localStorage.setItem", Key, token);
        _token = token;
        _loaded = true;
    }

    public async ValueTask RemoveTokenAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", Key);
        _token = null;
        _loaded = true;
    }
}
