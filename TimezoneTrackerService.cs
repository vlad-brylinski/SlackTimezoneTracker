public class TimezoneTrackerService : BackgroundService
{
    private readonly InMemoryWorkspaceStore _store;
    private readonly SlackApiService _slackApi;
    private readonly ILogger<TimezoneTrackerService> _logger;

    public TimezoneTrackerService(InMemoryWorkspaceStore store, SlackApiService slackApi, ILogger<TimezoneTrackerService> logger)
    {
        _store = store;
        _slackApi = slackApi;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timezone Tracker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var workspace in _store.GetAllWorkspaces())
            {
                try
                {
                    var currentTimezones = await _slackApi.GetUserTimezonesAsync(workspace.BotToken);

                    foreach (var userTz in currentTimezones)
                    {
                        if (!workspace.UserTimezones.TryGetValue(userTz.Key, out var oldTz) || oldTz != userTz.Value)
                        {
                            // Timezone changed or new user
                            workspace.UserTimezones[userTz.Key] = userTz.Value;

                            // Notify the configured user about the change
                            var message = $"User <@{userTz.Key}> timezone changed to {userTz.Value} in workspace {workspace.TeamName}.";
                            await _slackApi.SendMessageAsync(workspace.BotToken, workspace.NotifyUserId, message);

                            _logger.LogInformation(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking timezones for workspace {team}", workspace.TeamId);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
