using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
    public class ReinforceMosquito : Skyfaller
    {
        private Vector3 cachedDrawPos;

        public Vector3 adjustPos;

        private MaterialPropertyBlock shadowPropertyBlock = new MaterialPropertyBlock();

        private Material cachedShadowMat;

        private Material ShadowMat
        {
            get
            {
                if (cachedShadowMat == null && !def.skyfaller.shadow.NullOrEmpty())
                {
                    cachedShadowMat = MaterialPool.MatFrom(def.skyfaller.shadow, ShaderDatabase.Transparent);
                }

                return cachedShadowMat;
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                return base.DrawPos + cachedDrawPos;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            cachedDrawPos = adjustPos + new Vector3(base.DrawPos.x, 0, 0);
        }

        protected override void Tick()
        {
            base.Tick();
            for (int i = 0; i < 2; i++)
            {
                ThrowFleck(DrawPos + GDUtility.RandomPointInCircle(0.1f) + new Vector3(0.4f, 0, 0), Map, FleckDefOf.MicroSparksFast, 0.25f, -90);
                ThrowFleck(DrawPos + GDUtility.RandomPointInCircle(0.3f) + new Vector3(0.4f, 0, 0), Map, FleckDefOf.FireGlow, 0.5f, -90);
            }
        }

        public void ThrowFleck(Vector3 loc, Map map, FleckDef def, float size, float angle)
        {
            if (loc.ShouldSpawnMotesAt(map))
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, def, Rand.Range(1.5f, 2.5f) * size);
                dataStatic.rotationRate = Rand.Range(-30f, 30f);
                dataStatic.velocityAngle = angle;
                dataStatic.velocitySpeed = Rand.Range(0.5f, 1.0f);
                dataStatic.solidTimeOverride = 0;
                map.flecks.CreateFleck(dataStatic);
            }
        }

        protected override void DrawDropSpotShadow()
        {
            Material shadowMaterial = ShadowMat;
            if (!(shadowMaterial == null))
            {
                DrawShadow(DrawPos + new Vector3(0, 0, -3.8f), base.Rotation, shadowMaterial, def.skyfaller.shadowSize);
            }
        }

        private void DrawShadow(Vector3 center, Rot4 rot, Material material, Vector2 shadowSize)
        {
            Vector3 pos = center;
            pos.y = AltitudeLayer.Shadows.AltitudeFor();
            Vector3 s = new Vector3(shadowSize.x, 1f, shadowSize.y);
            Color white = Color.white;

            shadowPropertyBlock.SetColor(ShaderPropertyIDs.Color, white);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(pos, rot.AsQuat, s);
            Graphics.DrawMesh(MeshPool.plane10Back, matrix, material, 0, null, 0, shadowPropertyBlock);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cachedDrawPos, "cachedDrawPos");
            Scribe_Values.Look(ref adjustPos, "adjustPos");
        }
    }
}
