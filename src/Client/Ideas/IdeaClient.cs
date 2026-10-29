using System.Net;
using System.Net.Http.Json;

namespace Client.Ideas;

public record IdeaCreatedResult(bool Success, Guid IdeaId, string? Error = null);

public class IdeaClient(HttpClient http, IdeaDraft draft, ILogger<IdeaClient> logger)
{
    // the part of the server's ValidationProblem we show; the full type isn't available in wasm
    private record ValidationProblem(Dictionary<string, string[]> Errors);

    private readonly HttpClient http = http;
    private readonly IdeaDraft draft = draft;
    private readonly ILogger<IdeaClient> logger = logger;

    // submits the wizard's draft; the server promotes the autosaved draft with the same id
    public async Task<IdeaCreatedResult> CreateIdeaAsync()
    {
        try
        {
            var idea = draft.ToCreateIdeaRequest();
            var response = await http.PostAsJsonAsync("api/ideas", idea);

            if (response.IsSuccessStatusCode)
            {
                logger.LogDebug("Idea {IdeaId} created. Onto analysis...", idea.Id);
                return new IdeaCreatedResult(true, idea.Id);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
                var first = problem?.Errors?.Values.SelectMany(e => e).FirstOrDefault();
                return new IdeaCreatedResult(false, idea.Id, first ?? "Уверете се, че всички полета са попълнени.");
            }

            logger.LogWarning("Idea {IdeaId} wasn't created: {Status}", idea.Id, response.StatusCode);
            return new IdeaCreatedResult(false, idea.Id, "Идеята не беше създадена. Опитайте по-късно.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Idea could not be created");
            return new IdeaCreatedResult(false, Guid.Empty, "Идеята не беше създадена. Опитайте по-късно.");
        }
    }
}
