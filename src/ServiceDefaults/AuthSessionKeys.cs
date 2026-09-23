namespace ServiceDefaults;

// Redis keys for active login sessions. Identity writes a key per issued token (by its jti)
// and deletes it on sign-out; the Gateway rejects any token whose key is missing.
public static class AuthSessionKeys
{
    public static string For(string jti) => $"auth:session:{jti}";
}
