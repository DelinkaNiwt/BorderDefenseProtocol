using System;
using System.Globalization;
using System.Text;

namespace Scriban.Syntax;

[ScriptSyntax("literal", "<value>")]
public class ScriptLiteral : ScriptExpression
{
	public object Value { get; set; }

	public ScriptLiteralStringQuoteType StringQuoteType { get; set; }

	public ScriptLiteral()
	{
	}

	public ScriptLiteral(object value)
	{
		Value = value;
	}

	public override object Evaluate(TemplateContext context)
	{
		return Value;
	}

	public bool IsPositiveInteger()
	{
		if (Value == null)
		{
			return false;
		}
		Type type = Value.GetType();
		if (type == typeof(int))
		{
			return (int)Value >= 0;
		}
		if (type == typeof(byte))
		{
			return true;
		}
		if (type == typeof(sbyte))
		{
			return (sbyte)Value >= 0;
		}
		if (type == typeof(short))
		{
			return (short)Value >= 0;
		}
		if (type == typeof(ushort))
		{
			return true;
		}
		if (type == typeof(uint))
		{
			return true;
		}
		if (type == typeof(long))
		{
			return (long)Value > 0;
		}
		if (type == typeof(ulong))
		{
			return true;
		}
		return false;
	}

	public override void Write(TemplateRewriterContext context)
	{
		if (Value == null)
		{
			context.Write("null");
			return;
		}
		Type type = Value.GetType();
		if (type == typeof(string))
		{
			context.Write(ToLiteral(StringQuoteType, (string)Value));
		}
		else if (type == typeof(bool))
		{
			context.Write(((bool)Value) ? "true" : "false");
		}
		else if (type == typeof(int))
		{
			context.Write(((int)Value).ToString(CultureInfo.InvariantCulture));
		}
		else if (type == typeof(double))
		{
			context.Write(AppendDecimalPoint(((double)Value).ToString("R", CultureInfo.InvariantCulture), hasNaN: true));
		}
		else if (type == typeof(float))
		{
			context.Write(AppendDecimalPoint(((float)Value).ToString("R", CultureInfo.InvariantCulture), hasNaN: true));
		}
		else if (type == typeof(byte))
		{
			context.Write(((byte)Value).ToString(CultureInfo.InvariantCulture));
		}
		else if (type == typeof(sbyte))
		{
			context.Write(((sbyte)Value).ToString(CultureInfo.InvariantCulture));
		}
		else if (type == typeof(short))
		{
			context.Write(((short)Value).ToString(CultureInfo.InvariantCulture));
		}
		else if (type == typeof(ushort))
		{
			context.Write(((ushort)Value).ToString(CultureInfo.InvariantCulture));
		}
		else if (type == typeof(uint))
		{
			context.Write(((uint)Value).ToString(CultureInfo.InvariantCulture));
		}
		else if (type == typeof(long))
		{
			context.Write(((long)Value).ToString(CultureInfo.InvariantCulture));
		}
		else if (type == typeof(ulong))
		{
			context.Write(((uint)Value).ToString(CultureInfo.InvariantCulture));
		}
		else if (type == typeof(char))
		{
			context.Write(ToLiteral(ScriptLiteralStringQuoteType.SimpleQuote, Value.ToString()));
		}
		else
		{
			context.Write(Value.ToString());
		}
	}

	public override string ToString()
	{
		return Value?.ToString() ?? "null";
	}

	private static string ToLiteral(ScriptLiteralStringQuoteType quoteType, string input)
	{
		char c = quoteType switch
		{
			ScriptLiteralStringQuoteType.DoubleQuote => '"', 
			ScriptLiteralStringQuoteType.SimpleQuote => '\'', 
			ScriptLiteralStringQuoteType.Verbatim => '`', 
			_ => throw new ArgumentOutOfRangeException("quoteType"), 
		};
		StringBuilder stringBuilder = new StringBuilder(input.Length + 2);
		stringBuilder.Append(c);
		if (quoteType == ScriptLiteralStringQuoteType.Verbatim)
		{
			stringBuilder.Append(input.Replace("`", "``"));
		}
		else
		{
			foreach (char c2 in input)
			{
				switch (c2)
				{
				case '\\':
					stringBuilder.Append("\\\\");
					continue;
				case '\0':
					stringBuilder.Append("\\0");
					continue;
				case '\a':
					stringBuilder.Append("\\a");
					continue;
				case '\b':
					stringBuilder.Append("\\b");
					continue;
				case '\f':
					stringBuilder.Append("\\f");
					continue;
				case '\n':
					stringBuilder.Append("\\n");
					continue;
				case '\r':
					stringBuilder.Append("\\r");
					continue;
				case '\t':
					stringBuilder.Append("\\t");
					continue;
				case '\v':
					stringBuilder.Append("\\v");
					continue;
				}
				if (c2 == c)
				{
					stringBuilder.Append('\\').Append(c2);
				}
				else if (char.IsControl(c2))
				{
					stringBuilder.Append("\\u");
					ushort num = c2;
					stringBuilder.Append(num.ToString("x4"));
				}
				else
				{
					stringBuilder.Append(c2);
				}
			}
		}
		stringBuilder.Append(c);
		return stringBuilder.ToString();
	}

	private static string AppendDecimalPoint(string text, bool hasNaN)
	{
		foreach (char c in text)
		{
			if (c == 'e' || c == 'E' || c == '.')
			{
				return text;
			}
		}
		if (hasNaN && (string.Equals(text, "NaN") || text.Contains("Infinity")))
		{
			return text;
		}
		return text + ".0";
	}
}
