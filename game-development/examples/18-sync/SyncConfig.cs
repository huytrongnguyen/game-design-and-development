namespace Course.Sync;

/// <summary>Tunables of the replication layer.</summary>
/// <param name="SightRange">Distance at which an unknown entity becomes visible (Enter).</param>
/// <param name="LeaveRange">Distance at which a known entity is dropped (Leave). Larger than sight so an
/// entity hovering on the border does not flicker in and out.</param>
/// <param name="NearRange">Inside this range an entity is updated every tick.</param>
/// <param name="FarInterval">Outside NearRange an entity is updated only every N ticks.</param>
/// <param name="BudgetBytesPerViewerTick">Cap on Enter/Update bytes per viewer per tick.</param>
/// <param name="CellSize">Grid cell edge length.</param>
public sealed record SyncConfig(
    float SightRange = 100f,
    float LeaveRange = 120f,
    float NearRange = 50f,
    int FarInterval = 4,
    int BudgetBytesPerViewerTick = int.MaxValue,
    float CellSize = 50f);
