using System.Reflection;
using System.Text;

namespace Scriban.Runtime;

public sealed class StandardMemberRenamer
{
	public static readonly MemberRenamerDelegate Default = Rename;

	public static string Rename(MemberInfo member)
	{
		return Rename(member.Name);
	}

	public static string Rename(string name)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			if (char.IsUpper(c))
			{
				if (i > 0 && !flag)
				{
					stringBuilder.Append("_");
				}
				stringBuilder.Append(char.ToLowerInvariant(c));
				flag = true;
			}
			else
			{
				stringBuilder.Append(c);
				flag = false;
			}
		}
		return stringBuilder.ToString();
	}
}
