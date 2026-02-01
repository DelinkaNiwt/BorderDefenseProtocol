using System;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace GD3
{
    [StaticConstructorOnStartup]
    public class Command_WhiteAction : Command
    {

        public new static readonly Texture2D BGTex = ContentFinder<Texture2D>.Get("UI/AbilityWhiteBG");

        public new static readonly Texture2D BGTexShrunk = ContentFinder<Texture2D>.Get("UI/AbilityWhiteBGShrunk");

        public Action action;

        public Action onHover;

        private Color? iconDrawColorOverride;

        public override Color IconDrawColor => iconDrawColorOverride ?? base.IconDrawColor;

        public override Texture2D BGTexture => BGTex;

        public override Texture2D BGTextureShrunk => BGTexShrunk;

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            action();
        }

        public override void GizmoUpdateOnMouseover()
        {
            if (onHover != null)
            {
                onHover();
            }
        }

        public void SetColorOverride(Color color)
        {
            iconDrawColorOverride = color;
        }
    }
}