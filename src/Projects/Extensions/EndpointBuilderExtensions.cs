using Projects.Endpoints;

namespace Projects.Extensions;

public static class EndpointBuilderExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var ideas = app.MapGroup("/api/ideas").WithTags("ideas").RequireAuthorization();
        // var versions = app.MapGroup("/api/idea/versions").WithTags("versions");
        var drafts = app.MapGroup("/api/idea/drafts").WithTags("drafts").RequireAuthorization();
        var projects = app.MapGroup("/api/projects").WithTags("projects");

        ideas.MapIdeaEndpoints();
        drafts.MapDraftEndpoints();


        //later on - add endpoints for ideas(post) 
        return app;
    }
}
