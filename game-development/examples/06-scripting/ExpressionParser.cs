using System.Globalization;

namespace Course.Scripting;

/// <summary>
/// Recursive-descent parser for: numbers, property names, + - * / %, unary minus, parentheses and the
/// functions min, max, abs, floor, ceil, clamp. Grammar: expr = term (('+'|'-') term)*;
/// term = unary (('*'|'/'|'%') unary)*; unary = '-' unary | primary.
/// </summary>
public sealed class ExpressionParser
{
    private const int MaxDepth = 32;

    private static readonly Dictionary<string, (int Args, Func<double[], double> Fn)> Functions = new()
    {
        ["min"] = (2, a => Math.Min(a[0], a[1])),
        ["max"] = (2, a => Math.Max(a[0], a[1])),
        ["abs"] = (1, a => Math.Abs(a[0])),
        ["floor"] = (1, a => Math.Floor(a[0])),
        ["ceil"] = (1, a => Math.Ceiling(a[0])),
        ["clamp"] = (3, a => Math.Min(Math.Max(a[0], a[1]), a[2])),
    };

    private readonly string _text;
    private readonly List<string> _variables = new();
    private int _pos;
    private int _depth;

    private ExpressionParser(string text) => _text = text;

    public static CompiledExpression Parse(string text)
    {
        var parser = new ExpressionParser(text);
        var eval = parser.ParseExpr();
        parser.SkipSpaces();
        if (parser._pos < text.Length)
            throw parser.Error($"Unexpected '{text[parser._pos]}'");
        return new CompiledExpression(text, eval, parser._variables);
    }

    private Func<IPropertyLookup, double> ParseExpr()
    {
        if (++_depth > MaxDepth) throw Error("Expression is nested too deeply");
        var left = ParseTerm();
        while (true)
        {
            SkipSpaces();
            var op = Peek();
            if (op is not ('+' or '-')) break;
            _pos++;
            var l = left;
            var r = ParseTerm();
            left = op == '+' ? p => l(p) + r(p) : p => l(p) - r(p);
        }
        _depth--;
        return left;
    }

    private Func<IPropertyLookup, double> ParseTerm()
    {
        var left = ParseUnary();
        while (true)
        {
            SkipSpaces();
            var op = Peek();
            if (op is not ('*' or '/' or '%')) break;
            _pos++;
            var l = left;
            var r = ParseUnary();
            left = op switch
            {
                '*' => p => l(p) * r(p),
                '/' => p => Divide(l(p), r(p)),
                _ => p => Modulo(l(p), r(p)),
            };
        }
        return left;
    }

    private Func<IPropertyLookup, double> ParseUnary()
    {
        SkipSpaces();
        if (Peek() == '-')
        {
            _pos++;
            var inner = ParseUnary();
            return p => -inner(p);
        }
        return ParsePrimary();
    }

    private Func<IPropertyLookup, double> ParsePrimary()
    {
        SkipSpaces();
        var c = Peek();
        if (c == '(')
        {
            _pos++;
            var inner = ParseExpr();
            Expect(')');
            return inner;
        }
        if (char.IsAsciiDigit(c) || c == '.') return ParseNumber();
        if (char.IsAsciiLetter(c) || c == '_') return ParseNameOrCall();
        throw Error(c == '\0' ? "Unexpected end of formula" : $"Unexpected '{c}'");
    }

    private Func<IPropertyLookup, double> ParseNumber()
    {
        var start = _pos;
        while (char.IsAsciiDigit(Peek()) || Peek() == '.') _pos++;
        var token = _text[start.._pos];
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw Error($"Bad number '{token}'", start);
        return _ => value;
    }

    private Func<IPropertyLookup, double> ParseNameOrCall()
    {
        var start = _pos;
        while (char.IsAsciiLetterOrDigit(Peek()) || Peek() == '_' || Peek() == '.') _pos++;
        var name = _text[start.._pos];
        SkipSpaces();

        if (Peek() != '(')
        {
            if (!_variables.Contains(name)) _variables.Add(name);
            return p => p.TryGet(name, out var v)
                ? v
                : throw new ExpressionException($"Unknown property '{name}'.");
        }

        if (!Functions.TryGetValue(name, out var fn)) throw Error($"Unknown function '{name}'", start);
        _pos++; // '('
        var args = new List<Func<IPropertyLookup, double>>();
        SkipSpaces();
        if (Peek() != ')')
        {
            do args.Add(ParseExpr());
            while (TryConsume(','));
        }
        Expect(')');
        if (args.Count != fn.Args) throw Error($"Function '{name}' takes {fn.Args} argument(s), got {args.Count}", start);

        return p =>
        {
            var values = new double[args.Count];
            for (var i = 0; i < values.Length; i++) values[i] = args[i](p);
            return fn.Fn(values);
        };
    }

    private static double Divide(double a, double b) =>
        b == 0 ? throw new ExpressionException("Division by zero.") : a / b;

    private static double Modulo(double a, double b) =>
        b == 0 ? throw new ExpressionException("Division by zero.") : a % b;

    private char Peek() => _pos < _text.Length ? _text[_pos] : '\0';

    private void SkipSpaces()
    {
        while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos])) _pos++;
    }

    private bool TryConsume(char c)
    {
        SkipSpaces();
        if (Peek() != c) return false;
        _pos++;
        return true;
    }

    private void Expect(char c)
    {
        if (!TryConsume(c)) throw Error($"Expected '{c}'");
    }

    private ExpressionException Error(string message, int? at = null) =>
        new($"{message} at position {at ?? _pos} in \"{_text}\".");
}
