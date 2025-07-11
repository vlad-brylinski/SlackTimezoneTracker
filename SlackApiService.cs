using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class SlackApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SlackApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendMessageAsync(string botToken, string userId, string message)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);

        var payload = new
        {
            channel = userId,
            text = message
        };

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://slack.com/api/chat.postMessage", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Dictionary<string, string>> GetUserTimezonesAsync(string botToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);

        var response = await client.GetAsync("https://slack.com/api/users.list");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(body);

        var timezones = new Dictionary<string, string>();
        if (json["ok"]?.Value<bool>() == true)
        {
            foreach (var member in json["members"]!)
            {
                var id = member["id"]?.ToString();
                var tz = member["tz"]?.ToString() ?? "UTC";

                if (!string.IsNullOrEmpty(id))
                    timezones[id] = tz;
            }
        }
        return timezones;
    }
}