using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GD3
{
    [StaticConstructorOnStartup]
    public class Dialog_Artillery : Window
    {
        public Building_CommsConsole comms;

        private string title;
        private string option1Label;
        private string option2Label;
        private string option3Label;
        private string option1Text;
        private string option2Text;
        private string option3Text;
        private string actText;

        private Texture2D logo;
        private Texture2D penetration;
        private Texture2D inferno;
        private Texture2D emp;
        private float imageSize = 320f;
        private float buttonSize = 130f;

        private int selected = -1;

        private PlanetTile cachedClosest;
        private PlanetTile cachedOrigin;
        private PlanetLayer cachedLayer;
        public static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/Commands/Attack");

        public override Vector2 InitialSize => new Vector2(460, 350);

        public Dialog_Artillery(Building_CommsConsole comms)
        {
            this.comms = comms;
            this.title = "GD.ArtillerySystem".Translate();
            this.option1Label = "GD.ArtilleryPenetration".Translate();
            this.option2Label = "GD.ArtilleryInferno".Translate();
            this.option3Label = "GD.ArtilleryEMP".Translate();
            this.option1Text = "GD.ArtilleryPenetration.Desc".Translate(); ;
            this.option2Text = "GD.ArtilleryInferno.Desc".Translate(); ;
            this.option3Text = "GD.ArtilleryEMP.Desc".Translate();
            this.actText = "GD.ArtillerySelectTarget".Translate();
            this.doCloseButton = false;
            this.doCloseX = true;
            this.closeOnCancel = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.preventCameraMotion = false;
            this.doWindowBackground = true;
            logo = ContentFinder<Texture2D>.Get("UI/Dialog/ArtillerySystemLogo", reportFailure: false);
            penetration = ContentFinder<Texture2D>.Get("UI/Dialog/ArtilleryPeneration", reportFailure: false);
            inferno = ContentFinder<Texture2D>.Get("UI/Dialog/ArtilleryInferno", reportFailure: false);
            emp = ContentFinder<Texture2D>.Get("UI/Dialog/ArtilleryEMP", reportFailure: false);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect mainRect = inRect.ContractedBy(10f);
            Rect imageRect = new Rect(mainRect.x + (mainRect.width - imageSize) / 2f, mainRect.y + (mainRect.height - imageSize) / 2f, imageSize, imageSize);
            Widgets.DrawTextureFitted(imageRect, logo, 1.5f, MaterialPool.MatFrom("UI/Dialog/ArtillerySystemLogo", ShaderDatabase.TransparentPostLight, new Color(1, 1, 1, 0.2f)));

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            float titleHeight = Text.CalcHeight(title, mainRect.width);
            Widgets.Label(new Rect(mainRect.x, mainRect.y, mainRect.width, titleHeight), title);
            Text.Anchor = TextAnchor.UpperLeft;

            float optionY = mainRect.y + titleHeight + 10f;
            Rect option1Rect = new Rect(mainRect.x, optionY, buttonSize, buttonSize);
            Rect option2Rect = new Rect(mainRect.x + buttonSize + 10f, optionY, buttonSize, buttonSize);
            Rect option3Rect = new Rect(mainRect.x + (buttonSize + 10f) * 2, optionY, buttonSize, buttonSize);
            Widgets.DrawHighlightIfMouseover(option1Rect);
            Widgets.DrawHighlightIfMouseover(option2Rect);
            Widgets.DrawHighlightIfMouseover(option3Rect);
            Rect[] optionRects = { option1Rect, option2Rect, option3Rect };
            TaggedString[] optionTexts = { option1Text, option2Text, option3Text };

            if (selected != -1)
            {
                Text.Anchor = TextAnchor.UpperCenter;
                Rect selectedRect = optionRects[selected];
                Widgets.DrawHighlightSelected(selectedRect);
                float textHeight = Text.CalcHeight(optionTexts[selected], mainRect.width);
                Rect textRect = new Rect(mainRect.x, optionY + buttonSize + 10f, mainRect.width, textHeight);
                Widgets.Label(textRect, optionTexts[selected]);
                Text.Anchor = TextAnchor.UpperLeft;
            }

            if (Widgets.ButtonImage(option1Rect, penetration, new Color(1f, 0.76f, 0.15f), true, option1Label))
            {
                SoundDefOf.RowTabSelect.PlayOneShotOnCamera();
                selected = selected == 0 ? -1 : 0;
                if (selected == 0)
                {
                    GDUtility.MainComponent.artilleryStrikeNumber = 0;
                }
                else GDUtility.MainComponent.artilleryStrikeNumber = -1;
            }

            if (Widgets.ButtonImage(option2Rect, inferno, new Color(0.70f, 0.13f, 0.13f), true, option2Label))
            {
                SoundDefOf.RowTabSelect.PlayOneShotOnCamera();
                selected = selected == 1 ? -1 : 1;
                if (selected == 1)
                {
                    GDUtility.MainComponent.artilleryStrikeNumber = 1;
                }
                else GDUtility.MainComponent.artilleryStrikeNumber = -1;
            }

            if (Widgets.ButtonImage(option3Rect, emp, new Color(0.12f, 0.56f, 1f), true, option3Label))
            {
                SoundDefOf.RowTabSelect.PlayOneShotOnCamera();
                selected = selected == 2 ? -1 : 2;
                if (selected == 2)
                {
                    GDUtility.MainComponent.artilleryStrikeNumber = 2;
                }
                else GDUtility.MainComponent.artilleryStrikeNumber = -1;
            }

            if (selected != -1)
            {
                Text.Anchor = TextAnchor.UpperCenter;
                float textHeight = Text.CalcHeight(actText, mainRect.width) * 2;
                Rect actRect = new Rect(mainRect.x, mainRect.y + mainRect.height - textHeight, mainRect.width, textHeight);
                if (Widgets.ButtonText(actRect, actText))
                {
                    StartChoosingDestination();
                    this.Close();
                }
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        public void StartChoosingDestination()
        {
            GDUtility.MainComponent.ClusterAssistanceAvailable(comms.Map, out WorldObject artillery);
            PlanetTile tile = artillery.Tile;
            int num = 39;
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(new GlobalTargetInfo(tile)));
            Find.WorldSelector.ClearSelection();
            Find.WorldTargeter.BeginTargeting((GlobalTargetInfo t) => ChoseWorldTarget(t), canTargetTiles: false, TargeterMouseAttachment, false, delegate
            {
                PlanetTile planetTile;
                if (cachedLayer != Find.WorldSelector.SelectedLayer || cachedOrigin != tile)
                {
                    cachedLayer = Find.WorldSelector.SelectedLayer;
                    cachedOrigin = tile;
                    planetTile = (cachedClosest = Find.WorldSelector.SelectedLayer.GetClosestTile_NewTemp(tile));
                }
                else
                {
                    planetTile = cachedClosest;
                }
                GenDraw.DrawWorldRadiusRing(planetTile, num, CompPilotConsole.GetThrusterRadiusMat(planetTile));
            }, (GlobalTargetInfo target) => TargetingLabelGetter(target, tile, num), null, tile, showCancelButton: true);
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
                int number = GDUtility.MainComponent.artilleryStrikeNumber;
                if (number == 0)
                {
                    Thing strike = ThingMaker.MakeThing(GDDefOf.GD_PenetrationArtilleryStrike);
                    GenPlace.TryPlaceThing(strike, x.Cell, map, ThingPlaceMode.Direct);
                    GDUtility.MainComponent.artilleryStrikeCooldown = Find.TickManager.TicksGame + 5 * 60000;
                }
                else if (number == 1)
                {
                    Thing strike = ThingMaker.MakeThing(GDDefOf.GD_InfernoArtilleryStrike);
                    GenPlace.TryPlaceThing(strike, x.Cell, map, ThingPlaceMode.Direct);
                    GDUtility.MainComponent.artilleryStrikeCooldown = Find.TickManager.TicksGame + 25 * 60000;
                }
                else if (number == 2)
                {
                    Thing strike = ThingMaker.MakeThing(GDDefOf.GD_EMPArtilleryStrike);
                    GenPlace.TryPlaceThing(strike, x.Cell, map, ThingPlaceMode.Direct);
                    GDUtility.MainComponent.artilleryStrikeCooldown = Find.TickManager.TicksGame + 1 * 60000;
                }
                GDUtility.MainComponent.artilleryStrikeNumber = -1;
            }, null, null, null, null, TargeterMouseAttachment, true, OnGUI);
            return true;
        }

        public TaggedString TargetingLabelGetter(GlobalTargetInfo target, PlanetTile tile, int maxLaunchDistance)
        {
            if (!target.IsValid)
            {
                return null;
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

        public void OnGUI(LocalTargetInfo target)
        {
            string label = "GD.ClusterReadyToAttack".Translate();
            Widgets.MouseAttachedLabel(label);
        }
    }
}
