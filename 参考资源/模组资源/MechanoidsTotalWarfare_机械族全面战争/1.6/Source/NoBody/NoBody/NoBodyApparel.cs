using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NoBody;

public class NoBodyApparel : Apparel
{
	private Command_ActionWithFloat _cachedBodyTypeCommand;

	public override IEnumerable<Gizmo> GetWornGizmos()
	{
		if (Find.Selector.SingleSelectedThing != base.Wearer || !DebugSettings.godMode)
		{
			yield break;
		}
		if (_cachedBodyTypeCommand != null && _cachedBodyTypeCommand.action != null)
		{
			yield return _cachedBodyTypeCommand;
			yield break;
		}
		_cachedBodyTypeCommand = new Command_ActionWithFloat
		{
			defaultLabel = "切换身形",
			defaultDesc = "为这个角色选择不同的身形。",
			floatMenuGetter = GetBodyTypeOptions,
			action = delegate
			{
				Messages.Message("请选择一个新的身形", base.Wearer, MessageTypeDefOf.PositiveEvent);
			}
		};
		yield return _cachedBodyTypeCommand;
	}

	private List<FloatMenuOption> GetBodyTypeOptions()
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		Pawn pawn = base.Wearer;
		if (pawn == null || pawn.story == null)
		{
			return list;
		}
		foreach (BodyTypeDef bodyTypeDef in DefDatabase<BodyTypeDef>.AllDefsListForReading)
		{
			list.Add(new FloatMenuOption(bodyTypeDef.defName, delegate
			{
				pawn.story.bodyType = bodyTypeDef;
				PortraitsCache.PortraitsCacheUpdate();
				Messages.Message($"已将 {pawn.LabelShortCap} 的身形切换为 {bodyTypeDef.LabelCap}", pawn, MessageTypeDefOf.PositiveEvent);
			}));
		}
		return list;
	}
}
