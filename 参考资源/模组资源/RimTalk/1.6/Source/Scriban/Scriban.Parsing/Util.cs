namespace Scriban.Parsing;

internal static class Util
{
	public static bool IsHex(this char c)
	{
		if ((c < '0' || c > '9') && (c < 'a' || c > 'f'))
		{
			if (c >= 'A')
			{
				return c <= 'F';
			}
			return false;
		}
		return true;
	}

	public static int HexToInt(this char c)
	{
		if (c >= '0' && c <= '9')
		{
			return c - 48;
		}
		if (c >= 'a' && c <= 'f')
		{
			return c - 97 + 10;
		}
		if (c >= 'A' && c <= 'F')
		{
			return c - 65 + 10;
		}
		return 0;
	}
}
