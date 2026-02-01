using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

[StaticConstructorOnStartup]
public class Building_Ancient_WarBeacon : Building
{
	private static readonly Material filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f));

	private static readonly Material emptyMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));

	private static readonly Vector2 BarSize = new Vector2(1f, 0.14f);

	public bool allowFilling;

	public int stage;

	public List<ThingDefCountClass> requiredThings = new List<ThingDefCountClass>();

	public CompPowerTrader compPower;

	private Gizmo toggleGizmo;

	public int Progress;

	public ModExtension_AncientWarBeacon extension => def.GetModExtension<ModExtension_AncientWarBeacon>();

	public Gizmo ToggleGizmo
	{
		get
		{
			if (toggleGizmo == null)
			{
				RefreshGizmo();
			}
			return toggleGizmo;
		}
	}

	public bool stageValid => stage < extension.stages.Count;

	public bool matFulfilled => requiredThings.NullOrEmpty();

	public DragonGraveStageReq curStage => extension.stages[stage];

	public DragonGraveStageReq curStageii => extension.stages[stage - 1];

	public override Graphic Graphic
	{
		get
		{
			if (stageValid && curStage.graphic != null)
			{
				return curStage.graphic.Graphic;
			}
			return base.Graphic;
		}
	}

	public void RefreshGizmo()
	{
		Texture icon = ContentFinder<Texture2D>.Get(allowFilling ? extension.toggleGizmoIcon : extension.toggleGizmoOffIcon, reportFailure: false);
		string defaultLabel = (allowFilling ? extension.toggleGizmoLabel.Translate() : extension.toggleGizmoOffLabel.Translate());
		toggleGizmo = new Command_Action
		{
			action = delegate
			{
				allowFilling = !allowFilling;
				RefreshGizmo();
			},
			defaultLabel = defaultLabel,
			icon = icon
		};
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref allowFilling, "allowFilling", defaultValue: false);
		Scribe_Values.Look(ref stage, "stage", 0);
		Scribe_Collections.Look(ref requiredThings, "requiredThings", LookMode.Deep);
	}

	public override void PostMake()
	{
		base.PostMake();
		CopyStageReq();
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		compPower = this.TryGetComp<CompPowerTrader>();
	}

	public bool TryAcceptThing(Thing thing)
	{
		if (thing == null || !allowFilling || !stageValid)
		{
			return false;
		}
		int index = requiredThings.FirstIndexOf((ThingDefCountClass t) => t.thingDef == thing.def);
		if (index < 0)
		{
			return false;
		}
		requiredThings[index].count -= thing.stackCount;
		thing.Destroy();
		if (requiredThings[index].count <= 0)
		{
			requiredThings.RemoveAt(index);
		}
		if (matFulfilled && curStage.raidWhenCountdownStart)
		{
			DoRaid();
		}
		return true;
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		if (Progress > 0)
		{
			GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
			{
				center = DrawPos + Vector3.up * 0.1f + Vector3.forward * 0.1f,
				size = BarSize,
				fillPercent = (float)Progress / (float)curStage.timeDuration,
				filledMat = filledMat,
				unfilledMat = emptyMat,
				margin = 0.15f
			});
		}
	}

	protected override void Tick()
	{
		base.Tick();
		DoProgress();
	}

	public override void TickRare()
	{
		base.TickRare();
		DoProgress(250);
	}

	public override void TickLong()
	{
		base.TickLong();
		DoProgress(2000);
	}

	public void DoProgress(int i = 1)
	{
		if (compPower != null)
		{
			if (matFulfilled)
			{
				compPower.PowerOutput = 0f - curStage.rechargePower;
			}
			else
			{
				compPower.PowerOutput = 0f - base.PowerComp.Props.idlePowerDraw;
			}
		}
		if (matFulfilled && (compPower == null || compPower.PowerOn))
		{
			Progress += i;
			if (Progress >= curStage.timeDuration)
			{
				Upgrade();
			}
		}
	}

	public int RequiredCountFor(ThingDef def)
	{
		if (requiredThings.NullOrEmpty())
		{
			return 0;
		}
		int index = requiredThings.FirstIndexOf((ThingDefCountClass t) => t.thingDef == def);
		return (index >= 0) ? requiredThings[index].count : 0;
	}

	public void CopyStageReq()
	{
		foreach (ThingDefCountClass thingDefCountClass in curStage.Things)
		{
			ThingDefCountClass item = new ThingDefCountClass(thingDefCountClass.thingDef, thingDefCountClass.count);
			requiredThings.Add(item);
		}
		Progress = 0;
	}

	public void Upgrade()
	{
		try
		{
			if (extension == null)
			{
				Log.Error("Extension is null in Rebuilding()");
				return;
			}
			if (!stageValid)
			{
				Log.Warning($"Invalid stage {stage} during Rebuilding");
				return;
			}
			DragonGraveStageReq currentStage = curStage;
			if (currentStage == null)
			{
				Log.Error("Current stage is null");
				return;
			}
			bool isShellcoreBastion = extension.finalThing?.defName == "TW_Complete_Shellcore_Bastion";
			allowFilling = false;
			RefreshGizmo();
			if (compPower != null)
			{
				compPower.PowerOutput = 0f - (base.PowerComp?.Props.idlePowerDraw ?? 0f);
			}
			if (currentStage.Rewards != null && !isShellcoreBastion)
			{
				NewSelect(base.Position, base.Map);
			}
			if (!currentStage.raidWhenCountdownStart)
			{
				DoRaid();
			}
			stage++;
			if (!stageValid)
			{
				if (extension.finalPawnKind != null)
				{
					Pawn newThing = PawnGenerator.GeneratePawn(extension.finalPawnKind, Faction.OfPlayer);
					GenSpawn.Spawn(newThing, base.Position, base.Map, Rot4.Random);
				}
				if (extension.finalThing != null)
				{
					ConvertToFinalBuilding();
				}
			}
			else
			{
				CopyStageReq();
			}
		}
		catch (Exception arg)
		{
			Log.Error($"Rebuild failed: {arg}");
		}
	}

	private void ConvertToFinalBuilding()
	{
		try
		{
			bool wasSelected = Find.Selector.IsSelected(this);
			IntVec3 pos = base.Position;
			Map map = base.Map;
			Rot4 rot = base.Rotation;
			DeSpawn();
			Thing finalThing = ThingMaker.MakeThing(extension.finalThing);
			finalThing.SetFaction(Faction.OfPlayer);
			GenSpawn.Spawn(finalThing, pos, map, rot);
			if (wasSelected)
			{
				Find.Selector.Select(finalThing);
			}
			Destroy();
		}
		catch (Exception arg)
		{
			Log.Error($"WarBeacon Final conversion failed: {arg}");
		}
	}

	private void NewSelect(IntVec3 intVec, Map map)
	{
		string title = "NCL_WARBEACON_REACTIVATE_TITLE".Translate();
		string title2 = "SecondWarBeacon".Translate();
		string title3 = "ThirdWarBeacon".Translate();
		string str = "NCL_WARBEACON_REACTIVATE_DESCRIPTION".Translate();
		string str2 = "SecondWarBeaconDec".Translate();
		string str3 = "ThirdWarBeaconDec".Translate();
		string text = "NCL_WARBEACON_ELLIPSES".Translate();
		string FirstLog = "NCL_WARBEACON_FIRST_LOG".Translate();
		string SecondLog = "SecondWarBeaconLog".Translate();
		DiaNode diaNode = new DiaNode(str);
		DiaNode diaNode2 = new DiaNode(str2);
		DiaNode diaNode3 = new DiaNode(str3);
		DiaOption item = new DiaOption(text)
		{
			action = delegate
			{
				Messages.Message(FirstLog, MessageTypeDefOf.PositiveEvent);
				foreach (ThingDefCountRangeClass current in curStageii.Rewards)
				{
					int num = current.countRange.RandomInRange;
					if (num > 0)
					{
						while (num > 0)
						{
							Thing thing = ThingMaker.MakeThing(current.thingDef);
							thing.stackCount = ((num > current.thingDef.stackLimit) ? current.thingDef.stackLimit : num);
							num -= current.thingDef.stackLimit;
							GenDrop.TryDropSpawn(thing, intVec, map, ThingPlaceMode.Near, out var _);
						}
					}
				}
			},
			resolveTree = true
		};
		diaNode.options.Add(item);
		diaNode2.options.Add(item);
		diaNode3.options.Add(item);
		switch (stage)
		{
		default:
			Find.WindowStack.Add(new Dialog_NodeTree(diaNode3, delayInteractivity: true, radioMode: true, title3));
			break;
		case 1:
			Find.WindowStack.Add(new Dialog_NodeTree(diaNode2, delayInteractivity: true, radioMode: true, title2));
			break;
		case 0:
			Find.WindowStack.Add(new Dialog_NodeTree(diaNode, delayInteractivity: true, radioMode: true, title));
			break;
		}
	}

	public void DoRaid()
	{
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		if (mode != DestroyMode.Vanish && stageValid)
		{
			if (extension.finalPawnKind != null)
			{
				Pawn pawn = PawnGenerator.GeneratePawn(extension.finalPawnKind, Faction.OfAncientsHostile);
				GenSpawn.Spawn(pawn, base.Position, base.Map, Rot4.Random);
				pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
			}
			else
			{
				Log.Error("[NCL] Building " + def.defName + " tried to spawn a pawn on destroy, but its finalPawnKind is null in the ModExtension XML.");
			}
			foreach (ThingDefCountClass thingDefCountClass in curStage.Things)
			{
				int i = thingDefCountClass.count;
				i -= RequiredCountFor(thingDefCountClass.thingDef);
				if (i > 0)
				{
					while (i > 0)
					{
						Thing thing = ThingMaker.MakeThing(thingDefCountClass.thingDef);
						thing.stackCount = ((i > thingDefCountClass.thingDef.stackLimit) ? thingDefCountClass.thingDef.stackLimit : i);
						i -= thingDefCountClass.thingDef.stackLimit;
						GenDrop.TryDropSpawn(thing, base.Position, base.Map, ThingPlaceMode.Near, out var _);
					}
				}
			}
		}
		base.Destroy(mode);
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		yield return ToggleGizmo;
		if (!DebugSettings.ShowDevGizmos)
		{
			yield break;
		}
		if (!matFulfilled)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Finish requirement",
				action = delegate
				{
					requiredThings.Clear();
					if (curStage.raidWhenCountdownStart)
					{
						DoRaid();
					}
				}
			};
		}
		yield return new Command_Action
		{
			defaultLabel = "DEV: upgrade",
			action = delegate
			{
				Upgrade();
			}
		};
	}

	public override string GetInspectStringLowPriority()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("NCL_WARBEACON_INSPECT_DESCRIPTION".Translate());
		if (matFulfilled)
		{
			stringBuilder.AppendLine("NCL_WARBEACON_REPAIR_TIME".Translate() + (curStage.timeDuration - Progress).ToStringTicksToDays());
		}
		else
		{
			stringBuilder.AppendLine("NCL_WARBEACON_REQUIRED_RESOURCES".Translate());
			foreach (ThingDefCountClass thingDefCountClass in requiredThings)
			{
				stringBuilder.AppendLine(thingDefCountClass.thingDef.label.ToString() + ": " + thingDefCountClass.count);
			}
		}
		string text = stringBuilder.ToString();
		return text.TrimEnd();
	}
}
