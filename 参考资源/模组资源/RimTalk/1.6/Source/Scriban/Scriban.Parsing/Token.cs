using System;

namespace Scriban.Parsing;

public struct Token : IEquatable<Token>
{
	public static readonly Token Eof = new Token(TokenType.Eof, TextPosition.Eof, TextPosition.Eof);

	public readonly TokenType Type;

	public readonly TextPosition Start;

	public TextPosition End;

	public Token(TokenType type, TextPosition start, TextPosition end)
	{
		Type = type;
		Start = start;
		End = end;
	}

	public override string ToString()
	{
		return $"{Type}({Start}:{End})";
	}

	public string GetText(string text)
	{
		if (Type == TokenType.Eof)
		{
			return "<eof>";
		}
		if (Start.Offset < text.Length && End.Offset < text.Length)
		{
			return text.Substring(Start.Offset, End.Offset - Start.Offset + 1);
		}
		return "<error>";
	}

	public bool Match(string text, string lexerText)
	{
		int num = End.Offset - Start.Offset + 1;
		if (text.Length != num)
		{
			return false;
		}
		for (int i = 0; i < text.Length; i++)
		{
			if (lexerText[Start.Offset + i] != text[i])
			{
				return false;
			}
		}
		return true;
	}

	public bool Equals(Token other)
	{
		if (Type == other.Type && Start.Equals(other.Start))
		{
			return End.Equals(other.End);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is Token)
		{
			return Equals((Token)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((((int)Type * 397) ^ Start.GetHashCode()) * 397) ^ End.GetHashCode();
	}

	public static bool operator ==(Token left, Token right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Token left, Token right)
	{
		return !left.Equals(right);
	}
}
