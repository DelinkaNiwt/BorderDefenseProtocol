using System.Collections.Generic;
using BDP.Trigger.WeaponDraw;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody部分类 - 武器绘制模块
    ///
    /// 职责：聚合所有激活芯片的WeaponDrawEntry，供Patch_DrawEquipmentAiming_Weapon消费。
    ///
    /// ── 推导规则（以"右手South"为基准） ──
    ///
    /// South / North：
    ///   设 x = defaultOffset.x，z = defaultOffset.z
    ///   右South : offset=(x,  z+southZAdjust), flip=false
    ///   左South : offset=(-x, z+southZAdjust), flip=true
    ///   右North : offset=(-x, -z+northZAdjust), flip=true
    ///   左North : offset=(x,  -z+northZAdjust), flip=false
    ///   翻转规则：flip = isLeft XOR (facing==North)
    ///
    /// East / West：
    ///   设 bx=sideBaseX, dx=sideDeltaX, dz=sideDeltaZ
    ///   East前景手（右）: offset=(bx-dx, -dz), altitude=+alt
    ///   East背景手（左）: offset=(bx+dx, +dz), altitude=-alt
    ///   West背景手（右）: offset=(-bx-dx, +dz), altitude=-alt
    ///   West前景手（左）: offset=(-bx+dx, -dz), altitude=+alt
    ///   翻转：East/West 恒为 false
    /// </summary>
    public partial class CompTriggerBody
    {
        // 诊断日志：记录上次朝向
        private Rot4 lastDrawFacing = Rot4.Invalid;

        /// <summary>
        /// 获取当前帧所有激活芯片的武器绘制描述符。
        /// 由Patch_DrawEquipmentAiming_Weapon在每帧调用。
        /// Special槽位不参与自定义绘制。
        /// </summary>
        public IEnumerable<WeaponDrawEntry> GetActiveWeaponDrawEntries(Rot4 facing)
        {
            bool facingChanged = facing != lastDrawFacing;
            if (facingChanged)
            {
                lastDrawFacing = facing;
                Log.Message($"[BDP.WeaponDraw] ========== 朝向变化：{facing} ==========");
            }

            foreach (var slot in AllActiveSlots())
            {
                var chip = slot.loadedChip;
                if (chip == null) continue;
                if (slot.side == SlotSide.Special) continue;

                var config = chip.def.GetModExtension<WeaponDrawChipConfig>();
                if (config == null) continue;

                var graphic = config.GetGraphic(chip);
                if (graphic == null)
                {
                    Log.ErrorOnce(
                        $"[BDP.WeaponDraw] {chip.def.defName} GetGraphic返回null，texPath={config.graphicData?.texPath ?? "null"}",
                        chip.def.shortHash ^ 0xBD02);
                    continue;
                }

                bool isLeft = slot.side == SlotSide.LeftHand;
                WeaponDrawEntry entry = BuildEntry(config, chip, facing, isLeft);

                if (facingChanged)
                    Log.Message($"[BDP.WeaponDraw] {slot.side}: offset={entry.drawOffset}, altitude={entry.altitudeOffset}, flip={entry.flipHorizontal}");

                yield return entry;
            }
        }

        public static WeaponDrawEntry BuildEntry(
            WeaponDrawChipConfig config, Thing chip, Rot4 facing, bool isLeft)
        {
            float ox = config.defaultOffset.x;
            float oz = config.defaultOffset.z;
            float alt = config.defaultAltitudeOffset;

            Vector3 offset;
            float altitude;
            bool flip;

            if (facing == Rot4.South || facing == Rot4.North)
            {
                // ── South / North ──
                // 翻转规则：flip = isLeft XOR (facing==North)
                flip = isLeft ^ (facing == Rot4.North);

                // X：右手South为基准，左手取反；North整体X取反
                float signX = facing == Rot4.North ? -1f : 1f;
                float finalX = isLeft ? -ox * signX : ox * signX;

                // Z：North时取反，再叠加朝向微调
                float zAdj = facing == Rot4.South ? config.southZAdjust : config.northZAdjust;
                float finalZ = (facing == Rot4.North ? -oz : oz) + zAdj;

                offset   = new Vector3(finalX, 0f, finalZ);
                // North时武器在小人身后，altitude取反使其渲染在小人下层
                altitude = (facing == Rot4.North) ? -alt : alt;
            }
            else
            {
                // ── East / West ──
                // 前景手：East=右手，West=左手
                bool isFront = (facing == Rot4.East) ? !isLeft : isLeft;

                float bx = config.sideBaseX;
                float dx = config.sideDeltaX;
                float dz = config.sideDeltaZ;

                // East：bx为正；West：bx取反
                float signBase = facing == Rot4.East ? 1f : -1f;

                // 前景手靠近原点（-dx），背景手远离原点（+dx）
                float xDelta = isFront ? -dx : dx;
                float finalX = signBase * bx + xDelta;

                // 前景手靠近玩家（-dz），背景手远离玩家（+dz）
                float finalZ = isFront ? -dz : dz;

                offset   = new Vector3(finalX, 0f, finalZ);
                altitude = isFront ? alt : -alt;
                flip     = false;
            }

            return new WeaponDrawEntry
            {
                graphic        = config.GetGraphic(chip),
                glowGraphic    = config.GetGlowGraphic(chip),
                drawOffset     = offset,
                altitudeOffset = altitude,
                // 攻击时左手叠加偏移，使双刀不完全重合（待机时两手角度相同，保持X形）
                angle          = config.defaultAngle + (isLeft ? config.leftHandAngleOffset : 0f),
                flipHorizontal = flip,
            };
        }
    }
}
