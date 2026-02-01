using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.AI;
using Verse.AI.Group;
using VEF;

namespace GD3
{
	[StaticConstructorOnStartup]
    public static class GDUtility
    {
		private static readonly Material LineMatRed = MaterialPool.MatFrom("Mote/ReinforceLine", ShaderDatabase.MoteGlow, Color.red);

		private static readonly Material ProjLineMatBlue = MaterialPool.MatFrom("Mote/ProjLine", ShaderDatabase.MoteGlow, Color.blue);

		public static Faction BlackMechanoid
		{
			get
			{
				return Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid);
			}
		}

		public static MainComponent MainComponent => Find.World.GetComponent<MainComponent>();

		public static MissionComponent MissionComponent => Find.World.GetComponent<MissionComponent>();

		public static GameComponent_ExtraDrawer ExtraDrawer => Current.Game.GetComponent<GameComponent_ExtraDrawer>();

		public static List<JobDef> shouldFlyJobDefs => new List<JobDef> { JobDefOf.Wait_Combat, JobDefOf.Goto, JobDefOf.AttackStatic, JobDefOf.UseVerbOnThing };

		public static bool CanSpawnReinforce => !GDSettings.AirforceNotApply;

		public static readonly List<QuestState> QuestStatesEnded = new List<QuestState>()
		{
			QuestState.EndedSuccess,
			QuestState.EndedFailed,
			QuestState.EndedInvalid,
			QuestState.EndedOfferExpired
		};

		public static readonly List<QuestState> QuestStatesSuccess = new List<QuestState>()
		{
			QuestState.EndedSuccess,
		};

		public static readonly List<QuestState> QuestStatesNotFailed = new List<QuestState>()
		{
			QuestState.EndedSuccess,
			QuestState.Ongoing,
			QuestState.NotYetAccepted,
		};

		public static readonly Texture2D SnowScreenTex = ContentFinder<Texture2D>.Get("Mote/SnowScreen");

		public static readonly Texture2D DialogBKGTex = ContentFinder<Texture2D>.Get("UI/Dialog/DialogBKG");

		public static readonly Texture2D WhiteBlockTex = ContentFinder<Texture2D>.Get("Mote/WhiteBlock");

		public static readonly Texture2D MechanoidsIconTex = ContentFinder<Texture2D>.Get("UI/MechanoidsIcon");

		public static readonly Texture2D BlackMechanoidsIconTex = ContentFinder<Texture2D>.Get("UI/Reward_BlackMech");

