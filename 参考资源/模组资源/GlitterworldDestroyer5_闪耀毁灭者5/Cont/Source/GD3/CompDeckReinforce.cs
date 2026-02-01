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
	public class CompProperties_DeckReinforce : CompProperties
	{
		public CompProperties_DeckReinforce()
		{
			this.compClass = typeof(CompDeckReinforce);
		}

		public int maxLevel = 3;

		public int baseCostPer100Health = 10;

		public int baseSecPer100Health = 5;

		public float extraCostMultiplierPerLevel = 0.5f;

		public float extraTimeMultiplierPerLevel = 0.2f;

		public float healthGainPerLevel = 0.25f;
	}

	public class CompDeckReinforce : ThingComp
    {
		public int level;

		public float time;

		public int decks;

		public bool shouldBeNoticed;

		public CompProperties_DeckReinforce Props
		{
			get
			{
				return (CompProperties_DeckReinforce)this.props;
			}
		}

		public bool CanUpgrade
        {
            get
            {
				if (level >= Props.maxLevel)
                {
					return false;
                }
				if (!(parent is Building_Turret))
                {
					return false;
                }
				if (parent.Faction != Faction.OfPlayer)
                {
					return false;
                }
				return true;
            }
        }

		public float Multiplier => (float)Math.Round(parent.MaxHitPoints / 100f, 1);

		public int CostNow => (int)(Props.baseCostPer100Health * (Multiplier + level * Props.extraCostMultiplierPerLevel));

		public int TimeNow => (int)(Props.baseSecPer100Health * (Multiplier + level * Props.extraTimeMultiplierPerLevel)) * 60;

		public float WorkProgress => (float)time / (float)TimeNow;

		public bool IsFull => decks >= CostNow;

		public int CountToFullyRefuel => CostNow - decks;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
		}

        public void ChangeState(bool? force = null)
        {
			shouldBeNoticed = force ?? !shouldBeNoticed;
			if (shouldBeNoticed)
            {
				GDDefOf.GDDeckReinforce.PlayOneShotOnCamera();
				for (int i = 0; i < 8; i++)
				{
					FleckMaker.ThrowAirPuffUp(parent.DrawPos, parent.MapHeld);
				}
			}
            else if (decks > 0)
            {
				Thing deck = ThingMaker.MakeThing(GDDefOf.PrecisionDeck);
				deck.stackCount = decks;
				GenPlace.TryPlaceThing(deck, parent.PositionHeld, parent.MapHeld, ThingPlaceMode.Near);
				decks = 0;
				time = 0;
			}
		}

		public void Refuel(List<Thing> fuelThings)
		{
			int num = CountToFullyRefuel;
			while (num > 0 && fuelThings.Count > 0)
			{
				Thing thing = fuelThings.Pop();
				int num2 = Mathf.Min(num, thing.stackCount);
				Refuel(num2);
				thing.SplitOff(num2).Destroy();
				num -= num2;
			}
		}

		public void Refuel(int amount)
		{
			decks += amount;
			if (decks > CostNow)
			{
				decks = CostNow;
			}
		}

		public void WorkOn(Pawn pawn)
        {
			time += pawn.GetStatValue(StatDefOf.ConstructionSpeed, true, 300);
			if (WorkProgress >= 1f)
            {
				Upgrade();
			}
        }

		public void Upgrade()
        {
			level++;
			time = 0;
			decks = 0;
			shouldBeNoticed = false;
			for (int i = 0; i < 8; i++)
			{
				FleckMaker.ThrowAirPuffUp(parent.DrawPos, parent.MapHeld);
			}
			parent.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 10));
			parent.HitPoints = parent.MaxHitPoints;
		}

        public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string inspectString = base.CompInspectStringExtra();
			if (!inspectString.NullOrEmpty())
			{
				stringBuilder.AppendLine(inspectString);
			}
			if (level > 0)
            {
				stringBuilder.AppendLine("GD.TurretCurrentLevel".Translate(level));
			}
			if (decks > 0)
            {
				if (!IsFull)
                {
					stringBuilder.AppendLine("GD.NeedMoreDecks".Translate(CountToFullyRefuel));
				}
                else
                {
					stringBuilder.AppendLine("GD.WaitingForUpgrade".Translate(WorkProgress.ToStringPercentEmptyZero()));
				}
            }
			return stringBuilder.ToString().TrimEndNewlines();
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
			if (decks > 0)
			{
				Thing deck = ThingMaker.MakeThing(GDDefOf.PrecisionDeck);
				deck.stackCount = decks;
				GenPlace.TryPlaceThing(deck, parent.PositionHeld, previousMap, ThingPlaceMode.Near);
				decks = 0;
				time = 0;
			}
		}

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
			if (!CanUpgrade)
			{
				yield break;
			}
			Command_WhiteAction command = new Command_WhiteAction
			{
				action = delegate ()
				{
					ChangeState();
				},
				Order = 10001,
				defaultLabel = "GD.DeckReinforce".Translate(),
				defaultDesc = "GD.DeckReinforceDesc".Translate(CostNow, TimeNow.ToStringSecondsFromTicks()),
				icon = ContentFinder<Texture2D>.Get("UI/Buttons/PrecisionDeckUpgrade", false)
			};
			yield return command;
		}

        public override void PostDraw()
        {
			if (shouldBeNoticed)
            {
				Vector3 drawPos = parent.TrueCenter();
				drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
				Matrix4x4 matrix = default(Matrix4x4);
				matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(1.0f, 1.0f, 1.0f));
				Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("UI/Buttons/PrecisionDeckUpgrade", ShaderDatabase.Transparent, new Color(1, 1, 1, 0.3f)), 0);
			}
		}

        public override void PostExposeData()
        {
            base.PostExposeData();
			Scribe_Values.Look(ref level, "level");
			Scribe_Values.Look(ref time, "time");
			Scribe_Values.Look(ref decks, "decks");
			Scribe_Values.Look(ref shouldBeNoticed, "shouldBeNoticed");
		}
	}
}
