using System.Net.Http.Json;
using Contracts.Ideas;

namespace Analysis.Api.Clients;


public sealed class IdeasClient(HttpClient http)
{
    public async Task<IdeaDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var res = await http.GetAsync($"/api/ideas/{id}", ct);
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<IdeaDto>(ct)
            : null;
    }
}
