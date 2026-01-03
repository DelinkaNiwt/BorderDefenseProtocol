using Verse;

namespace Milira;

public class MilianModifySetting
{
	public static string CurrentMilianModifyScaleLabel(MilianModificationChanceScale scale)
	{
		MilianModifyChanceScaleInfo(scale, out var label, out var _);
		return label;
	}

	public static string CurrentMilianModifyScaleDesc(MilianModificationChanceScale scale)
	{
		MilianModifyChanceScaleInfo(scale, out var _, out var desc);
		return desc;
	}

	public static float MilianModifyChanceScale(MilianModificationChanceScale scale)
	{
		return scale switch
		{
			MilianModificationChanceScale.Off => 0f, 
			MilianModificationChanceScale.Small => 0.6f, 
			MilianModificationChanceScale.Normal => 1f, 
			MilianModificationChanceScale.More => 1.6f, 
			MilianModificationChanceScale.Full => 100f, 
			_ => 1f, 
		};
	}

	public static void MilianModifyChanceScaleInfo(MilianModificationChanceScale scale, out string label, out string desc)
	{
		label = "";
		desc = "";
		switch (scale)
		{
		case MilianModificationChanceScale.Off:
			label = "Milira.MilianModificationChance_Off".Translate();
			desc = "Milira.MilianModificationChance_OffDesc".Translate();
			break;
		case MilianModificationChanceScale.Small:
			label = "Milira.MilianModificationChance_Small".Translate();
			desc = "Milira.MilianModificationChance_SmallDesc".Translate();
			break;
		case MilianModificationChanceScale.Normal:
			label = "Milira.MilianModificationChance_Normal".Translate();
			desc = "Milira.MilianModificationChance_NormalDesc".Translate();
			break;
		case MilianModificationChanceScale.More:
			label = "Milira.MilianModificationChance_More".Translate();
			desc = "Milira.MilianModificationChance_MoreDesc".Translate();
			break;
		case MilianModificationChanceScale.Full:
			label = "Milira.MilianModificationChance_Full".Translate();
			desc = "Milira.MilianModificationChance_FullDesc".Translate();
			break;
		}
	}
}
