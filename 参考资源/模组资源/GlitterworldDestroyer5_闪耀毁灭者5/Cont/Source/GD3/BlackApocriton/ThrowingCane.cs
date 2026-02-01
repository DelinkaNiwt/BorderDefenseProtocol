using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    public class ThrowingCane : ThingWithComps
    {
        public int time = 0;

        public float Speed => Math.Min(1, (150 - time) / 120f);

        public Vector3 offset;

        public float angle;

        public float pitch = 1f;

        public FleckDef fleck;

        protected override void Tick()
        {
            base.Tick();
            time++;
            offset += new Vector3(0, 0, Speed * 1 / 60f) * 10;
            angle += 6 * Speed;
            if (time > 120)
            {
                if (PositionHeld.ShouldSpawnMotesAt(MapHeld))
                {
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(DrawPos + offset, MapHeld, fleck);
                    dataStatic.scale = 3f;
                    MapHeld.flecks.CreateFleck(dataStatic);
                }
                SoundInfo info = SoundInfo.OnCamera();
                info.pitchFactor = pitch;
                GDDefOf.ThunderArrowWarning.PlayOneShot(info);
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawPos = drawLoc;
            drawPos += offset;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(2f, 1.0f, 2f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("BlackCane", ShaderDatabase.Transparent, Color.white), 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref time, "time");
            Scribe_Values.Look(ref offset, "offset");
            Scribe_Values.Look(ref angle, "angle");
            Scribe_Values.Look(ref pitch, "pitch", 1f);
            Scribe_Defs.Look(ref fleck, "fleck");
        }
    }
}
