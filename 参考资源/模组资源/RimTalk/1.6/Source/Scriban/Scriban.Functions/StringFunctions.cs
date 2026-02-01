using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Scriban.Runtime;

namespace Scriban.Functions;

public class StringFunctions : ScriptObject
{
	[ThreadStatic]
	private static StringBuilder _tlsBuilder;

	private static StringBuilder GetTempStringBuilder()
	{
		StringBuilder stringBuilder = _tlsBuilder;
		if (stringBuilder == null)
		{
			stringBuilder = (_tlsBuilder = new StringBuilder(1024));
		}
		return stringBuilder;
	}

	private static void ReleaseBuilder(StringBuilder builder)
	{
		builder.Length = 0;
	}

	public static string Append(string text, string with)
	{
		return (text ?? string.Empty) + (with ?? string.Empty);
	}

	public static string Capitalize(string text)
	{
		if (string.IsNullOrEmpty(text) || char.IsUpper(text[0]))
		{
			return text ?? string.Empty;
		}
		StringBuilder tempStringBuilder = GetTempStringBuilder();
		tempStringBuilder.Append(char.ToUpper(text[0]));
		if (text.Length > 1)
		{
			tempStringBuilder.Append(text, 1, text.Length - 1);
		}
		string result = tempStringBuilder.ToString();
		ReleaseBuilder(tempStringBuilder);
		return result;
	}

