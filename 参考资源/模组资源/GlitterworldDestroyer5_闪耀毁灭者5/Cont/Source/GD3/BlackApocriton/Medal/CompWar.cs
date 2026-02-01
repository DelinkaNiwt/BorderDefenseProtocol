using System;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using UnityEngine;

namespace GD3
{
	public class CompWar : ThingComp
	{
		public CompProperties_War Props
		{
			get
			{
				return this.props as CompProperties_War;
			}
		}

		public Apparel Medal
		{
			get
			{
				return this.parent as Apparel;
			}
		}

		private bool CanAttack
		{
			get
			{
				return this.readyToUseTicks <= 0;
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			if (!this.CanAttack)
            {
				this.readyToUseTicks--;
            }
		}

		public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
			{
				yield return gizmo;
			}
			bool flag = this.Medal.Wearer.Faction == Faction.OfPlayer;
			if (flag)
			{
				Command_Action bombardBig = new Command_Action
				{
					defaultLabel = this.Props.toggleLabelKey.Translate(),
					defaultDesc = this.Props.toggleDescKey.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					action = delegate ()
					{
						Find.Targeter.BeginTargeting(this.ConnectCorpseTargetParameters(), new Action<LocalTargetInfo>(this.BombardBig), null, new Func<LocalTargetInfo, bool>(this.CanAffect), null, null, null, true, null);
					}
				};
				Command_Action bombardSmall = new Command_Action
				{
					defaultLabel = this.Props.toggleLabelKey2.Translate(),
					defaultDesc = this.Props.toggleDescKey2.Translate(),
					icon = ContentFinder<Texture2D>.Get(this.Props.toggleIconPath, true),
					action = delegate ()
					{
						Find.Targeter.BeginTargeting(this.ConnectCorpseTargetParameters(), new Action<LocalTargetInfo>(this.BombardSmall), null, new Func<LocalTargetInfo, bool>(this.CanAffect), null, null, null, true, null);
					}
				};
				if (!this.CanAttack)
                {
					bombardBig.Disable("GD.WarCooling".Translate(Mathf.Floor(this.readyToUseTicks / 60)));
					bombardSmall.Disable("GD.WarCooling".Translate(Mathf.Floor(this.readyToUseTicks / 60)));
				}
				yield return bombardBig;
				yield return bombardSmall;
			}
			yield break;
		}

		public TargetingParameters ConnectCorpseTargetParameters()
		{
			return new TargetingParameters
			{
				canTargetPawns = false,
				canTargetBuildings = false,
				canTargetHumans = false,
				canTargetMechs = false,
				canTargetAnimals = false,
				canTargetLocations = true,
				validator = ((TargetInfo x) => this.CanAffect((LocalTargetInfo)x))
			};
		}

		public bool CanAffect(LocalTargetInfo target)
		{
			IntVec3 vec3 = target.Cell;
			return vec3.IsValid;
		}

		private void BombardBig(LocalTargetInfo target)
        {
			this.readyToUseTicks = 600;
			IntVec3 vec3 = target.Cell;
			GDDefOf.PocketThunderEffect.Spawn(vec3, this.Medal.Wearer.Map, 1f).EffectTick(new TargetInfo(vec3, this.Medal.Wearer.Map, false), null);
			GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.GD_DummyBombardment, null), vec3, this.Medal.Wearer.Map, ThingPlaceMode.Near, null, null, default(Rot4));
		}

		private void BombardSmall(LocalTargetInfo target)
		{
			this.readyToUseTicks = 120;
			IntVec3 vec3 = target.Cell;
			GDDefOf.PocketThunderEffect.Spawn(vec3, this.Medal.Wearer.Map, 1f).EffectTick(new TargetInfo(vec3, this.Medal.Wearer.Map, false), null);
			GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.GD_DummyMine, null), vec3, this.Medal.Wearer.Map, ThingPlaceMode.Near, null, null, default(Rot4));
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.readyToUseTicks, "readyToUseTicks", 0, false);
		}

		private int readyToUseTicks;

		public LocalTargetInfo curTarget = LocalTargetInfo.Invalid;
	}
}