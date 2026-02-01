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
    public class CompProperties_AbilityAnnihilatorLongJump : CompProperties_AbilityEffect
    {
        public int range = 50;
        
        public CompProperties_AbilityAnnihilatorLongJump()
        {
            compClass = typeof(CompAbilityEffect_AnnihilatorLongJump);
        }
    }

    public class CompAbilityEffect_AnnihilatorLongJump : CompAbilityEffect
	{
        public new CompProperties_AbilityAnnihilatorLongJump Props => (CompProperties_AbilityAnnihilatorLongJump)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			Annihilator pawn = parent.pawn as Annihilator;
			if (pawn != null)
			{
                StartChoosingDestination();
			}
		}

		public override void Apply(GlobalTargetInfo target)
		{
			this.Apply(null, null);
		}

		public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
		{
			return true;
		}

		public override bool CanApplyOn(GlobalTargetInfo target)
		{
			return this.CanApplyOn(null, null);
		}

		public override bool GizmoDisabled(out string reason)
		{
			reason = null;
			Annihilator pawn = parent.pawn as Annihilator;
			if (pawn.animation != GDDefOf.Annihilator_Ambient || pawn.Flying)
			{
				reason = "GD.AnnihilatorBusy".Translate(pawn.LabelShort);
				return true;
			}
            if (pawn.MapHeld == null || !pawn.MapHeld.Tile.Valid)
            {
                reason = "GD.AnnihilatorTileInvalid".Translate(pawn.LabelShort);
                return true;
            }
            if (pawn.Dying)
			{
				return true;
			}
			return false;
		}

        public void StartChoosingDestination()
        {
            PlanetTile? tile = parent.pawn?.MapHeld?.Tile;
            if (tile == null)
            {
                return;
            }
            int num = Props.range;
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(new GlobalTargetInfo(tile.Value)));
            Find.WorldSelector.ClearSelection();
            Find.WorldTargeter.BeginTargeting((GlobalTargetInfo t) => ChoseWorldTarget(t), canTargetTiles: false, Dialog_Artillery.TargeterMouseAttachment, false, delegate
            {
                PlanetTile planetTile;
                if (cachedLayer != Find.WorldSelector.SelectedLayer || cachedOrigin != tile)
                {
                    cachedLayer = Find.WorldSelector.SelectedLayer;
                    cachedOrigin = tile.Value;
                    planetTile = (cachedClosest = Find.WorldSelector.SelectedLayer.GetClosestTile_NewTemp(tile.Value));
                }
                else
                {
                    planetTile = cachedClosest;
                }
                GenDraw.DrawWorldRadiusRing(planetTile, num, CompPilotConsole.GetThrusterRadiusMat(planetTile));
            }, (GlobalTargetInfo target) => TargetingLabelGetter(target, tile.Value, num), (GlobalTargetInfo target) => CanSelect(target, tile.Value, num), tile, showCancelButton: true);
        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            cachedClosest = (cachedOrigin = PlanetTile.Invalid);
            cachedLayer = null;
            if (!target.HasWorldObject)
            {
                return false;
            }
            Map map = (target.WorldObject as MapParent)?.Map;
            if (map == null)
            {
                return false;
            }
            Current.Game.CurrentMap = map;
            CameraJumper.TryHideWorld();
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), delegate (LocalTargetInfo x)
            {
                ((Annihilator)parent.pawn).LongJumpTo(new TargetInfo(x.Cell, map));
            }, delegate (LocalTargetInfo y)
            {
                if (y.IsValid && y.Cell.InBounds(map))
                {
                    List<IntVec3> cells = map.AllCells.Where(c => GenAdj.IsInside(c, y.Cell, Rot4.East, new IntVec2(9, 9))).ToList();
                    if (cells.Any())
                    {
                        GenDraw.DrawFieldEdges(cells);
                    }
                    GenDraw.DrawTargetHighlight(y);
                }
            }, null, null, null, Dialog_Artillery.TargeterMouseAttachment, true, OnGUI);
            return true;
        }

        public TaggedString TargetingLabelGetter(GlobalTargetInfo target, PlanetTile tile, int maxLaunchDistance)
        {
            if (!target.IsValid)
            {
                return null;
            }
            if (target.Tile == tile)
            {
                GUI.color = ColorLibrary.RedReadable;
                return "GD.MeaningLessTile".Translate();
            }
            if (target.Tile.Layer != tile.Layer)
            {
                GUI.color = ColorLibrary.RedReadable;
                return "GD.BeyondReach".Translate();
            }
            int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile, passImpassable: true, int.MaxValue, canTraverseLayers: false);
            if (maxLaunchDistance > 0 && num > maxLaunchDistance)
            {
                GUI.color = ColorLibrary.RedReadable;
                return "GD.BeyondReach".Translate();
            }
            if (target.WorldObject is MapParent mapParent && mapParent.HasMap)
            {
                return "ClickToSeeAvailableOrders_WorldObject".Translate(mapParent.LabelCap);
            }
            return "ClickToSeeAvailableOrders_Empty".Translate();
        }

        public bool CanSelect(GlobalTargetInfo target, PlanetTile tile, int maxLaunchDistance)
        {
            if (target.Tile == tile)
            {
                return false;
            }
            return true;
        }

        public void OnGUI(LocalTargetInfo target)
        {
            string label = "GD.AnnihilatorReadyToJump".Translate();
            Widgets.MouseAttachedLabel(label);
        }

        private PlanetTile cachedClosest;
        private PlanetTile cachedOrigin;
        private PlanetLayer cachedLayer;
    }
}