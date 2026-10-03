namespace Course.Quests;

/// <summary>The life of one quest for one player. Transitions only move forward.</summary>
public enum QuestState
{
    /// <summary>Prerequisites or level not met yet; invisible to the player.</summary>
    Locked,
    /// <summary>Can be accepted (a "!" over the NPC).</summary>
    Available,
    /// <summary>Accepted; objectives are being tracked.</summary>
    Active,
    /// <summary>All objectives done; waiting for the player to turn in.</summary>
    Completed,
    /// <summary>Reward granted. Only this state unlocks follow-up quests.</summary>
    Rewarded,
}
