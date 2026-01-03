using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class CompTransporterCustom : CompTransporter
{
	private static readonly Texture2D LoadCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter");

	private static readonly Texture2D SelectPreviousInGroupCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/SelectPreviousTransporter");

	private static readonly Texture2D SelectAllInGroupCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/SelectAllTransporters");

	private static readonly Texture2D SelectNextInGroupCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/SelectNextTransporter");

	private Texture2D HaulGizmoIcon;

	private Texture2D SelectAllInGroupCommandOverrideIcon;

	public new CompProperties_TransporterCustom Props => (CompProperties_TransporterCustom)props;

	public Pawn PawnOwner => parent as Pawn;

	public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
	{
		if (parent.BeingTransportedOnGravship || mode == DestroyMode.WillReplace || (PawnOwner != null && !PawnOwner.Dead))
		{
			return;
		}
		if (CancelLoad(map) && base.Shuttle == null)
		{
			if (!base.Groupable)
			{
				Messages.Message("MessageTransporterSingleLoadCanceled_TransporterDestroyed".Translate(), MessageTypeDefOf.NegativeEvent);
			}
			else
			{
				Messages.Message("MessageTransportersLoadCanceled_TransporterDestroyed".Translate(), MessageTypeDefOf.NegativeEvent);
			}
		}
		innerContainer.TryDropAll(parent.Position, map, ThingPlaceMode.Near);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (Props.haulGizmoTexPath != null && Find.Selector.SelectedObjects.Count == 1)
		{
			if ((object)HaulGizmoIcon == null)
			{
				HaulGizmoIcon = ContentFinder<Texture2D>.Get(Props.haulGizmoTexPath);
			}
			yield return new Command_Target
			{
				defaultLabel = "Ancot.HaulToTransporter".Translate(),
				defaultDesc = "Ancot.HaulToTransporterDesc".Translate(),
				icon = HaulGizmoIcon,
				targetingParams = new TargetingParameters
				{
					canTargetPawns = false,
					canTargetBuildings = false,
					canTargetItems = true,
					canTargetPlants = false,
					canTargetFires = false,
					canTargetMechs = false,
					mapObjectTargetsMustBeAutoAttackable = false,
					thingCategory = ThingCategory.Item
				},
				action = delegate(LocalTargetInfo target)
				{
					Thing thing2 = (Thing)target;
					if (thing2 != null)
					{
						Job job = new Job(AncotJobDefOf.Ancot_HaulToOwnTransporter, thing2, parent);
						if (parent is Pawn pawn)
						{
							pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						}
					}
				}
			};
		}
		int num = 0;
		foreach (object selectedObject in Find.Selector.SelectedObjects)
		{
			if (selectedObject is ThingWithComps thingWithComps && thingWithComps.HasComp<CompTransporter>())
			{
				num++;
			}
		}
		if (base.Shuttle != null && (!base.Shuttle.ShowLoadingGizmos || num > 1))
		{
			yield break;
		}
		if (innerContainer.Any)
		{
			TaggedString taggedString = (base.AnythingLeftToLoad ? "CommandCancelLoad".Translate() : "CommandUnload".Translate());
			TaggedString taggedString2 = (base.AnythingLeftToLoad ? "CommandCancelLoadDesc".Translate() : "CommandUnloadDesc".Translate(parent.LabelShort));
			Texture2D cancelLoadCommandTex = CompTransporter.CancelLoadCommandTex;
			yield return new Command_Action
			{
				defaultLabel = taggedString,
				defaultDesc = taggedString2,
				icon = cancelLoadCommandTex,
				action = delegate
				{
					SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
					CancelLoad();
				}
			};
		}
		if (base.LoadingInProgressOrReadyToLaunch)
		{
			if (base.Groupable)
			{
				yield return new Command_Action
				{
					defaultLabel = "Ancot.CommandSelectPreviousTransporter".Translate(),
					defaultDesc = "Ancot.CommandSelectPreviousTransporterDesc".Translate(),
					icon = SelectPreviousInGroupCommandTex,
					action = SelectPreviousInGroup
				};
				if (Props.selectAllInGroupCommandTexPath != null && (object)SelectAllInGroupCommandOverrideIcon == null)
				{
					SelectAllInGroupCommandOverrideIcon = ContentFinder<Texture2D>.Get(Props.selectAllInGroupCommandTexPath);
				}
				yield return new Command_Action
				{
					defaultLabel = "Ancot.CommandSelectAllTransporters".Translate(),
					defaultDesc = "Ancot.CommandSelectAllTransportersDesc".Translate(),
					icon = ((Props.selectAllInGroupCommandTexPath != null) ? SelectAllInGroupCommandOverrideIcon : SelectAllInGroupCommandTex),
					action = SelectAllInGroup
				};
				yield return new Command_Action
				{
					defaultLabel = "Ancot.CommandSelectNextTransporter".Translate(),
					defaultDesc = "Ancot.CommandSelectNextTransporterDesc".Translate(),
					icon = SelectNextInGroupCommandTex,
					action = SelectNextInGroup
				};
			}
			if (Props.canChangeAssignedThingsAfterStarting && (base.Shuttle == null || !base.Shuttle.Autoload))
			{
				yield return new Command_LoadToTransporter
				{
					defaultLabel = "Ancot.CommandLoadTransporterSingle".Translate(),
					defaultDesc = "Ancot.CommandLoadTransporterSingleDesc".Translate(),
					icon = LoadCommandTex,
					transComp = this
				};
			}
			yield break;
		}
		Command_LoadToTransporter command_LoadToTransporter2 = new Command_LoadToTransporter();
		if (!base.Groupable)
		{
			command_LoadToTransporter2.defaultLabel = "Ancot.CommandLoadTransporterSingle".Translate();
			command_LoadToTransporter2.defaultDesc = "Ancot.CommandLoadTransporterSingleDesc".Translate();
		}
		else
		{
			int num2 = 0;
			for (int i = 0; i < Find.Selector.NumSelected; i++)
			{
				Thing thing = Find.Selector.SelectedObjectsListForReading[i] as Thing;
				if (thing == null || thing.def != parent.def)
				{
					continue;
				}
				CompLaunchable compLaunchable = thing.TryGetComp<CompLaunchable>();
				if (compLaunchable != null)
				{
					CompRefuelable refuelable = compLaunchable.Refuelable;
					if (refuelable == null || !refuelable.HasFuel)
					{
						continue;
					}
				}
				num2++;
			}
			command_LoadToTransporter2.defaultLabel = "Ancot.CommandLoadTransporter".Translate(num2.ToString());
			command_LoadToTransporter2.defaultDesc = "Ancot.CommandLoadTransporterDesc".Translate();
		}
		command_LoadToTransporter2.icon = LoadCommandTex;
		command_LoadToTransporter2.transComp = this;
		CompLaunchable launchable = base.Launchable;
		if (launchable?.RequiresFuelingPort ?? false)
		{
			if (launchable is CompLaunchable_TransportPod obj && !obj.ConnectedToFuelingPort)
			{
				command_LoadToTransporter2.Disable("CommandLoadTransporterFailNotConnectedToFuelingPort".Translate());
			}
			else if (!launchable.Refuelable.HasFuel && base.Shuttle == null)
			{
				command_LoadToTransporter2.Disable("CommandLoadTransporterFailNoFuel".Translate());
			}
		}
		yield return command_LoadToTransporter2;
	}

	private void SelectPreviousInGroup()
	{
		List<CompTransporter> list = TransportersInGroup(base.Map);
		if (list != null)
		{
			int num = list.IndexOf(this);
			CameraJumper.TryJumpAndSelect(list[GenMath.PositiveMod(num - 1, list.Count)].parent);
		}
	}

	private void SelectAllInGroup()
	{
		List<CompTransporter> list = TransportersInGroup(base.Map);
		if (list != null)
		{
			Selector selector = Find.Selector;
			selector.ClearSelection();
			for (int i = 0; i < list.Count; i++)
			{
				selector.Select(list[i].parent);
			}
		}
	}

	private void SelectNextInGroup()
	{
		List<CompTransporter> list = TransportersInGroup(base.Map);
		if (list != null)
		{
			int num = list.IndexOf(this);
			CameraJumper.TryJumpAndSelect(list[(num + 1) % list.Count].parent);
		}
	}
}
