using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
	public class Building_BrokenTurret : Building
	{
		public float time;

		public bool shouldBeNoticed;

		public int TimeNow => 4000 * 60;

		public float WorkProgress => (float)time / (float)TimeNow;

		public static List<string> turretDefs => new List<string>
        {
			"PlayerTurret_ChargeSleetTurret",
			"PlayerTurret_RailgunTurret",
			"PlayerTurret_ArcTurret",
		};

		public void ChangeState(bool? force = null)
		{
			shouldBeNoticed = force ?? !shouldBeNoticed;
			if (shouldBeNoticed)
			{
				GDDefOf.GDDeckReinforce.PlayOneShotOnCamera();
				for (int i = 0; i < 8; i++)
				{
					FleckMaker.ThrowAirPuffUp(DrawPos, MapHeld);
				}
			}
		}

		public void WorkOn(Pawn pawn)
		{
			time += pawn.GetStatValue(StatDefOf.ConstructionSpeed, true, 300);
			if (WorkProgress >= 1f)
			{
				Finish();
			}
		}

		public void Finish()
        {
			QuestUtility.SendQuestTargetSignals(questTags, "Fixed", this.Named("SUBJECT"));
			GDDefOf.GDGearStart.PlayOneShot(this);
			GDDefOf.PocketThunderEffect.Spawn(Position, Map).Trigger(new TargetInfo(Position, Map), new TargetInfo(Position, Map));
			Map map = Map;
			IntVec3 pos = Position;
			Destroy();
			Building turret = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(turretDefs.RandomElement())) as Building;
			turret.SetFaction(Faction.OfPlayer);
			GenPlace.TryPlaceThing(turret, pos, map, ThingPlaceMode.Near);
		}

        public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string inspectString = base.GetInspectString();
			if (!inspectString.NullOrEmpty())
			{
				stringBuilder.AppendLine(inspectString);
			}
			stringBuilder.AppendLine("GD.WaitingForFix".Translate(WorkProgress.ToStringPercentEmptyZero()));
			return stringBuilder.ToString().TrimEndNewlines();
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (var gizmo in base.GetGizmos())
            {
				yield return gizmo;
            }
			Command_WhiteAction command = new Command_WhiteAction
			{
				action = delegate ()
				{
					ChangeState();
				},
				Order = 10001,
				defaultLabel = "GD.FixTurret".Translate(),
				defaultDesc = "GD.FixTurretDesc".Translate(TimeNow.ToStringTicksToDays()),
				icon = ContentFinder<Texture2D>.Get("UI/Buttons/BrokenTurretFix", false)
			};
			if (DebugSettings.ShowDevGizmos)
			{
				Command_Action command_Action = new Command_Action();
				command_Action.defaultLabel = "DEV: Finish Now";
				command_Action.action = delegate
				{
					time = TimeNow;
				};
				yield return command_Action;
			}
			yield return command;
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (shouldBeNoticed)
			{
				Vector3 drawPos = drawLoc;
				drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
				Matrix4x4 matrix = default(Matrix4x4);
				matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(1.0f, 1.0f, 1.0f));
				Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("UI/Buttons/BrokenTurretFix", ShaderDatabase.Transparent, new Color(1, 1, 1, 0.3f)), 0);
			}
		}

		public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref time, "time");
            Scribe_Values.Look(ref shouldBeNoticed, "shouldBeNoticed");
        }
    }
}
