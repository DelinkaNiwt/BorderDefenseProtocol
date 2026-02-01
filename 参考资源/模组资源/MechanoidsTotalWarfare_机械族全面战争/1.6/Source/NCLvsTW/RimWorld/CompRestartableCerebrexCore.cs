using System;
using System.Collections.Generic;
using System.Reflection;
using NCL;
using UnityEngine;
using Verse;

namespace RimWorld;

public class CompRestartableCerebrexCore : ThingComp
{
	private Graphic brainGraphic;

	private int restartCountdown = -1;

	private bool isRestarting;

	private bool restartCompleted = false;

	private bool spawnMailSent = false;

	private CompProperties_RestartableCerebrexCore Props => (CompProperties_RestartableCerebrexCore)props;

	private Graphic BrainGraphic
	{
		get
		{
			if (brainGraphic == null)
			{
				brainGraphic = GraphicDatabase.Get<Graphic_Multi>("Buildings/MechCoreTop", ShaderDatabase.Cutout, new Vector3(7f, 7f, 7f), Color.white);
			}
			return brainGraphic;
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!respawningAfterLoad && !spawnMailSent)
		{
			SendSpawnMail();
			spawnMailSent = true;
		}
	}

	private void SendSpawnMail()
	{
		Find.LetterStack.ReceiveLetter("CerebrexCore_SpawnTitle".Translate(), "CerebrexCore_SpawnDesc".Translate(), LetterDefOf.NeutralEvent, parent);
	}

	public override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		float z = Props.zOffset + 0.5f * (1f + Mathf.Sin((float)Math.PI * 2f * (float)GenTicks.TicksGame / 300f)) * Props.bobAmplitude;
		Matrix4x4 matrix = Matrix4x4.TRS(drawLoc + new Vector3(0f, Props.yOffset, z), Quaternion.AngleAxis(0f, Vector3.up), Vector3.one);
		GenDraw.DrawMeshNowOrLater(BrainGraphic.MeshAt(Rot4.South), matrix, BrainGraphic.MatSouth, drawNow: false);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (!isRestarting && !restartCompleted && parent.Faction == Faction.OfPlayer)
		{
			yield return new Command_Action
			{
				icon = ContentFinder<Texture2D>.Get("ModIcon/RestartCore"),
				defaultLabel = "RestartCore".Translate(),
				defaultDesc = "RestartCoreDECS".Translate(),
				activateSound = SoundDefOf.Tick_Low,
				action = StartCoreRestart
			};
		}
		if (isRestarting && !restartCompleted)
		{
			yield return new Command_Action
			{
				icon = ContentFinder<Texture2D>.Get("ModIcon/RestartCore"),
				defaultLabel = "CancelRestart".Translate(),
				defaultDesc = "CancelRestartDESC".Translate(),
				activateSound = SoundDefOf.Click,
				action = CancelRestart
			};
		}
	}

	private void StartCoreRestart()
	{
		isRestarting = true;
		restartCountdown = Props.restartDurationTicks;
		Find.TickManager.slower.SignalForceNormalSpeed();
		Find.MusicManagerPlay.ForceFadeoutAndSilenceFor(999f, 3f);
	}

	private void CancelRestart()
	{
		isRestarting = false;
		restartCountdown = -1;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (restartCountdown > 0)
		{
			restartCountdown--;
			if (restartCountdown == 0)
			{
				CompleteRestart();
			}
		}
	}

	private void CompleteRestart()
	{
		restartCompleted = true;
		if (Faction.OfMechanoids != null)
		{
			typeof(Faction).GetField("deactivated", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(Faction.OfMechanoids, false);
		}
		CameraJumper.TryJump(parent, CameraJumper.MovementMode.Cut);
		Messages.Message("CerebrexCoreRestored".Translate(), MessageTypeDefOf.NeutralEvent);
		ActivateNCLTotalWarfare();
		isRestarting = false;
	}

	private void ActivateNCLTotalWarfare()
	{
		TipComponent tipComp = Find.World.GetComponent<TipComponent>();
		if (tipComp != null)
		{
			tipComp.TWtriggered3 = true;
			tipComp.lastActivationTick = Find.TickManager.TicksGame;
			Find.LetterStack.ReceiveLetter("NCL_COREBASED_ENHANCEMENT_TITLE".Translate(), "NCL_COREBASED_ENHANCEMENT_DESC".Translate(), LetterDefOf.NeutralEvent);
			if (TotalWarfareSettings.ShowDevLogs)
			{
				Log.Message("[NCL] Total Warfare system activated by Cerebrex Core");
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref restartCountdown, "restartCountdown", -1);
		Scribe_Values.Look(ref isRestarting, "isRestarting", defaultValue: false);
		Scribe_Values.Look(ref restartCompleted, "restartCompleted", defaultValue: false);
		Scribe_Values.Look(ref spawnMailSent, "spawnMailSent", defaultValue: false);
	}
}
