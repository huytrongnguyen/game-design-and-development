namespace Course.PartyControl;

/// <summary>
/// Maps a key to (character index, hotbar slot). Each character owns a keyboard row, so every
/// character's skills are reachable at once, with no need to make that character the leader first.
/// The table is data, so it can be rebound without touching code.
/// </summary>
public sealed class KeyMap
{
    private readonly Dictionary<char, (int Character, int Slot)> _bindings = [];

    public static KeyMap Default(int characters = 3)
    {
        var map = new KeyMap();
        string[] rows = ["QWER", "ASDF", "ZXCV", "1234"];
        for (int c = 0; c < Math.Min(characters, rows.Length); c++)
        {
            for (int s = 0; s < rows[c].Length; s++)
            {
                map.Bind(rows[c][s], c, s);
            }
        }
        return map;
    }

    public void Bind(char key, int character, int slot) =>
        _bindings[char.ToUpperInvariant(key)] = (character, slot);

    public bool TryResolve(char key, out (int Character, int Slot) target) =>
        _bindings.TryGetValue(char.ToUpperInvariant(key), out target);
}
