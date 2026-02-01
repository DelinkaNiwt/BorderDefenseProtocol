using System;
using RimWorld;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using UnityEngine;
using System.Linq;

namespace GD3
{
	public class CompProperties_SpawnMech : CompProperties
	{
		public CompProperties_SpawnMech()
		{
			this.compClass = typeof(CompSpawnMech);
		}

		public SoundDef soundWhenImpact;

		public PawnKindDef mechanoid;
	}

	public class CompSpawnMech : ThingComp
	{
		public CompProperties_SpawnMech Props
		{
			get
			{
				return (CompProperties_SpawnMech)this.props;
			}
		}

		public Projectile Bullet
        {
            get
            {
				return (Projectile)this.parent;
            }
        }

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			this.map = this.parent.Map;
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			Faction f = null;
			if (this.Bullet.Launcher != null && this.Bullet.Launcher.Faction != null)
            {
				f = this.Bullet.Launcher.Faction;
            }
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.Props.mechanoid, f, PawnGenerationContext.NonPlayer, -1, true, false, false, false, true, 1f, true, true, true, false, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, 0, 0, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false));
			GenPlace.TryPlaceThing(pawn, this.parent.Position, this.map, ThingPlaceMode.Near, null, null, default(Rot4));
			Pawn p;
			Lord lord = ((p = (this.Bullet.Launcher as Pawn)) != null) ? p.GetLord() : null;
			if (lord != null)
			{
				lord.AddPawn(pawn);
			}
			this.Props.soundWhenImpact.PlayOneShot(new TargetInfo(this.Bullet.PositionHeld, this.Bullet.MapHeld, false));
			base.PostDestroy(mode, previousMap);
		}

		private Map map;
	}
}
