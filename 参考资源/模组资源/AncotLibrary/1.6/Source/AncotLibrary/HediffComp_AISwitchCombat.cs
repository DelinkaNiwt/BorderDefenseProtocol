using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public abstract class HediffComp_AISwitchCombat : HediffComp
{
	public bool switchOn;

	private Texture2D GizmoIcon;

	private CompMechAutoFight compAutoFight;

	protected virtual string GizmoLabel => "";

	protected virtual string GizmoDesc => "";

	protected abstract string IconPath { get; }

	protected virtual float AICanSwitchOnSeverity => 0.5f;

	protected virtual int IntervalTicks => 180;

	private CompMechAutoFight CompAutoFight => compAutoFight ?? (compAutoFight = base.Pawn.TryGetComp<CompMechAutoFight>());

	protected virtual bool AIConditionalSwitchOn
	{
		get
		{
			if (base.Pawn.mindState.enemyTarget != null)
			{
				if (base.Pawn.IsColonyMech)
				{
					CompMechAutoFight compMechAutoFight = CompAutoFight;
					if (compMechAutoFight == null || !compMechAutoFight.AutoFight)
					{
						goto IL_0053;
					}
				}
				return parent.Severity > AICanSwitchOnSeverity;
			}
			goto IL_0053;
			IL_0053:
			return false;
		}
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		if (base.Pawn.IsHashIntervalTick(IntervalTicks) && !base.Pawn.Drafted)
		{
			if (AIConditionalSwitchOn && !switchOn)
			{
				switchOn = true;
			}
			else
			{
				switchOn = false;
			}
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmos()
	{
		if (base.Pawn.Drafted)
		{
			if (IconPath != null && (object)GizmoIcon == null)
			{
				GizmoIcon = ContentFinder<Texture2D>.Get(IconPath);
			}
			yield return new Command_Toggle
			{
				defaultLabel = GizmoLabel.Translate(),
				defaultDesc = GizmoDesc.Translate(),
				icon = ((IconPath != null) ? GizmoIcon : AncotLibraryIcon.SwitchA),
				toggleAction = delegate
				{
					switchOn = !switchOn;
				},
				isActive = () => switchOn
			};
		}
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_Values.Look(ref switchOn, "switchOn", defaultValue: false);
	}
}
