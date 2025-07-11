public class WorkspaceInstall
{
    public string TeamId { get; set; } = null!;
    public string TeamName { get; set; } = null!;
    public string BotToken { get; set; } = null!;
    public string BotUserId { get; set; } = null!;
    public string NotifyUserId { get; set; } = null!; // user to notify on timezone change
    public Dictionary<string, string> UserTimezones { get; set; } = new(); // userid -> timezone
}