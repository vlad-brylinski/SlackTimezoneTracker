using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

[ApiController]
[Route("slack/oauth")]
public class SlackOAuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly InMemoryWorkspaceStore _store;

    public SlackOAuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration, InMemoryWorkspaceStore store)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _store = store;
    }

    [HttpGet("start")]
    public IActionResult Start()
    {
        var clientId = _configuration["Slack:ClientId"];
        var redirectUri = _configuration["Slack:RedirectUri"];
        var scopes = _configuration["Slack:BotScopes"];

        var url = QueryHelpers.AddQueryString("https://slack.com/oauth/v2/authorize", new Dictionary<string, string>
        {
            {"client_id", clientId},
            {"scope", scopes},
            {"redirect_uri", redirectUri}
        });

        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code)
    {
        var client = _httpClientFactory.CreateClient();

        var response = await client.PostAsync("https://slack.com/api/oauth.v2.access", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", _configuration["Slack:ClientId"]! },
            { "client_secret", _configuration["Slack:ClientSecret"]! },
            { "code", code },
            { "redirect_uri", _configuration["Slack:RedirectUri"]! }
        }));

        var body = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(body);

        if (json["ok"]?.Value<bool>() != true)
        {
            return BadRequest("OAuth failed: " + body);
        }

        var accessToken = json["access_token"]!.ToString();
        var teamId = json["team"]!["id"]!.ToString();
        var teamName = json["team"]!["name"]!.ToString();
        var botUserId = json["bot_user_id"]?.ToString() ?? "";

        var install = new WorkspaceInstall
        {
            TeamId = teamId,
            TeamName = teamName,
            BotToken = accessToken,
            BotUserId = botUserId,
            NotifyUserId = botUserId, // default notify to bot user itself; you can change later
            UserTimezones = new Dictionary<string, string>()
        };

        _store.SaveWorkspace(install);

        return Content("Slack app installed successfully! You may close this tab.");
    }
}
