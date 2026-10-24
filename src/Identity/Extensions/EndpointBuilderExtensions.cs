using Identity.Endpoints;

namespace Identity.Extensions;

public static class EndpointBuilderExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("auth");
        var githubGroup = app.MapGroup("api/auth/external").AllowAnonymous();
        
        group.MapLogin();
        group.MapRegistration();
        group.MapLogout();
        githubGroup.MapExternalLogin();

        return app;
    }
}
