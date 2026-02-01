using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using Steamworks;
using Verse.Steam;

namespace GD3
{
    public class GameComponent_ExtraDrawer : GameComponent
    {

        public GameComponent_ExtraDrawer(Game game)
        {
        }

        public int drawTick = 0;

        public int whiteTick = 0;

        public int sustainerTick = 0;

        public float dialogTick = -1;

        public bool preventInteraction;

        public FinalBattleDummy pointer;

        private Dictionary<IntRange, string> tmpStrings;

        private List<IntRange> tmpInt;

        private List<string> tmpString;

        public Sustainer sustainer;

        private string titleInt = "GD.BlackApocriton";

        public TaggedString DialogTitle
        {
            get
            {
                return titleInt.Translate();
            }
            set
            {
                titleInt = value;
            }
        }

        private static SimpleCurve whiteAlphaCurve = new SimpleCurve
        {
            new CurvePoint(0, 0),
            new CurvePoint(1, 0.7f),
        };

        private static SimpleCurve alphaCurve = new SimpleCurve
        {
            new CurvePoint(0, 0),
            new CurvePoint(0.15f, 0.7f),
            new CurvePoint(0.85f, 0.7f),
            new CurvePoint(1, 0),
        };

        private static SimpleCurve fontCurve = new SimpleCurve
        {
            new CurvePoint(0, 0),
            new CurvePoint(0.15f, 1f),
            new CurvePoint(0.85f, 1f),
            new CurvePoint(1, 0),
        };

        public MissionComponent MissionComponent => GDUtility.MissionComponent;

        public void StartDraw(int tick)
        {
            drawTick = tick;
            sustainerTick = tick;
        }

        public void StartWhiteOverlay(int tick)
        {
            whiteTick = tick;
        }

        public void StartDialog(Dictionary<IntRange, string> strings, bool preventInteraction = false)
        {
            ResetDialog();
            tmpStrings = strings;
            dialogTick = 0;
            this.preventInteraction = preventInteraction;
        }

        public void ResetDialog()
        {
            if (tmpString == null) tmpStrings = new Dictionary<IntRange, string>();
            tmpStrings.Clear();
            dialogTick = -1;
            preventInteraction = false;
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (drawTick > 0)
            {
                drawTick--;
            }
            if (whiteTick > 0)
            {
                whiteTick--;
            }
            if (dialogTick >= 0)
            {
                dialogTick += 1f;
                if (tmpStrings.NullOrEmpty() || tmpStrings.Last().Key.TrueMax < dialogTick)
                {
                    ResetDialog();
                }
            }
            if (sustainerTick > 0)
            {
                sustainerTick--;
                if (sustainer == null || sustainer.Ended)
                {
                    sustainer = GDDefOf.SnowScreenTriggered.TrySpawnSustainer(SoundInfo.OnCamera(MaintenanceType.PerTick));
                }
                sustainer?.Maintain();
                if (sustainerTick == 0)
                {
                    sustainer.End();
                    sustainer = null;
                }
            }
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();

            #region 白屏遮罩
            if (whiteTick > 0)
            {
                float alpha = whiteAlphaCurve.Evaluate(whiteTick / 60f);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), BaseContent.WhiteTex);
                GUI.color = Color.white;
            }
            #endregion

