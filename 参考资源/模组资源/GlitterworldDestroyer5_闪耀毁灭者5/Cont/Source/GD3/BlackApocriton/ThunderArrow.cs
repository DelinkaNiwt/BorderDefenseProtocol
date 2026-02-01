using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    public class ThunderArrow : ThingWithComps
    {
        public int timeToLaunch = 0;

        public int Time => Find.TickManager.TicksGame - timeToLaunch;

        public Thing launcher;

        public Thing instigator;

        public bool instant;

        public bool dontPlaySound;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (instant)
            {
                if ((!(launcher is Pawn p) || !p.DeadOrDowned) && launcher.MapHeld == MapHeld)
                {
                    Projectile proj = (Projectile)GenSpawn.Spawn(GDDefOf.Bullet_RedThunderArrow, launcher.PositionHeld, launcher.MapHeld);
                    proj.Launch(instigator ?? launcher, launcher.TrueCenter(), this, this, ProjectileHitFlags.IntendedTarget, true, proj);
                    if (!dontPlaySound) GDDefOf.ThunderArrowShoot.PlayOneShot(launcher);
                }
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (Time == 0 && !instant)
            {
                if ((!(launcher is Pawn p) || !p.DeadOrDowned) && launcher.MapHeld == MapHeld)
                {
                    Projectile proj = (Projectile)GenSpawn.Spawn(GDDefOf.Bullet_RedThunderArrow, launcher.PositionHeld, launcher.MapHeld);
                    proj.Launch(instigator ?? launcher, launcher.TrueCenter(), this, this, ProjectileHitFlags.IntendedTarget, true, proj);
                    if (!dontPlaySound) GDDefOf.ThunderArrowShoot.PlayOneShot(launcher);
                }
                else Destroy();
            }
            if (Time > 10)
            {
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (Time < 10 && Time > -180 && !instant)
            {
                Vector3 drawPos = drawLoc;
                drawPos.y = AltitudeLayer.MoteLow.AltitudeFor();
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(1.2f, 1.0f, 1.2f));
                float alpha = Time < 0 ? 0.8f : 0.8f * Math.Max(0, 1 - Time / 10f); 
                Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("UI/Symbols/WarningIcon", ShaderDatabase.MoteGlow, new Color(1, 1, 1, alpha)), 0);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref timeToLaunch, "timeToLaunch");
            Scribe_Values.Look(ref instant, "instant");
            Scribe_Values.Look(ref dontPlaySound, "dontPlaySound");
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_References.Look(ref instigator, "instigator");
        }
    }
}
