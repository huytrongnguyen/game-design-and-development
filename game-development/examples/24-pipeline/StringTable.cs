using System.Text;

namespace M24.Pipeline;

/// <summary>
/// Localised text by string id. Supports <c>{name}</c> placeholders, plural variants
/// (<c>id.one</c>, <c>id.other</c>) chosen by a <c>count</c> argument, and a fallback language.
/// </summary>
public sealed class StringTable
{
    private readonly Dictionary<string, Dictionary<string, string>> _languages = new();
    private readonly string _fallback;

    public StringTable(string fallbackLanguage)
    {
        _fallback = fallbackLanguage;
    }

    public void Add(string language, string id, string text)
    {
        if (!_languages.TryGetValue(language, out var table))
        {
            table = new Dictionary<string, string>();
            _languages[language] = table;
        }

        table[id] = text;
    }

    /// <summary>True when the language defines the id, directly or through its plural variants.</summary>
    public bool Has(string language, string id) =>
        _languages.TryGetValue(language, out var t) &&
        (t.ContainsKey(id) || t.ContainsKey(id + ".other"));

    /// <summary>
    /// Looks up text, falling back to the fallback language, and finally to <c>[id]</c> so a gap is
    /// visible on screen instead of crashing.
    /// </summary>
    public string Get(string language, string id, IReadOnlyDictionary<string, object>? args = null)
    {
        var template = Find(language, id, args) ?? Find(_fallback, id, args);
        return template is null ? $"[{id}]" : Substitute(template, args);
    }

    private string? Find(string language, string id, IReadOnlyDictionary<string, object>? args)
    {
        if (!_languages.TryGetValue(language, out var t))
        {
            return null;
        }

        if (args is not null && args.TryGetValue("count", out var count) &&
            t.TryGetValue(id + (Convert.ToInt32(count) == 1 ? ".one" : ".other"), out var plural))
        {
            return plural;
        }

        return t.GetValueOrDefault(id);
    }

    private static string Substitute(string template, IReadOnlyDictionary<string, object>? args)
    {
        if (args is null)
        {
            return template;
        }

        var sb = new StringBuilder();
        var i = 0;
        while (i < template.Length)
        {
            var close = template[i] == '{' ? template.IndexOf('}', i) : -1;
            if (close > i && args.TryGetValue(template[(i + 1)..close], out var value))
            {
                sb.Append(value);
                i = close + 1;
            }
            else
            {
                sb.Append(template[i]);
                i++;
            }
        }

        return sb.ToString();
    }
}
