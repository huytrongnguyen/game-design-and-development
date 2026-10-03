namespace Course.Quests;

/// <summary>What the player has to do (objective) or did (event). One enum serves both,
/// so "does this event advance that objective" is a plain equality check.</summary>
public enum ObjectiveKind
{
    Kill,
    Collect,
    Talk,
    Reach,
    /// <summary>A mission instance was cleared; this is how a story mission feeds the quest log.</summary>
    Clear,
}
