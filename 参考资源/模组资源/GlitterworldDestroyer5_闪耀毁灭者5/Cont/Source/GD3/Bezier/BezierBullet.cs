using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GD3
{
    public class Mst_BeziertBullet : BezierProjectiles
    {
        //modExt初始化标志。
        private bool extensionLoaded = false;
        //命中护盾是否销毁自身
        private bool destroyOnShieldHit = true;
        public override bool AnimalsFleeImpact => true;
        // 懒加载配置的方法，把 XML 中的扩展数据一次性读取
        private void LoadExtension()
        {
            if (!extensionLoaded)
            {
                ModExt_Mst_BezierBullet modExtension = def.GetModExtension<ModExt_Mst_BezierBullet>();
                destroyOnShieldHit = modExtension?.destroyOnShieldHit ?? true;

                extensionLoaded = true;
            }
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (Destroyed || Map == null)
            {
                return;
            }

            // 加载配置
            LoadExtension();

            // 被护盾挡下，子弹看情况销毁
            if (blockedByShield)
            {
                if (destroyOnShieldHit)
                {
                    Destroy();
                    return;
                }
            }

            if (hitThing == null)
            {
                // 护盾挡弹且不销毁，直接跳过后续销毁逻辑
                if (blockedByShield && destroyOnShieldHit == false)
                {
                    return;
                }

                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(Position, Map));
                if (Position.GetTerrain(Map).takeSplashes)
                    FleckMaker.WaterSplash(ExactPosition, Map, Mathf.Sqrt(DamageAmount), 4f);
                else
                    FleckMaker.Static(ExactPosition, Map, FleckDefOf.ShotHit_Dirt);

                Destroy();
                return;
            }
            Map map = base.Map;
            IntVec3 position = base.Position;
            base.Impact(hitThing, blockedByShield);
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            NotifyImpact(hitThing, map, position);

            if (hitThing != null)
            {
                bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;
                DamageInfo dinfo = new DamageInfo(base.DamageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                dinfo.SetWeaponQuality(equipmentQuality);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                if (hitThing is Pawn impactedPawn && impactedPawn.stances?.stagger != null)
                {
                    impactedPawn.stances.stagger.StaggerFor(95); // 让目标停顿
                }
                if (base.ExtraDamages == null)
                {
                    return;
                }
                {
                    foreach (ExtraDamage extraDamage in base.ExtraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                    return;
                }
            }
            if (!blockedByShield)
            {
                if (base.Position.GetTerrain(map).takeSplashes)
                {
                    FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt(DamageAmount) * 1f, 4f);
                }
                else
                {
                    FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt);
                }
            }
            if (Rand.Chance(base.DamageDef.igniteCellChance))
            {
                FireUtility.TryStartFireIn(base.Position, map, Rand.Range(0.55f, 0.85f), launcher);
            }
        }

        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            BulletImpactData impactData = new BulletImpactData
            {
                bullet = null, // 或者创建一个继承 Bullet 的 dummyBullet 实例
                hitThing = hitThing,
                impactPosition = position
            };
            hitThing?.Notify_BulletImpactNearby(impactData);
            int num = 9;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (!c.InBounds(map))
                {
                    continue;
                }
                List<Thing> thingList = c.GetThingList(map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (thingList[j] != hitThing)
                    {
                        thingList[j].Notify_BulletImpactNearby(impactData);
                    }
                }
            }
        }
    }
}
