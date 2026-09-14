using Identity.Endpoints;

namespace Identity.Extensions;

public static class EndpointBuilderExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("auth");
        group.MapLogin();

        return app;
    }
}
