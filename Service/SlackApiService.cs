using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SlackTimezoneTracker.Service;
    
public class SlackApiService(IHttpClientFactory httpClientFactory)
{
    public async Task SendMessageAsync(string botToken, string userId, string message)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);

        var payload = new
        {
            channel = userId,
            text = message
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://slack.com/api/chat.postMessage", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Dictionary<string, string>> GetUserTimezonesAsync(string botToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);

        var response = await client.GetAsync("https://slack.com/api/users.list");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var timezones = new Dictionary<string, string>();

        if (doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean())
        {
            if (doc.RootElement.TryGetProperty("members", out var members))
            {
                foreach (var member in members.EnumerateArray())
                {
                    if (member.TryGetProperty("id", out var idProp))
                    {
                        var id = idProp.GetString();
                        var tz = member.TryGetProperty("tz", out var tzProp) ? tzProp.GetString() ?? "UTC" : "UTC";

                        if (!string.IsNullOrEmpty(id))
                            timezones[id] = tz;
                    }
                }
            }
        }

        return timezones;
    }
}
