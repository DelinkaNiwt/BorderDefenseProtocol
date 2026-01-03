using System.Reflection;
using RimWorld.QuestGen;
using Verse;

namespace Milira;

public class QuestNode_ModSettingOn : QuestNode
{
	[NoTranslate]
	public SlateRef<string> modSettingSwitch;

	protected override bool TestRunInt(Slate slate)
	{
		FieldInfo field = typeof(MiliraRaceSettings).GetField(modSettingSwitch.GetValue(QuestGen.slate), BindingFlags.Static | BindingFlags.Public);
		if (field != null && field.FieldType == typeof(bool) && !(bool)field.GetValue(null))
		{
			return false;
		}
		return true;
	}

	protected override void RunInt()
	{
	}
}
