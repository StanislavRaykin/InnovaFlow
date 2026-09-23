using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Contracts.Identity;

namespace Client.Auth;

public record AuthResult(bool Succeeded, string? Error = null);

public class AuthService(HttpClient http, JwtAuthenticationStateProvider authState)
{
    public async Task<AuthResult> SignInAsync(LoginRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/signin", request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new AuthResult(false, "Грешен имейл или парола.");

            return await CompleteSignInAsync(response);
        }
        catch (HttpRequestException)
        {
            return new AuthResult(false, "Сървърът не е достъпен. Опитайте отново.");
        }
    }

    public async Task<AuthResult> SignUpAsync(RegistrationRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/signup", request);
            return await CompleteSignInAsync(response);
        }
        catch (HttpRequestException)
        {
            return new AuthResult(false, "Сървърът не е достъпен. Опитайте отново.");
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            // ends the session in Redis so the token stops working everywhere
            await http.PostAsync("api/auth/signout", null);
        }
        catch (HttpRequestException)
        {
            // still sign out locally; the token expires on its own
        }
        finally
        {
            await authState.SignOutAsync();
        }
    }

    private async Task<AuthResult> CompleteSignInAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            return new AuthResult(false, await ReadErrorAsync(response));

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (auth == null || string.IsNullOrWhiteSpace(auth.Token))
            return new AuthResult(false, "Неочакван отговор от сървъра.");

        await authState.SignInAsync(auth.Token);
        return new AuthResult(true);
    }

    // Identity returns errors as a plain string, a { code: [messages] } dictionary,
    // or a ValidationProblem with an "errors" dictionary.
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        const string fallback = "Възникна грешка. Опитайте отново.";

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            return fallback;

        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            if (root.ValueKind == JsonValueKind.String)
                return root.GetString() ?? fallback;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("errors", out var errors))
                    root = errors;

                var messages = root.EnumerateObject()
                    .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                        ? p.Value.EnumerateArray().Select(v => v.ToString())
                        : [p.Value.ToString()])
                    .ToList();

                if (messages.Count > 0)
                    return string.Join(" ", messages);
            }
        }
        catch (JsonException)
        {
            return body;
        }

        return fallback;
    }
}
