using System;

namespace Scriban.Parsing;

public struct TextPosition : IEquatable<TextPosition>
{
	public static readonly TextPosition Eof = new TextPosition(-1, -1, -1);

	public int Offset { get; set; }

	public int Column { get; set; }

	public int Line { get; set; }

	public TextPosition(int offset, int line, int column)
	{
		Offset = offset;
		Column = column;
		Line = line;
	}

	public TextPosition NextColumn(int offset = 1)
	{
		return new TextPosition(Offset + offset, Line, Column + offset);
	}

	public TextPosition NextLine(int offset = 1)
	{
		return new TextPosition(Offset + offset, Line + offset, 0);
	}

	public override string ToString()
	{
		return $"({Offset}:{Line},{Column})";
	}

	public string ToStringSimple()
	{
		return $"{Line + 1},{Column + 1}";
	}

	public bool Equals(TextPosition other)
	{
		if (Offset == other.Offset && Column == other.Column)
		{
			return Line == other.Line;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is TextPosition)
		{
			return Equals((TextPosition)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Offset;
	}

	public static bool operator ==(TextPosition left, TextPosition right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(TextPosition left, TextPosition right)
	{
		return !left.Equals(right);
	}
}
