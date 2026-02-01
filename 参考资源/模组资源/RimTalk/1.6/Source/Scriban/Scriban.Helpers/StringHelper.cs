using System.Collections;
using System.Text;

namespace Scriban.Helpers;

internal class StringHelper
{
	public static string Join(string separator, IEnumerable items)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		foreach (object item in items)
		{
			if (!flag)
			{
				stringBuilder.Append(separator);
			}
			stringBuilder.Append(item);
			flag = false;
		}
		return stringBuilder.ToString();
	}
}