	public static string Capitalizewords(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		StringBuilder tempStringBuilder = GetTempStringBuilder();
		bool flag = true;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (char.IsWhiteSpace(c))
			{
				flag = true;
			}
			else if (flag && char.IsLetter(c))
			{
				c = char.ToUpper(c);
				flag = false;
			}
			tempStringBuilder.Append(c);
		}
		string result = tempStringBuilder.ToString();
		ReleaseBuilder(tempStringBuilder);
		return result;
	}

	public static bool Contains(string text, string value)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value))
		{
			return false;
		}
		return text.Contains(value);
	}

	public static string Downcase(string text)
	{
		return text?.ToLowerInvariant();
	}

	public static bool EndsWith(string text, string value)
	{
		if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(text))
		{
			return false;
		}
		return text.EndsWith(value);
	}

	public static string Handleize(string text)
	{
		StringBuilder tempStringBuilder = GetTempStringBuilder();
		char c = '\0';
		foreach (char c2 in text)
		{
			if (char.IsLetterOrDigit(c2))
			{
				c = c2;
				tempStringBuilder.Append(char.ToLowerInvariant(c2));
			}
			else if (c != '-')
			{
				tempStringBuilder.Append('-');
				c = '-';
			}
		}
		if (tempStringBuilder.Length > 0 && tempStringBuilder[tempStringBuilder.Length - 1] == '-')
		{
			tempStringBuilder.Length--;
		}
		string result = tempStringBuilder.ToString();
		ReleaseBuilder(tempStringBuilder);
		return result;
	}

	public static string LStrip(string text)
	{
		return text?.TrimStart();
	}

	public static string Pluralize(int number, string singular, string plural)
	{
		if (number != 1)
		{
			return plural;
		}
		return singular;
	}

	public static string Prepend(string text, string by)
	{
		return (by ?? string.Empty) + (text ?? string.Empty);
	}

	public static string Remove(string text, string remove)
	{
		if (string.IsNullOrEmpty(remove) || string.IsNullOrEmpty(text))
		{
			return text;
		}
		return text.Replace(remove, string.Empty);
	}

	public static string RemoveFirst(string text, string remove)
	{
		return ReplaceFirst(text, remove, string.Empty);
	}

	public static string Replace(string text, string match, string replace)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		match = match ?? string.Empty;
		replace = replace ?? string.Empty;
		return text.Replace(match, replace);
	}

	public static string ReplaceFirst(string text, string match, string replace)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		if (string.IsNullOrEmpty(match))
		{
			return text;
		}
		replace = replace ?? string.Empty;
		int num = text.IndexOf(match, StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return text;
		}
		StringBuilder tempStringBuilder = GetTempStringBuilder();
		tempStringBuilder.Append(text.Substring(0, num));
		tempStringBuilder.Append(replace);
		tempStringBuilder.Append(text.Substring(num + match.Length));
		string result = tempStringBuilder.ToString();
		ReleaseBuilder(tempStringBuilder);
		return result;
	}

	public static string RStrip(string text)
	{
		return text?.TrimEnd();
	}

	public static int Size(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return text.Length;
		}
		return 0;
	}

	public static string Slice(string text, int start, int length = 0)
	{
		if (string.IsNullOrEmpty(text) || start >= text.Length)
		{
			return string.Empty;
		}
		if (start < 0)
		{
			start += text.Length;
		}
		if (length <= 0)
		{
			length = text.Length;
		}
		if (start < 0)
		{
			if (start + length <= 0)
			{
				return string.Empty;
			}
			length += start;
			start = 0;
		}
		if (start + length > text.Length)
		{
			length = text.Length - start;
		}
		return text.Substring(start, length);
	}

	public static string Slice1(string text, int start, int length = 1)
	{
		if (string.IsNullOrEmpty(text) || start > text.Length || length <= 0)
		{
			return string.Empty;
		}
		if (start < 0)
		{
			start += text.Length;
		}
		if (start < 0)
		{
			length += start;
			start = 0;
		}
		if (start + length > text.Length)
		{
			length = text.Length - start;
		}
		return text.Substring(start, length);
	}

	public static IEnumerable Split(string text, string match)
	{
		if (string.IsNullOrEmpty(text))
		{
			return Enumerable.Empty<string>();
		}
		match = match ?? string.Empty;
		return text.Split(new string[1] { match }, StringSplitOptions.RemoveEmptyEntries);
	}

	public static bool StartsWith(string text, string value)
	{
		if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(text))
		{
			return false;
		}
		return text.StartsWith(value);
	}

	public static string Strip(string text)
	{
		return text?.Trim();
	}

	public static string StripNewlines(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		return Regex.Replace(text, "\\r\\n|\\r|\\n", string.Empty);
	}

	public static object ToInt(TemplateContext context, string text)
	{
		if (!int.TryParse(text, NumberStyles.Integer, context.CurrentCulture, out var result))
		{
			return null;
		}
		return result;
	}

	public static object ToLong(TemplateContext context, string text)
	{
		if (!long.TryParse(text, NumberStyles.Integer, context.CurrentCulture, out var result))
		{
			return null;
		}
		return result;
	}

	public static object ToFloat(TemplateContext context, string text)
	{
		if (!float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, context.CurrentCulture, out var result))
		{
			return null;
		}
		return result;
	}

	public static object ToDouble(TemplateContext context, string text)
	{
		if (!double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, context.CurrentCulture, out var result))
		{
			return null;
		}
		return result;
	}

	public static string Truncate(string text, int length, string ellipsis = null)
	{
		ellipsis = ellipsis ?? "...";
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		int num = length - ellipsis.Length;
		if (text.Length > length)
		{
			StringBuilder tempStringBuilder = GetTempStringBuilder();
			tempStringBuilder.Append(text, 0, (num >= 0) ? num : 0);
			tempStringBuilder.Append(ellipsis);
			text = tempStringBuilder.ToString();
			ReleaseBuilder(tempStringBuilder);
		}
		return text;
	}

	public static string Truncatewords(string text, int count, string ellipsis = null)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		StringBuilder tempStringBuilder = GetTempStringBuilder();
		bool flag = true;
		string[] array = Regex.Split(text, "\\s+");
		foreach (string value in array)
		{
			if (count <= 0)
			{
				break;
			}
			if (!flag)
			{
				tempStringBuilder.Append(' ');
			}
			tempStringBuilder.Append(value);
			flag = false;
			count--;
		}
		tempStringBuilder.Append("...");
		string result = tempStringBuilder.ToString();
		ReleaseBuilder(tempStringBuilder);
		return result;
	}

	public static string Upcase(string text)
	{
		return text?.ToUpperInvariant();
	}

	public static string Md5(string text)
	{
		text = text ?? string.Empty;
		using MD5 algo = MD5.Create();
		return Hash(algo, text);
	}

	public static string Sha1(string text)
	{
		using SHA1 algo = SHA1.Create();
		return Hash(algo, text);
	}

	public static string Sha256(string text)
	{
		using SHA256 algo = SHA256.Create();
		return Hash(algo, text);
	}

	public static string HmacSha1(string text, string secretKey)
	{
		using HMACSHA1 algo = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey ?? string.Empty));
		return Hash(algo, text);
	}

	public static string HmacSha256(string text, string secretKey)
	{
		using HMACSHA256 algo = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey ?? string.Empty));
		return Hash(algo, text);
	}

	private static string Hash(HashAlgorithm algo, string text)
	{
		text = text ?? string.Empty;
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		byte[] array = algo.ComputeHash(bytes);
		StringBuilder tempStringBuilder = GetTempStringBuilder();
		foreach (byte b in array)
		{
			tempStringBuilder.Append(b.ToString("x2"));
		}
		string result = tempStringBuilder.ToString();
		ReleaseBuilder(tempStringBuilder);
		return result;
	}

	public static string PadLeft(string text, int width)
	{
		return (text ?? string.Empty).PadLeft(width);
	}

	public static string PadRight(string text, int width)
	{
		return (text ?? string.Empty).PadRight(width);
	}

	public static string Base64Encode(string text)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(text ?? string.Empty));
	}

	public static string Base64Decode(string text)
	{
		byte[] bytes = Convert.FromBase64String(text ?? string.Empty);
		return Encoding.UTF8.GetString(bytes);
	}
}
