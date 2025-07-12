using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using SlackTimezoneTracker.Model;

namespace SlackTimezoneTracker.Controller;

[ApiController]
[Route("slack/oauth")]
public class SlackOAuthController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    InMemoryWorkspaceStore store)
    : ControllerBase
{
    [HttpGet("start")]
    public IActionResult Start()
    {
        var clientId = configuration["Slack:ClientId"];
        var redirectUri = configuration["Slack:RedirectUri"];
        var scopes = configuration["Slack:BotScopes"];

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
        var client = httpClientFactory.CreateClient();

        var response = await client.PostAsync("https://slack.com/api/oauth.v2.access", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", configuration["Slack:ClientId"]! },
            { "client_secret", configuration["Slack:ClientSecret"]! },
            { "code", code },
            { "redirect_uri", configuration["Slack:RedirectUri"]! }
        }));

        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("ok", out var okProp) || !okProp.GetBoolean())
        {
            return BadRequest("OAuth failed: " + body);
        }

        var accessToken = root.GetProperty("access_token").GetString() ?? "";
        var team = root.GetProperty("team");
        var teamId = team.GetProperty("id").GetString() ?? "";
        var teamName = team.GetProperty("name").GetString() ?? "";
        var userId = root.TryGetProperty("authed_user", out var authedUserProp) &&
                     authedUserProp.TryGetProperty("id", out var idProp)
            ? idProp.GetString() ?? ""
            : "";

        var install = new WorkspaceInstall
        {
            TeamId = teamId,
            TeamName = teamName,
            BotToken = accessToken,
            NotifyUserId = userId,
            UserTimezones = new Dictionary<string, string>()
        };

        store.SaveWorkspace(install);

        return Content("Slack app installed successfully! You may close this tab.");
    }
}
