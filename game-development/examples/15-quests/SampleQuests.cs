namespace Course.Quests;

/// <summary>A tiny quest chain as a designer would write it: data, not code.</summary>
public static class SampleQuests
{
    public const string Json = """
    [
      { "id": "q.wolves", "title": "Wolves at the Gate",
        "objectives": [
          { "id": "kill", "kind": "Kill", "target": "wolf", "count": 3 },
          { "id": "talk", "kind": "Talk", "target": "elder" } ],
        "reward": { "exp": 200, "gold": 50, "items": ["item.pelt_cloak"] },
        "onAcceptDialogue": "elder.intro", "onCompleteCutscene": "gate.relief" },
      { "id": "q.cave", "title": "Into the Cave",
        "prerequisites": ["q.wolves"], "minLevel": 2,
        "objectives": [
          { "id": "reach", "kind": "Reach", "target": "cave" },
          { "id": "clear", "kind": "Clear", "target": "mission.cave" } ],
        "reward": { "exp": 500, "unlockCharacter": "fellow.rina" } }
    ]
    """;
}
