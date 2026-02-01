using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
	public class WorldObjectCompProperties_BuildRequest : WorldObjectCompProperties
	{
		public WorldObjectCompProperties_BuildRequest()
		{
			compClass = typeof(BuildRequestComp);
		}
	}

	[StaticConstructorOnStartup]
	public class BuildRequestComp : WorldObjectComp
	{
		public List<ThingDef> requestThingDefs;

		public List<int> requestCountList;

		public string outSignalFulfilled;

		private static readonly Texture2D TradeCommandTex = ContentFinder<Texture2D>.Get("UI/Symbols/AllyGiantCluster_constructing");

		public bool activeRequest = true;

		public int builtTicksGame = -1;

		private static readonly int daysToBeReady = 25;

        public override void CompTickInterval(int delta)
        {
            if (!activeRequest && builtTicksGame > 0 && builtTicksGame <= Find.TickManager.TicksGame)
            {
				Spawn();
            }
        }

        public override string CompInspectStringExtra()
		{
			if (activeRequest)
			{
				List<NamedArgument> strings = new List<NamedArgument>();
				for	(int i = 0; i < requestThingDefs.Count; i++)
                {
					if (requestCountList[i] <= 0)
                    {
						strings.Add("GD.ResourceFull".Translate(requestThingDefs[i]));
						continue;
                    }
					strings.Add(GenLabel.ThingLabel(requestThingDefs[i], null, requestCountList[i]).Translate());
				}
				return TranslatorFormattedStringExtensions.Translate("GD.CaravanRequestInfo", strings.ToArray());
			}
            else
            {
				return "GD.ClusterToBeReady".Translate((builtTicksGame - Find.TickManager.TicksGame).ToStringTicksToDays());
            }
		}

		public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
		{
			if (activeRequest && ObjectVisitedNow(caravan) == parent)
			{
				yield return FulfillRequestCommand(caravan);
			}
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			if (DebugSettings.ShowDevGizmos)
			{
				if (activeRequest)
                {
					Command_Action command_Action = new Command_Action();
					command_Action.defaultLabel = "DEV: Fill resource";
					command_Action.action = delegate
					{
						QuestUtility.SendQuestTargetSignals(parent.questTags, "Built", parent.Named("SUBJECT"));
						Disable();
						builtTicksGame = Find.TickManager.TicksGame + daysToBeReady * 60000;
					};
					yield return command_Action;
				}
                else
                {
					Command_Action command_Action = new Command_Action();
					command_Action.defaultLabel = "DEV: Cluster finish now";
					command_Action.action = delegate
					{
						builtTicksGame = Find.TickManager.TicksGame;
					};
					yield return command_Action;
				}
			}
		}

		public void Disable()
		{
			activeRequest = false;
		}

		public void Spawn()
        {
			PlanetTile tile = parent.Tile;
			parent.Destroy();
			WorldObject worldObject = WorldObjectMaker.MakeWorldObject(GDDefOf.GD_AllyCluster);
			worldObject.Tile = tile;
			worldObject.SetFaction(Faction.OfMechanoids);
			Find.WorldObjects.Add(worldObject);
			Find.LetterStack.ReceiveLetter("GD.ClusterBuilt".Translate(), "GD.ClusterBuiltDesc".Translate(), LetterDefOf.PositiveEvent, worldObject, null, parent.GetQuestOfSite());
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref requestThingDefs, "requestThingDefs", LookMode.Def);
			Scribe_Collections.Look(ref requestCountList, "requestCountList", LookMode.Value);
			Scribe_Values.Look(ref activeRequest, "activeRequest", true);
			Scribe_Values.Look(ref builtTicksGame, "builtTicksGame", -1);
			BackCompatibility.PostExposeData(this);
		}

		private Command FulfillRequestCommand(Caravan caravan)
		{
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = "GD.CommandFulfillBuildOffer".Translate();
			command_Action.defaultDesc = "GD.CommandFulfillBuildOfferDesc".Translate();
			command_Action.icon = TradeCommandTex;
			command_Action.action = delegate
			{
				if (!activeRequest)
				{
					Log.Error("Attempted to fulfill an unavailable request");
				}
				else
                {
					Dictionary<ThingDef, int> tmpDict = new Dictionary<ThingDef, int>();
					for (int i = 0; i < requestThingDefs.Count; i++)
                    {
						if (CaravanInventoryUtility.HasThings(caravan, requestThingDefs[i], 1))
						{
							int num = CaravanInventoryUtility.AllInventoryItems(caravan).Where(t => t.def == requestThingDefs[i]).Sum(t => t.stackCount);
							int remaining = requestCountList[i];
							tmpDict.Add(requestThingDefs[i], Math.Min(num, remaining));
						}
                        else
                        {
							tmpDict.Add(requestThingDefs[i], 0);
						}
                    }
					if (tmpDict.NullOrEmpty())
					{
						Messages.Message("GD.FailToFillCluster".Translate(), MessageTypeDefOf.RejectInput, historical: false);
					}
					else
					{
						List<NamedArgument> strings = new List<NamedArgument>();
						for (int i = 0; i < tmpDict.Count; i++)
						{
							if (requestCountList[i] <= 0)
                            {
								strings.Add("GD.ResourceFull".Translate(requestThingDefs[i]));
								continue;
                            }
							else if (tmpDict.ElementAt(i).Value <= 0)
							{
								strings.Add("GD.NoThisResource".Translate(requestThingDefs[i]));
								continue;
							}
							strings.Add(GenLabel.ThingLabel(tmpDict.ElementAt(i).Key, null, tmpDict.ElementAt(i).Value));
						}
						Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(TranslatorFormattedStringExtensions.Translate("GD.CommandFulfillBuildOfferConfirm", strings.ToArray()), delegate
						{
							Fulfill(caravan);
						}));
					}
				}
			};
			bool flag = false;
			for (int i = 0; i < requestThingDefs.Count; i++)
			{
				if (CaravanInventoryUtility.HasThings(caravan, requestThingDefs[i], 1))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				command_Action.Disable("GD.FailToFillCluster".Translate());
			}
			return command_Action;
		}

		private void Fulfill(Caravan caravan)
		{
			for (int i = 0; i < requestThingDefs.Count; i++)
			{
				if (CaravanInventoryUtility.HasThings(caravan, requestThingDefs[i], 1) && requestCountList[i] > 0)
				{
					int remaining = requestCountList[i];
					int all = 0;
					List<Thing> list = CaravanInventoryUtility.TakeThings(caravan, delegate (Thing thing)
					{
						if (requestThingDefs[i] != thing.def)
						{
							return 0;
						}
						int num = Mathf.Min(remaining, thing.stackCount);
						remaining -= num;
						return num;
					});
					for (int j = 0; j < list.Count; j++)
					{
						all += list[j].stackCount;
						list[j].Destroy();
					}
					Messages.Message("GD.ClusterReceiveResource".Translate(GenLabel.ThingLabel(requestThingDefs[i], null, all)), MessageTypeDefOf.PositiveEvent);
					requestCountList[i] -= all;
				}
			}
			if (!requestCountList.Any(i => i > 0))
            {
				QuestUtility.SendQuestTargetSignals(parent.questTags, "Built", parent.Named("SUBJECT"), caravan.Named("CARAVAN"));
				Disable();
				builtTicksGame = Find.TickManager.TicksGame + daysToBeReady * 60000;
			}
			SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
		}

		public static WorldObject ObjectVisitedNow(Caravan caravan)
		{
			if (!caravan.Spawned || caravan.pather.Moving)
			{
				return null;
			}

			List<WorldObject> settlementBases = Find.WorldObjects.AllWorldObjects;
			for (int i = 0; i < settlementBases.Count; i++)
			{
				WorldObject settlement = settlementBases[i];
				if (settlement.Tile == caravan.Tile && settlement.Faction != caravan.Faction)
				{
					return settlement;
				}
			}

			return null;
		}
	}
}
