using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Milira;

[StaticConstructorOnStartup]
public class WorldObjectCompMiliraSettlement : WorldObjectComp
{
	protected bool ValidSettlement => parent.Faction?.def == MiliraDefOf.Milira_Faction;

	protected Settlement Settlement => parent as Settlement;

	private Pawn FallenMilira => MiliraGameComponent_OverallControl.OverallControl.pawn;

	public override void Initialize(WorldObjectCompProperties props)
	{
		base.props = props;
	}

	public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
	{
		if (ValidSettlement && FallenMilira != null && caravan.ContainsPawn(FallenMilira))
		{
			yield return new Command_Action
			{
				defaultLabel = "Milira.HandOverFallenAngel".Translate(FallenMilira.LabelShort),
				defaultDesc = "Milira.HandOverFallenAngelDesc".Translate(FallenMilira.LabelShort),
				icon = MiliraIcon.MiliraGivePawn,
				action = delegate
				{
					caravan.RemovePawn(FallenMilira);
					MiliraGameComponent_OverallControl.OverallControl.turnToFriend_Pre = true;
					MiliraGameComponent_OverallControl.OverallControl.CheckMiliraPermanentEnemyStatus();
					GoodwillSituationManager goodwillSituationManager = Find.FactionManager.goodwillSituationManager;
					MethodInfo method = goodwillSituationManager.GetType().GetMethod("RecalculateAll", BindingFlags.Instance | BindingFlags.Public);
					method.Invoke(goodwillSituationManager, new object[1] { false });
					Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
					faction.SetRelation(new FactionRelation
					{
						other = Faction.OfPlayer,
						baseGoodwill = -60,
						kind = FactionRelationKind.Neutral
					});
				}
			};
		}
	}
}
