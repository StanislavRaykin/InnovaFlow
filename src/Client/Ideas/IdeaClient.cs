using System.Net.Http.Json;
using Contracts.Ideas;

namespace Client.Ideas;

public record IdeaCreatedResult(bool success,Guid ideaId ,string? Error = null);
public class IdeaClient(HttpClient http, IdeaDraft draft ,ILogger<IdeaClient> logger)
{
      private readonly HttpClient http = http;
    private readonly ILogger<IdeaClient> logger = logger;
    
    public async Task CreateIdeaAsync()
    {
        try
        {
            var idea = draft.ToCreateIdeaRequest();
            var result = await http.PostAsJsonAsync("api/ideas", idea);
            if  (result.IsSuccessStatusCode)
            {
                logger.LogDebug("Idea created", idea);
            }
            else
            {
                logger.LogDebug("Idea wasn't created", result.Content);
            }
            
        }
        catch (Exception ex)
        {
            logger.LogError("Idea could not be created", ex);
        }
        
    }
}
