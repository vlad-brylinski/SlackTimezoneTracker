using System.Collections.Concurrent;

namespace SlackTimezoneTracker.Model;

public class InMemoryWorkspaceStore
{
    private readonly ConcurrentDictionary<string, WorkspaceInstall> _workspaces = new();

    public void SaveWorkspace(WorkspaceInstall install)
    {
        _workspaces[install.TeamId] = install;
    }

    public WorkspaceInstall? GetWorkspace(string teamId)
    {
        _workspaces.TryGetValue(teamId, out var workspace);
        return workspace;
    }

    public IEnumerable<WorkspaceInstall> GetAllWorkspaces() => _workspaces.Values;
}