using System.Reflection;
using RimWorld.QuestGen;
using Verse;

namespace Milira;

public class QuestNode_GameComponent_ReadSetting : QuestNode
{
	[NoTranslate]
	public string componentBool;

	protected override bool TestRunInt(Slate slate)
	{
		FieldInfo field = typeof(MiliraGameComponent_OverallControl).GetField(componentBool, BindingFlags.Public);
		if (field == null)
		{
		}
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
