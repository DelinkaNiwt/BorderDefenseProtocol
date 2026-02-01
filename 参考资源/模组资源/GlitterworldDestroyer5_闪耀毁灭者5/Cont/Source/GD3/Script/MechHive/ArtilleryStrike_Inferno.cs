using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    [StaticConstructorOnStartup]
    public class ArtilleryStrike_Inferno : Thing
    {
        private Effecter attachedEffecter;

        public int tickToImpact = -1;

        public static readonly int tickInBound = 1200;

        public static readonly int lifetime = 500;

        public static readonly float radius = 27.9f;

        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

        public int TicksPassed => Find.TickManager.TicksGame - spawnedTick;

        public int TicksLeft => tickInBound + lifetime - TicksPassed;

        private static readonly float width = 0.3f;

        private float BeamEndHeight => width * 0.5f;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            tickToImpact = Find.TickManager.TicksGame + tickInBound;
            Messages.Message("GD.ArtilleryStrikeIncoming".Translate(tickInBound.ToStringSecondsFromTicks()), this, MessageTypeDefOf.NeutralEvent);
        }

        protected override void Tick()
        {
            base.Tick();
            if (Spawned)
            {
                if (attachedEffecter == null)
                {
                    attachedEffecter = GDDefOf.GDReinforceFlareAttached.SpawnAttached(this, MapHeld);
                    attachedEffecter.scale = 0.3f;
                    attachedEffecter.offset = new Vector3(0, 0, -0.2f);
                }
                attachedEffecter?.EffectTick(this, this);
            }
            else
            {
                attachedEffecter?.Cleanup();
                attachedEffecter = null;
            }

            int tick = Find.TickManager.TicksGame;
            if (tick > tickToImpact)
            {
                if (this.IsHashIntervalTick(50))
                {
                    for (int i = 0; i < Rand.Range(6, 8); i++)
                    {
                        IntVec3 pos = GenRadial.RadialCellsAround(Position, radius, true).RandomElement();
                        Thing shell = ThingMaker.MakeThing(GDDefOf.GD_InfernoArtilleryShell);
                        GenPlace.TryPlaceThing(shell, pos, Map, ThingPlaceMode.Direct);
                    }
                    for (int i = 0; i < Rand.Range(2, 3); i++)
                    {
                        IntVec3 posL = GenRadial.RadialCellsAround(Position, radius, true).RandomElement();
                        Thing shellL = ThingMaker.MakeThing(GDDefOf.GD_InfernoArtilleryShellLarge);
                        GenPlace.TryPlaceThing(shellL, posL, Map, ThingPlaceMode.Direct);
                    }
                }
                if (tick - tickToImpact > lifetime)
                {
                    Destroy();
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (!Destroyed)
            {
                Vector3 drawPos = drawLoc;
                float num = ((float)Map.Size.z - drawPos.z) * 1.4142135f;
                Vector3 vector = Vector3Utility.FromAngleFlat(-90f);
                Vector3 vector2 = drawPos + vector * num * 0.5f;
                vector2.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                float num2 = Mathf.Min((float)TicksPassed / 10f, 1f);
                Vector3 vector3 = vector * ((1f - num2) * num);
                float num3 = 0.975f + Mathf.Sin((float)TicksPassed * 0.3f) * 0.025f;
                if (TicksLeft < 20)
                {
                    num3 *= (float)TicksLeft / (float)20;
                }
                Color color = new Color(255, 20, 20, 242);
                color.a *= num3;
                MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector2 + vector * BeamEndHeight * 0.5f + vector3, Quaternion.Euler(0f, 0f, 0f), new Vector3(width, 1f, num));
                Graphics.DrawMesh(MeshPool.plane10, matrix, BeamMat, 0, null, 0, MatPropertyBlock);
                Vector3 pos = drawPos + vector3;
                pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                Matrix4x4 matrix2 = default(Matrix4x4);
                matrix2.SetTRS(pos, Quaternion.Euler(0f, 0f, 0f), new Vector3(width, 1f, BeamEndHeight));
                Graphics.DrawMesh(MeshPool.plane10, matrix2, BeamEndMat, 0, null, 0, MatPropertyBlock);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickToImpact, "tickToImpact", -1);
        }
    }
}
