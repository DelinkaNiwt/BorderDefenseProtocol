using System.Text;
using Verse;

namespace AncotLibrary;

public class HediffWithSeverityLabel : HediffWithComps
{
	public override string LabelBase
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.LabelBase);
			stringBuilder.Append(": ");
			stringBuilder.Append((base.Severity / def.maxSeverity).ToStringPercent());
			return stringBuilder.ToString();
		}
	}
}
