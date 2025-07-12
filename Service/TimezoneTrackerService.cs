using SlackTimezoneTracker.Model;

namespace SlackTimezoneTracker.Service;

public class TimezoneTrackerService(
    InMemoryWorkspaceStore store,
    SlackApiService slackApi,
    ILogger<TimezoneTrackerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Timezone Tracker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var workspace in store.GetAllWorkspaces())
            {
                try
                {
                    var currentTimezones = await slackApi.GetUserTimezonesAsync(workspace.BotToken);

                    foreach (var userTz in currentTimezones)
                    {
                        if (!workspace.UserTimezones.TryGetValue(userTz.Key, out var oldTz) || oldTz != userTz.Value)
                        {
                            // Timezone changed or new user
                            workspace.UserTimezones[userTz.Key] = userTz.Value;

                            // Notify the configured user about the change
                            var message = $"User <@{userTz.Key}> timezone changed to {userTz.Value} in workspace {workspace.TeamName}.";
                            await slackApi.SendMessageAsync(workspace.BotToken, workspace.NotifyUserId, message);

                            logger.LogInformation(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error checking timezones for workspace {team}", workspace.TeamId);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
