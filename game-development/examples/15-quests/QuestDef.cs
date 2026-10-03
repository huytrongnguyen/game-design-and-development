namespace Course.Quests;

/// <summary>Static, designer-authored quest data. Loaded from JSON; never mutated at runtime.</summary>
/// <param name="Prerequisites">Quest ids that must be <see cref="QuestState.Rewarded"/> first.</param>
/// <param name="OnAcceptDialogue">Dialogue id to show when accepted (a trigger, not the text).</param>
/// <param name="OnCompleteCutscene">Cutscene id to play when the last objective finishes.</param>
public sealed record QuestDef(
    string Id,
    string Title,
    IReadOnlyList<ObjectiveDef> Objectives,
    RewardDef Reward,
    IReadOnlyList<string>? Prerequisites = null,
    int MinLevel = 1,
    string? OnAcceptDialogue = null,
    string? OnCompleteCutscene = null);