		public static Material FlightShadowMaterial => MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent, new Color(1f, 1f, 1f, 0.5f));

		public static readonly Color UnfilledColor = new Color(0.3f, 0.3f, 0.3f, 0.85f);

		public static readonly Color FilledColor = new Color(0.9f, 0.85f, 0.2f, 0.85f);

		public static readonly Color EngagingColor = new Color(1f, 0.96f, 0.56f, 0.30f);

		public static float AnnihilatorJumpSpeed = 1.2f;

		public static bool IsBlackMechanoid(this Pawn pawn)
        {
			IsBlackMechanoid ext = pawn.def.GetModExtension<IsBlackMechanoid>();
			if (ext != null)
            {
				return true;
            }
			return false;
        }

		public static bool IsBlackMechanoid(this Pawn pawn, out bool traitor)
		{
			traitor = false;
			IsBlackMechanoid ext = pawn.def.GetModExtension<IsBlackMechanoid>();
			if (ext != null)
			{
				traitor = ext.isTraitor;
				return true;
			}
			return false;
		}

		public static bool IsSpecialMech(this Pawn pawn)
        {
			if (pawn.IsBlackMechanoid())
            {
				return true;
            }
			return false;
        }

		public static Vector3 RandomPointInCircle(float radius, bool edge = false)
		{
			float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
			float distance = edge ? radius : UnityEngine.Random.Range(0f, radius);

			Vector2 randomFlatPoint = new Vector2(distance * Mathf.Cos(angle), distance * Mathf.Sin(angle));

			return new Vector3(randomFlatPoint.x, 0f, randomFlatPoint.y);
		}

		public static void LeaveFilthAtPawn(this Pawn pawn, ThingDef filth, int radius, int count)
		{
			if (pawn.MapHeld == null)
			{
				Log.Warning("Try to make filth but map is null.");
				return;
			}
			for (int i = 0; i < count; i++)
			{
				if (CellFinder.TryFindRandomCellNear(pawn.PositionHeld, pawn.MapHeld, radius, (IntVec3 c) => c.Standable(pawn.MapHeld) && !c.GetTerrain(pawn.MapHeld).IsWater, out var result))
				{
					FilthMaker.TryMakeFilth(result, pawn.MapHeld, filth);
				}
			}
		}

		public static void LeaveFilthAtPosition(IntVec3 pos, Map map, ThingDef filth, int radius, int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (CellFinder.TryFindRandomCellNear(pos, map, radius, (IntVec3 c) => c.Standable(map) && !c.GetTerrain(map).IsWater, out var result))
				{
					FilthMaker.TryMakeFilth(result, map, filth);
				}
			}
		}

		public static Rot4 GetRot(this Thing thing, Vector3 p, bool flip = false)
		{
			if (p == thing.DrawPos)
			{
				return Rot4.South;
			}
			float angle = (p - thing.DrawPos).AngleFlat();
			if (angle < 30f)
			{
				if (flip)
				{
					return Rot4.South;
				}
				return Rot4.North;
			}

			if (angle < 150f)
			{
				if (flip)
				{
					return Rot4.West;
				}
				return Rot4.East;
			}

			if (angle < 210f)
			{
				if (flip)
				{
					return Rot4.North;
				}
				return Rot4.South;
			}

			if (angle < 330f)
			{
				if (flip)
				{
					return Rot4.East;
				}
				return Rot4.West;
			}

			return Rot4.North;
		}

		public static void DrawHighlightLineBetween(Vector3 A, Vector3 B, float alpha, float lineWidth = 0.4f)
		{
			if (!(Mathf.Abs(A.x - B.x) < 0.01f) || !(Mathf.Abs(A.z - B.z) < 0.01f))
			{
				Vector3 pos = (A + B) / 2f;
				if (!(A == B))
				{
					A.y = B.y;
					float z = (A - B).MagnitudeHorizontal();
					Quaternion q = Quaternion.LookRotation(A - B);
					Vector3 s = new Vector3(lineWidth, 1f, z);
					Matrix4x4 matrix = default(Matrix4x4);
					matrix.SetTRS(pos, q, s);
					Material mat = LineMatRed;
					mat.color = new Color(1f, 0f, 0f, alpha);
					Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
				}
			}
		}

		public static void DrawProjHighlightLineBetween(Vector3 A, Vector3 B, float alpha, float lineWidth = 0.4f)
		{
			if (!(Mathf.Abs(A.x - B.x) < 0.01f) || !(Mathf.Abs(A.z - B.z) < 0.01f))
			{
				Vector3 pos = (A + B) / 2f;
				if (!(A == B))
				{
					A.y = B.y;
					float z = (A - B).MagnitudeHorizontal();
					Quaternion q = Quaternion.LookRotation(A - B);
					Vector3 s = new Vector3(lineWidth, 1f, z);
					Matrix4x4 matrix = default(Matrix4x4);
					matrix.SetTRS(pos, q, s);
					Material mat = ProjLineMatBlue;
					mat.color = new Color(0f, 0f, 0.4f, alpha);
					Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
				}
			}
		}

		public static bool CanReinforce(Map map)
        {
			Log.Message(WealthUtility.PlayerWealth);
			if (map.IsPocketMap) return false;
			if (WealthUtility.PlayerWealth < 200000) return false;
			if (!GDUtility.MainComponent.CanReinforce) return false;
			return CanSpawnReinforce;
        }

		public static void CallForReinforcement(IntVec3 cell, Map map, float? overridePoint = null, Action<TargetInfo> action = null)
        {
			ReinforceFlare flare = (ReinforceFlare)GenSpawn.Spawn(GDDefOf.GD_ReinforceFlare, cell, map);
			flare.overridePoint = overridePoint;
			if (action != null) action(new TargetInfo(cell, map));
			GDUtility.MainComponent.reinforceTick = GenTicks.TicksGame;
		}

		public static bool SpawnMosquitosAt(Map map, IntVec3 loc, int numRange, int radius)
		{
			List<Pawn> mosquitos = new List<Pawn>();
			for (int i = 0; i < numRange; i++)
            {
				Pawn pawn = PawnGenerator.GeneratePawn(GDDefOf.Mech_Mosquito, Faction.OfMechanoids);
				mosquitos.Add(pawn);
			}
			Lord lord = LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_AssaultColony(Faction.OfMechanoids), map, mosquitos);
			for (int i = 0; i < mosquitos.Count; i++)
			{
				IntVec3 intVec = CellFinder.RandomClosewalkCellNear(loc, map, radius);
				Pawn pawn = mosquitos[i];
				GenPlace.TryPlaceThing(SkyfallerMaker.MakeSkyfaller(ThingDefOf.FlyerArrival, pawn), intVec, map, ThingPlaceMode.Near);
				pawn.Rotation = Rot4.East;
			}
			return true;
		}

		public static bool IsFlyingMech(this Pawn pawn)
        {
			return pawn.def.HasModExtension<IsFlyingUnit>();
        }

		public static Thing FindBestThing(Pawn pawn, ThingDef need)
		{
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(need), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, Validator);
			bool Validator(Thing x)
			{
				if (x.IsForbidden(pawn) || !pawn.CanReserve(x))
				{
					return false;
				}

				return true;
			}
		}

		public static void SendSignal(this Quest quest, string signal)
        {
			List<string> list = new List<string>();
			if (quest != null)
			{
				if (DebugSettings.godMode)
				{
					Log.Message(quest.name);
				}
				list.Add("Quest");
				list.Add(quest.id.ToString());
				list.Add(".");
			}
			list.Add(signal);
			Find.SignalManager.SendSignal(new Signal(string.Concat(list)));
		}

		public static Quest GetQuestOfThing(this Thing thing)
		{
			Site site;
			if ((site = thing.MapHeld?.Parent as Site) != null)
			{
				List<Quest> quests = Find.QuestManager.QuestsListForReading;
				for (int i = 0; i < quests.Count; i++)
				{
					Quest quest = quests[i];
					if (quest.QuestLookTargets.Contains(site))
					{
						return quest;
					}
				}
			}
			return null;
		}

		public static Quest GetQuestOfPawn(this Pawn pawn)
		{
			List<Quest> quests = Find.QuestManager.QuestsListForReading;
			for (int i = 0; i < quests.Count; i++)
			{
				Quest quest = quests[i];
				if (quest.QuestLookTargets.Contains(pawn))
				{
					return quest;
				}
			}
			return null;
		}

		public static Quest GetQuestOfSite(this WorldObject site)
		{
			List<Quest> quests = Find.QuestManager.QuestsListForReading;
			for (int i = 0; i < quests.Count; i++)
			{
				Quest quest = quests[i];
				if (quest.QuestLookTargets.Contains(site))
				{
					return quest;
				}
			}
			return null;
		}

		public static void ForceEndQuestOfThing(this Thing thing, QuestEndOutcome outcome = QuestEndOutcome.Success)
		{
			Site site;
			if ((site = thing.MapHeld?.Parent as Site) != null)
			{
				site.ForceEndQuestOfSite(outcome);
			}
		}

		public static void ForceEndQuestOfSite(this Site site, QuestEndOutcome outcome = QuestEndOutcome.Success)
		{
			if (site.HasMap && site.Map.IsPlayerHome)
			{
				return;
			}
			List<Quest> quests = Find.QuestManager.QuestsListForReading;
			for (int i = 0; i < quests.Count; i++)
			{
				Quest quest = quests[i];
				if (quest.State == QuestState.Ongoing && quest.QuestLookTargets.Contains(site))
				{
					if (outcome == QuestEndOutcome.Success)
					{
						quest.End(QuestEndOutcome.Success);
					}
					else if (outcome == QuestEndOutcome.Fail)
					{
						quest.End(QuestEndOutcome.Fail);
					}
				}
			}
		}

		public static bool QuestExist(QuestScriptDef quest, List<QuestState> states = null)
		{
			List<Quest> list = Find.QuestManager.QuestsListForReading;
			if (list.Count > 0)
			{
				for (int i = 0; i < list.Count; i++)
				{
					Quest q = list[i];
					if (q.root.defName == quest.defName && (states.NullOrEmpty() || states.Contains(q.State)))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static void TrySpawnQuest(QuestScriptDef quest, Map map, bool avoidConflict = false, bool sendLetter = true)
		{
			bool hasThis = false;
			List<Quest> list = Find.QuestManager.QuestsListForReading;
			if (list.Count > 0)
			{
				for (int i = 0; i < list.Count; i++)
				{
					Quest q = list[i];
					if (q.root.defName == quest.defName)
					{
						hasThis = true;
						break;
					}
				}
			}
			IncidentParms parms = new IncidentParms();
			parms.points = StorytellerUtility.DefaultThreatPointsNow(map);
			parms.target = map;
			parms.sendLetter = sendLetter;
			Slate slate = new Slate();
			slate.Set("map", map);
			if ((hasThis && avoidConflict) || !quest.CanRun(slate, map))
			{
				return;
			}
			QuestUtility.SendLetterQuestAvailable(QuestUtility.GenerateQuestAndMakeAvailable(quest, parms.points));
		}

		public static void TryGeneratePawnInRandomCorner(Map map, LayoutRoom room, PawnKindDef pawnKindDef, Faction faction)
		{
			List<IntVec3> list = room.rects.SelectMany((CellRect r) => r.ContractedBy(1).Corners).Where(Predicate).ToList();
			if (list.Count != 0)
			{
				IntVec3 loc = list.RandomElement();
				Pawn pawn = PawnGenerator.GeneratePawn(pawnKindDef);
				pawn.SetFactionDirect(faction);
				GenSpawn.Spawn(pawn, loc, map);
			}

			bool Predicate(IntVec3 c)
			{
				if (c.GetEdifice(map) != null)
				{
					return false;
				}

				for (int i = 0; i < 4; i++)
				{
					if ((c + GenAdj.AdjacentCells[i]).GetFirstThing<Building_Trap>(map) != null)
					{
						return false;
					}
				}

				return true;
			}
		}

		public static bool TrueIncludes(this IntRange range, int val)
		{
			if (val >= range.min)
			{
				return val < range.max;
			}

			return false;
		}

		public static List<IntVec3> GetStraightLineRange(LocalTargetInfo center, Map map, int width, float _angle, float range = 100f, bool monodirectional = false, bool devTest = false)
        {
			List<IntVec3> list = new List<IntVec3>();
			float angle = -_angle + 90f;
			Vector3 direction = new Vector3((float)Mathf.Cos(angle * Mathf.PI / 180f), 0, (float)Mathf.Sin(angle * Mathf.PI / 180f)) * 0.8f;
			float num = 0;

			for (int j = 0; j < 2; j++)
            {
				if (j == 1)
                {
					if (monodirectional)
                    {
						break;
                    }
					direction = -direction;
                }

				Vector3 zero = center.CenterVector3;
				while (num < range)
				{
					num++;
					IntVec3 tmp = (zero + num * direction).ToIntVec3();
					if (!tmp.InBounds(map))
					{
						break;
					}
					if (list.Contains(tmp))
					{
						continue;
					}
					list.Add((zero + num * direction).ToIntVec3());
				}
				num = 0;

				for (int i = 1; i <= (width - 1) * 2; i++)
				{
					int index = i % 2 == 1 ? (i + 1) / 2 : -i / 2;
					zero = center.CenterVector3 + new Vector3(-(float)Mathf.Sin(angle * Mathf.PI / 180f) * i, 0, (float)Mathf.Cos(angle * Mathf.PI / 180f) * i);
					while (num < range)
					{
						num++;
						IntVec3 tmp = (zero + num * direction).ToIntVec3();
						if (!tmp.InBounds(map))
						{
							break;
						}
						if (list.Contains(tmp))
						{
							continue;
						}
						list.Add((zero + num * direction).ToIntVec3());
					}
					num = 0;
				}
			}
			
			list.RemoveAll(c => !c.IsValid || !c.InBounds(map));
			if (devTest)
            {
				foreach (IntVec3 c in list)
                {
					FleckMaker.ThrowMicroSparks(c.ToVector3Shifted(), map);
                }
            }
			return list;
		}

		public static float QualityFactor(this Thing thing)
        {
			if (thing.TryGetQuality(out QualityCategory quality))
            {
				switch (quality)
                {
					case QualityCategory.Awful : return 0.75f;
					case QualityCategory.Poor : return 0.9f;
					case QualityCategory.Normal: return 1f;
					case QualityCategory.Good: return 1f;
					case QualityCategory.Excellent: return 1f;
					case QualityCategory.Masterwork: return 1.25f;
					case QualityCategory.Legendary: return 1.5f;
					default : return 1f;
                }
            }
			return 1f;
        }

		public static float QualityFactor(QualityCategory quality)
		{
			switch (quality)
			{
				case QualityCategory.Awful: return 0.75f;
				case QualityCategory.Poor: return 0.9f;
				case QualityCategory.Normal: return 1f;
				case QualityCategory.Good: return 1f;
				case QualityCategory.Excellent: return 1f;
				case QualityCategory.Masterwork: return 1.25f;
				case QualityCategory.Legendary: return 1.5f;
				default: return 1f;
			}
		}
	}
}
