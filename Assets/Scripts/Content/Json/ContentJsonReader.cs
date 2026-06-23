using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class ContentJsonReader
{
    public static object Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new FormatException("JSON is empty.");
        }

        var parser = new Parser(json);
        return parser.ParseValue();
    }

    public static Dictionary<string, object> ParseObject(string json)
    {
        return Parse(json) as Dictionary<string, object>
               ?? throw new FormatException("JSON root is not an object.");
    }

    private sealed class Parser
    {
        private readonly string text;
        private int index;

        public Parser(string text)
        {
            this.text = text;
        }

        public object ParseValue()
        {
            SkipWhitespace();
            if (index >= text.Length)
            {
                throw new FormatException("Unexpected end of JSON.");
            }

            var ch = text[index];
            return ch switch
            {
                '{' => ParseObject(),
                '[' => ParseArray(),
                '"' => ParseString(),
                't' => ParseLiteral("true", true),
                'f' => ParseLiteral("false", false),
                'n' => ParseNull(),
                _ => ParseNumber()
            };
        }

        private Dictionary<string, object> ParseObject()
        {
            Expect('{');
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            SkipWhitespace();
            if (TryConsume('}'))
            {
                return result;
            }

            while (true)
            {
                SkipWhitespace();
                var key = ParseString();
                SkipWhitespace();
                Expect(':');
                result[key] = ParseValue();
                SkipWhitespace();
                if (TryConsume('}'))
                {
                    break;
                }

                Expect(',');
            }

            return result;
        }

        private List<object> ParseArray()
        {
            Expect('[');
            var result = new List<object>();
            SkipWhitespace();
            if (TryConsume(']'))
            {
                return result;
            }

            while (true)
            {
                result.Add(ParseValue());
                SkipWhitespace();
                if (TryConsume(']'))
                {
                    break;
                }

                Expect(',');
            }

            return result;
        }

        private string ParseString()
        {
            Expect('"');
            var builder = new StringBuilder();
            while (index < text.Length)
            {
                var ch = text[index++];
                if (ch == '"')
                {
                    return builder.ToString();
                }

                if (ch == '\\')
                {
                    if (index >= text.Length)
                    {
                        throw new FormatException("Invalid escape in JSON string.");
                    }

                    var escaped = text[index++];
                    builder.Append(escaped switch
                    {
                        '"' => '"',
                        '\\' => '\\',
                        '/' => '/',
                        'b' => '\b',
                        'f' => '\f',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        'u' => ParseUnicode(),
                        _ => throw new FormatException($"Invalid escape \\{escaped}.")
                    });
                    continue;
                }

                builder.Append(ch);
            }

            throw new FormatException("Unterminated JSON string.");
        }

        private char ParseUnicode()
        {
            if (index + 4 > text.Length)
            {
                throw new FormatException("Invalid unicode escape.");
            }

            var hex = text.Substring(index, 4);
            index += 4;
            return (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        private object ParseNumber()
        {
            var start = index;
            if (text[index] == '-')
            {
                index++;
            }

            while (index < text.Length && char.IsDigit(text[index]))
            {
                index++;
            }

            if (index < text.Length && text[index] == '.')
            {
                index++;
                while (index < text.Length && char.IsDigit(text[index]))
                {
                    index++;
                }
            }

            if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
            {
                index++;
                if (index < text.Length && (text[index] == '+' || text[index] == '-'))
                {
                    index++;
                }

                while (index < text.Length && char.IsDigit(text[index]))
                {
                    index++;
                }
            }

            var numberText = text.Substring(start, index - start);
            if (numberText.Contains(".") || numberText.Contains("e") || numberText.Contains("E"))
            {
                return double.Parse(numberText, CultureInfo.InvariantCulture);
            }

            return long.Parse(numberText, CultureInfo.InvariantCulture);
        }

        private object ParseNull()
        {
            ParseLiteral("null", null);
            return null;
        }

        private object ParseLiteral(string literal, object value)
        {
            if (!text.AsSpan(index, literal.Length).SequenceEqual(literal.AsSpan()))
            {
                throw new FormatException($"Expected '{literal}'.");
            }

            index += literal.Length;
            return value;
        }

        private void SkipWhitespace()
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }
        }

        private void Expect(char ch)
        {
            SkipWhitespace();
            if (index >= text.Length || text[index] != ch)
            {
                throw new FormatException($"Expected '{ch}'.");
            }

            index++;
        }

        private bool TryConsume(char ch)
        {
            SkipWhitespace();
            if (index < text.Length && text[index] == ch)
            {
                index++;
                return true;
            }

            return false;
        }
    }
}
