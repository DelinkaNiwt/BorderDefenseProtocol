using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
	public class CompUseEffect_HackMortar : CompUseEffect
	{
		private Map map;

		private Faction faction;

        public override void CompTick()
        {
            base.CompTick();
			map = this.parent.Map;
			faction = this.parent.Faction;
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
		{
			AcceptanceReport result = base.CanBeUsedBy(p);
			if (p.skills == null || p.skills.GetSkill(SkillDefOf.Intellectual).PermanentlyDisabled || p.skills.GetSkill(SkillDefOf.Intellectual).Level < 10)
			{
				return "GD.LevelNotEnoughToAnalyse".Translate();
			}
			Building building = this.parent as Building;
			if (building.Faction != null && building.Faction == Faction.OfPlayer)
			{
				return "GD.MortarHacked".Translate();
			}
			return result;
		}

		public override void DoEffect(Pawn user)
		{
			base.DoEffect(user);
			Building building = this.parent as Building;
			FleckMaker.Static(building.TrueCenter().ToIntVec3(), building.Map, FleckDefOf.BroadshieldActivation, 1.5f);
			GDDefOf.GD_Controled.PlayOneShot(building);
			SoundDefOf.ControlMech_Complete.PlayOneShot(building);
			building.SetFaction(Faction.OfPlayer);
		}

        public override void PostDraw()
        {
            base.PostDraw();
			Building building = this.parent as Building;
			if (building.Faction == null)
            {
				return;
            }
			Color color = building.Faction == Faction.OfPlayer ? Color.blue : Color.red;
			Vector3 drawPos = building.DrawPos;
			drawPos.y = AltitudeLayer.Building.AltitudeFor();
			drawPos += building.def.graphicData.drawOffset;
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(6.5f, 1f, 6.5f));
			Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom(graphicPath, ShaderDatabase.Transparent, color), 0);
		}

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
			if (faction != null && faction == Faction.OfPlayer)
            {
				Messages.Message("GD.MortarLost".Translate(), MessageTypeDefOf.NegativeEvent);
            }

			List<Thing> buildings = map.listerThings.AllThings.FindAll(t => t is Building_TurretGun);
			if (buildings.Count == 0)
            {
				return;
            }
			if (!buildings.Any(b => b.def.defName == "Turret_GiantAutoMortar_Script"))
            {
				Messages.Message("GD.MortarAllLost".Translate(), MessageTypeDefOf.PawnDeath);
			}
		}

        public override void PostExposeData()
        {
            base.PostExposeData();
			Scribe_References.Look(ref map, "map");
			Scribe_References.Look(ref faction, "faction");
        }

        private readonly string graphicPath = "Buildings/GiantMechTurret/GiantBase_Color";
	}
}
