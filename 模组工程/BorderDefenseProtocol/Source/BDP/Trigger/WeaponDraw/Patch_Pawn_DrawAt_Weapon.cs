using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger.WeaponDraw
{
    /// <summary>
    /// Prefix patch on PawnRenderUtility.DrawEquipmentAiming
    ///
    /// 逻辑：
    ///   · 有激活芯片武器条目时：返回 false，跳过原方法（触发体贴图不绘制），
    ///     改为绘制芯片武器贴图 + 发光层。
    ///   · 无激活条目时：返回 true，原方法正常绘制触发体贴图。
    ///
    /// 为什么挂在 DrawEquipmentAiming：
    ///   · 提供 aimAngle，可正确处理朝向旋转和左右翻转
    ///   · drawLoc 是武器实际持握位置（含后坐力偏移）
    ///   · 只在小人持武器时调用，取消征召后自动停止
    ///   · drawSize.y 必须为 0f（RimWorld 武器平铺在地面，Y 轴缩放=0）
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
    public static class Patch_DrawEquipmentAiming_Weapon
    {
        /// <returns>
        /// false = 跳过原方法（触发体贴图被芯片武器贴图替换）
        /// true  = 执行原方法（无激活芯片，正常绘制触发体贴图）
        /// </returns>
        // 用于抑制重复日志（每朝向只输出一次）
        private static float lastLoggedAimAngle = -1f;

        public static bool Prefix(Thing eq, Vector3 drawLoc, float aimAngle)
        {
            var triggerBody = eq.TryGetComp<CompTriggerBody>();
            if (triggerBody == null) return true; // 非触发体，不干预

            var pawn = eq.TryGetComp<CompEquippable>()?.PrimaryVerb?.CasterPawn;
            Rot4 facing = pawn?.Rotation ?? Rot4.South;

            // 收集激活条目（避免多次枚举）
            var entries = new List<WeaponDrawEntry>(triggerBody.GetActiveWeaponDrawEntries(facing));

            // [诊断] 每当 aimAngle 变化时输出一次
            if (Mathf.Abs(aimAngle - lastLoggedAimAngle) > 1f)
            {
                lastLoggedAimAngle = aimAngle;
                Log.Message($"[BDP.DrawPatch] Prefix 被调用: aimAngle={aimAngle:F1}, facing={facing}, entries={entries.Count}, drawLoc={drawLoc}");
            }

            if (entries.Count == 0) return true; // 无激活芯片，正常绘制触发体

            // 有激活芯片：绘制芯片武器贴图，跳过触发体贴图
            foreach (var entry in entries)
                DrawEntry(eq, drawLoc, aimAngle, entry);

            return false; // 阻止原方法执行
        }

        private static void DrawEntry(Thing triggerBody, Vector3 drawLoc, float aimAngle, WeaponDrawEntry entry)
        {
            // 复用 DrawEquipmentAiming 的角度和翻转逻辑
            float angle = aimAngle - 90f;
            Mesh mesh;
            if (aimAngle > 20f && aimAngle < 160f)
            {
                mesh = MeshPool.plane10;
                angle += triggerBody.def.equippedAngleOffset;
            }
            else if (aimAngle > 200f && aimAngle < 340f)
            {
                mesh = MeshPool.plane10Flip;
                angle -= 180f;
                angle -= triggerBody.def.equippedAngleOffset;
            }
            else
            {
                mesh = MeshPool.plane10;
                angle += triggerBody.def.equippedAngleOffset;
            }
            // 叠加芯片配置的额外旋转
            // plane10Flip 是水平镜像 mesh，旋转方向也随之反转，需对 entry.angle 取反
            angle += (mesh == MeshPool.plane10Flip) ? -entry.angle : entry.angle;

            // flipHorizontal：切换 mesh + 对 angle 取反
            // 几何关系：plane10Flip@(-A) == plane10@(A) 的水平镜像
            // 用 mesh 切换代替负 scaleX，避免负缩放触发 Unity 背面剔除导致不可见
            if (entry.flipHorizontal)
            {
                mesh  = (mesh == MeshPool.plane10) ? MeshPool.plane10Flip : MeshPool.plane10;
                angle = -angle;
            }

            angle %= 360f;

            // 位置 = 武器持握点 + 芯片偏移 + 高度偏移
            Vector3 pos = drawLoc + entry.drawOffset;
            pos.y += entry.altitudeOffset;

            // 武器本体层
            Vector2 sz = entry.graphic.drawSize;
            Graphics.DrawMesh(
                mesh,
                Matrix4x4.TRS(pos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(sz.x, 0f, sz.y)),
                entry.graphic.MatSingle,
                0
            );

            // 发光层（Y 略高于武器本体，避免 Z-fighting）
            if (entry.glowGraphic != null)
            {
                Vector3 glowPos = pos;
                glowPos.y += 0.001f;
                Vector2 gsz = entry.glowGraphic.drawSize;
                Graphics.DrawMesh(
                    mesh,
                    Matrix4x4.TRS(glowPos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(gsz.x, 0f, gsz.y)),
                    entry.glowGraphic.MatSingle,
                    0
                );
            }
        }
    }
}
