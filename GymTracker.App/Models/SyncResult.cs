namespace GymTracker.App.Models;

/// <summary>
/// Result of a pull: the active (non-deleted) rows, plus every id the server has ever seen for this
/// table (active or soft-deleted). The second set lets sync tell "never synced yet - safe to push"
/// apart from "deleted elsewhere - must not resurrect", which a plain active-rows list can't do once
/// a deleted row simply disappears from the list.
/// </summary>
public class SyncResult<TEntity, TKey>
{
    public List<TEntity> Active { get; set; } = new();
    public HashSet<TKey> AllKnownKeys { get; set; } = new();
}