            #region 雪花屏遮罩
            if (drawTick > 0)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.7f);
                GUI.DrawTexture(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), GDUtility.SnowScreenTex);
                GUI.color = Color.white;
            }
            #endregion

            #region 对话
            if (dialogTick >= 0 && !tmpStrings.NullOrEmpty())
            {
                for (int i = 0; i < tmpStrings.Count; i++)
                {
                    KeyValuePair<IntRange, string> pair = tmpStrings.ElementAt(i);
                    if (pair.Key.TrueIncludes((int)dialogTick))
                    {
                        float centerX = UI.screenWidth * 0.5f;
                        float centerY = UI.screenHeight * 0.7f;
                        float percentage = (dialogTick - pair.Key.TrueMin) / (float)(pair.Key.TrueMax - pair.Key.TrueMin);
                        GUI.color = new Color(1f, 1f, 1f, alphaCurve.Evaluate(percentage));
                        GUI.DrawTexture(new Rect(centerX - UI.screenWidth * 0.35f, centerY - 100f, UI.screenWidth * 0.7f, 200f), GDUtility.DialogBKGTex);
                        
                        GUI.color = new Color(1f, 47f / 51f, 4f / 255f, fontCurve.Evaluate(percentage));
                        Text.Anchor = TextAnchor.UpperCenter;
                        Text.Font = GameFont.Medium;
                        Vector2 vector = Text.CalcSize(DialogTitle);
                        Rect titleRect = new Rect(centerX - vector.x * 0.5f, centerY - Text.LineHeight - 25f, vector.x, Text.LineHeight);
                        Widgets.Label(titleRect, DialogTitle);

                        Text.Font = GameFont.Small;
                        TaggedString text = pair.Value.Translate(SteamFriends.GetPersonaName());
                        vector = Text.CalcSize(text);
                        float length = Math.Min(vector.x, UI.screenWidth * 0.5f);
                        Rect textRect = new Rect(centerX - length * 0.5f, centerY - 25f, length, Text.CalcHeight(text, UI.screenWidth * 0.5f));
                        GUI.color = new Color(1f, 1f, 1f, fontCurve.Evaluate(percentage));
                        Widgets.Label(textRect, text);

                        GUI.color = Color.white;
                        Text.Font = GameFont.Small;
                        Text.Anchor = TextAnchor.UpperLeft;
                    }
                }
            }
            #endregion

            #region 最终战血条
            if (pointer != null && pointer.Spawned && pointer.trueProgress > 0 && pointer.Boss != null && !pointer.Boss.beaten)
            {
                float centerX = UI.screenWidth * 0.5f;
                float centerY = UI.screenHeight * 0.15f;
                Rect BGRect = new Rect(centerX - UI.screenWidth * 0.35f, centerY - 60f, UI.screenWidth * 0.7f, 160f);
                GUI.color = new Color(1f, 1f, 1f, 0.7f);
                GUI.DrawTexture(BGRect, GDUtility.DialogBKGTex);

                float fillPercent = pointer.TruePercentage;
                Rect emptyRect = new Rect(BGRect.x + 15f, BGRect.y + 40f, BGRect.width - 30f, 40f);
                GUI.color = GDUtility.UnfilledColor;
                GUI.DrawTexture(emptyRect, GDUtility.WhiteBlockTex);
                if (fillPercent > 0.001f)
                {
                    Rect fillRect = new Rect(emptyRect.x + 5f, emptyRect.y + 5f, (emptyRect.width - 10f) * fillPercent, 30f);
                    GUI.color = GDUtility.EngagingColor;
                    GUI.DrawTexture(fillRect, GDUtility.WhiteBlockTex);
                    fillPercent = pointer.DisplayPercentage;
                    fillRect.width = (emptyRect.width - 10f) * fillPercent;
                    GUI.color = GDUtility.FilledColor;
                    GUI.DrawTexture(fillRect, GDUtility.WhiteBlockTex);

                    Rect iconRect = new Rect(emptyRect.x, emptyRect.y + emptyRect.height + 5f, 30f, 30f);
                    GUI.color = Faction.OfMechanoids.Color;
                    GUI.DrawTexture(iconRect, GDUtility.MechanoidsIconTex);

                    Rect tipRect = new Rect(emptyRect.x + 35f, iconRect.y, 500f, 30f);
                    GUI.color = pointer.ReadyToEnd ? Color.red : Color.white;
                    Text.Font = GameFont.Small;
                    Widgets.Label(tipRect, pointer.ReadyToEnd ? "GD.ReadyToEnd".Translate() : "GD.PsychicWaveEngaging".Translate(pointer.RemainingSeconds));

                    Rect blackIconRect = new Rect(emptyRect.x + emptyRect.width - 30f, emptyRect.y + emptyRect.height + 5f, 30f, 30f);
                    GUI.color = Color.white;
                    GUI.DrawTexture(blackIconRect, GDUtility.BlackMechanoidsIconTex);

                    int index = pointer.GetHealthCondition();
                    TaggedString numString = "\n+" + (pointer.GetHealthProgress(index) / (float)pointer.ProgressFull).ToStringPercent("F4") + "GD.Progress".Translate() + "/s";
                    TaggedString text = "GD.BlackApocritonCondition".Translate(GetHealthDesc(index).Colorize(GetHealthColor(index)), numString).Colorize(GUI.color);
                    float width = Text.CalcSize(text).x;
                    Rect blackTipRect = new Rect(emptyRect.x + emptyRect.width - 35f - width, blackIconRect.y, width, 50f);
                    Widgets.Label(blackTipRect, text);
                }
            }
            #endregion
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref drawTick, "drawTick");
            Scribe_Values.Look(ref whiteTick, "whiteTick");
            Scribe_Values.Look(ref sustainerTick, "sustainerTick");
            Scribe_Values.Look(ref dialogTick, "dialogTick", -1);
            Scribe_Values.Look(ref preventInteraction, "preventInteraction");
            Scribe_Values.Look(ref titleInt, "titleInt", "GD.BlackApocriton");
            Scribe_References.Look(ref pointer, "pointer");
            Scribe_Collections.Look(ref tmpStrings, "tmpStrings", LookMode.Value, LookMode.Value, ref tmpInt, ref tmpString, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (tmpStrings == null) tmpStrings = new Dictionary<IntRange, string>();
            }
        }

        private Color GetHealthColor(int index)
        {
            switch (index)
            {
                case 0:
                    return Color.white;
                case 1:
                    return Color.white;
                case 2:
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }

        private TaggedString GetHealthDesc(int index)
        {
            switch (index)
            {
                case 0:
                    return "GD.ConditionGood".Translate();
                case 1:
                    return "GD.ConditionNormal".Translate();
                case 2:
                    return "GD.ConditionBad".Translate();
                default:
                    return "error: boss not found";
            }
        }
    }
}
