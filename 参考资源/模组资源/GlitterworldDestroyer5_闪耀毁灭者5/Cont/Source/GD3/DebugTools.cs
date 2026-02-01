using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using UnityEngine;
using System.Text;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace GD3
{
    public static class DebugTools
    {
		private static PrefabDef buffer;

		private static readonly int[] QuestNumOptions = new int[7] { 1, 2, 5, 10, 25, 50, 100 };

		private static readonly float[] QuestRewardDebugPointLevels = new float[8] { 35f, 100f, 200f, 400f, 800f, 1600f, 3200f, 6000f };

		[DebugAction("Glitterworld Destroyer", "Bad relation with black mechanoid", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void BadRelationWithBlack()
		{
			GDUtility.MissionComponent.BlackMechRelationOffset(-200);
		}

		[DebugAction("Glitterworld Destroyer", "Good relation with black mechanoid", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void GoodRelationWithBlack()
		{
			GDUtility.MissionComponent.factionRelationLock = false;
			GDUtility.BlackMechanoid.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, false);
			GDUtility.MissionComponent.BlackMechRelationOffset(200);
		}

		[DebugAction("Glitterworld Destroyer", "Call for reinforcement", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CallForReinforcement()
        {
            GDUtility.CallForReinforcement(UI.MouseCell(), Find.CurrentMap, null, delegate (TargetInfo tar)
			{
				GDDefOf.GDReinforceFlare.PlayOneShot(tar);
			});
        }

        [DebugAction("Glitterworld Destroyer", "Reset artillery cooldown", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ResetArtilleryCooldown()
        {
            GDUtility.MainComponent.artilleryStrikeCooldown = -1;
        }

		[DebugAction("Glitterworld Destroyer", "Create prefab", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void CreatePrefab()
		{
			DebugToolsGeneral.GenericRectTool("Create", delegate (CellRect rect)
			{
				PrefabDef prefabDef = PrefabUtility.CreatePrefab(rect, DebugGenerationSettings.prefabCopyAllThings, true);
				StringBuilder stringBuilder = new StringBuilder();
				string text = "  ";
				stringBuilder.AppendLine("\n<PrefabDef>");
				stringBuilder.AppendLine(text + "<defName>NewPrefab</defName> <!-- rename -->");
				stringBuilder.AppendLine($"{text}<size>({rect.Size.x},{rect.Size.z})</size>");
				List<PrefabThingData> things = (List<PrefabThingData>)typeof(PrefabDef).GetField("things", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(prefabDef);
				List<PrefabTerrainData> terrain = (List<PrefabTerrainData>)typeof(PrefabDef).GetField("terrain", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(prefabDef);
				if (things.CountAllowNull() > 0)
				{
					stringBuilder.AppendLine(text + "<things>");
					for (int i = 0; i < things.Count; i++)
					{
						PrefabThingData prefabThingData = things[i];
						stringBuilder.AppendLine(text + text + "<" + prefabThingData.def.defName + ">");
						if (prefabThingData.rects != null)
						{
							stringBuilder.AppendLine(text + text + text + "<rects>");
							foreach (CellRect rect0 in prefabThingData.rects)
							{
								stringBuilder.AppendLine($"{text}{text}{text}{text}<li>{rect0}</li>");
							}
							stringBuilder.AppendLine(text + text + text + "</rects>");
						}
						else if (prefabThingData.positions != null)
						{
							stringBuilder.AppendLine(text + text + text + "<positions>");
							foreach (IntVec3 position in prefabThingData.positions)
							{
								stringBuilder.AppendLine($"{text}{text}{text}{text}<li>{position}</li>");
							}
							stringBuilder.AppendLine(text + text + text + "</positions>");
						}
						else
						{
							stringBuilder.AppendLine($"{text}{text}{text}<position>{prefabThingData.position}</position>");
						}
						if (prefabThingData.relativeRotation != 0)
						{
							stringBuilder.AppendLine(text + text + text + "<relativeRotation>" + Enum.GetName(typeof(RotationDirection), prefabThingData.relativeRotation) + "</relativeRotation>");
						}
						if (prefabThingData.stuff != null)
						{
							stringBuilder.AppendLine(text + text + text + "<stuff>" + prefabThingData.stuff.defName + "</stuff>");
						}
						if (prefabThingData.quality.HasValue)
						{
							stringBuilder.AppendLine($"{text}{text}{text}<quality>{prefabThingData.quality}</quality>");
						}
						if (prefabThingData.hp != 0)
						{
							stringBuilder.AppendLine($"{text}{text}{text}<hp>{prefabThingData.hp}</hp>");
						}
						if (prefabThingData.stackCountRange != IntRange.One)
						{
							stringBuilder.AppendLine($"{text}{text}{text}<stackCountRange>{prefabThingData.stackCountRange.min}~{prefabThingData.stackCountRange.max}</stackCountRange>");
						}
						if (prefabThingData.colorDef != null)
						{
							stringBuilder.AppendLine($"{text}{text}{text}<colorDef>{prefabThingData.colorDef}</colorDef>");
						}
						if (prefabThingData.color != default(Color))
						{
							stringBuilder.AppendLine($"{text}{text}{text}<color>{prefabThingData.color}</color>");
						}
						stringBuilder.AppendLine(text + text + "</" + prefabThingData.def.defName + ">");
					}
					stringBuilder.AppendLine(text + "</things>");
				}
				if (terrain.CountAllowNull() > 0)
				{
					stringBuilder.AppendLine(text + "<terrain>");
					foreach (PrefabTerrainData item in terrain)
					{
						stringBuilder.AppendLine(text + text + "<" + item.def.defName + ">");
						if (item.color != null)
						{
							stringBuilder.AppendLine($"{text}{text}{text}<color>{item.color}</color>");
						}
						stringBuilder.AppendLine(text + text + text + "<rects>");
						foreach (CellRect rect2 in item.rects)
						{
							stringBuilder.AppendLine($"{text}{text}{text}{text}<li>{rect2}</li>");
						}
						stringBuilder.AppendLine(text + text + text + "</rects>");
						stringBuilder.AppendLine(text + text + "</" + item.def.defName + ">");
					}
					stringBuilder.AppendLine(text + "</terrain>");
				}
				stringBuilder.AppendLine("</PrefabDef>");
				buffer = prefabDef;
				GUIUtility.systemCopyBuffer = stringBuilder.ToString();
				Messages.Message("Copied to clipboard", MessageTypeDefOf.NeutralEvent, historical: false);
			}, closeOnComplete: true);
		}

		[DebugAction("Glitterworld Destroyer", "Advance Progress +5", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void AdvanceProgress()
		{
			GDUtility.MissionComponent.progress += 5;
		}

		[DebugAction("Glitterworld Destroyer", "Quest can run?", false, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static List<DebugActionNode> TestQuest()
		{
			return TestQuests();
		}

		private static List<DebugActionNode> TestQuests()
		{
			List<DebugActionNode> list = new List<DebugActionNode>();
			foreach (QuestScriptDef item in DefDatabase<QuestScriptDef>.AllDefs.Where((QuestScriptDef x) => x.IsRootAny))
			{
				QuestScriptDef localScriptDef = item;
				DebugActionNode debugActionNode = new DebugActionNode(localScriptDef.defName);
				Slate slate2 = new Slate();
				slate2.Set("discoveryMethod", "QuestDiscoveredFromDebug".Translate());
				debugActionNode.action = delegate
				{
					Log.Warning(localScriptDef.CanRun(slate2, Find.CurrentMap as IIncidentTarget ?? Find.World).ToString());
				};
				list.Add(debugActionNode);
			}
			return list.OrderBy((DebugActionNode op) => op.label).ToList();
		}
	}
}
