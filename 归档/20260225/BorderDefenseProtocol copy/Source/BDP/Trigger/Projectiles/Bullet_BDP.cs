using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// BDP统一投射物宿主——继承原版Bullet，通过模块组合实现拖尾/引导/爆炸等功能。
    ///
    /// 架构v3（PMS重构）：
    ///   · 宿主本身是薄壳，不含业务逻辑
    ///   · 功能由IBDPProjectileModule模块提供（TrailModule/GuidedModule/ExplosionModule）
    ///   · 模块通过ThingDef.modExtensions上的配置类自动挂载（BDPModuleFactory）
    ///   · 无modExtensions时等同原版Bullet行为
    ///
    /// 生命周期分发：
    ///   SpawnSetup → modules.OnSpawn
    ///   TickInterval → base + modules.OnTick
    ///   ImpactSomething → modules.OnPreImpact（拦截检查）
    ///   Impact → modules.OnImpact（first-handler-wins）+ base.Impact fallback + modules.OnPostImpact
    /// </summary>
    public class Bullet_BDP : Bullet
    {
        /// <summary>已挂载的模块列表（按Priority升序排列）。</summary>
        private List<IBDPProjectileModule> modules = new List<IBDPProjectileModule>();

        /// <summary>获取指定类型的模块实例（供Verb层调用）。</summary>
        public T GetModule<T>() where T : class, IBDPProjectileModule
        {
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] is T typed) return typed;
            }
            return null;
        }

        /// <summary>
        /// 重定向飞行——由模块调用，将弹道从当前位置重定向到新目标。
        /// 重置origin/destination/ticksToImpact。
        /// </summary>
        public void RedirectFlight(Vector3 newOrigin, Vector3 newDestination)
        {
            origin = newOrigin;
            destination = newDestination;
            ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
            if (ticksToImpact < 1) ticksToImpact = 1;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // 首次生成时通过工厂创建模块（读档时模块由ExposeData恢复）
            if (!respawningAfterLoad)
            {
                modules = BDPModuleFactory.CreateModules(def);
                // 按Priority升序排列
                modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }

            // 通知所有模块
            for (int i = 0; i < modules.Count; i++)
                modules[i].OnSpawn(this);
        }

        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            // 分发OnTick
            for (int i = 0; i < modules.Count; i++)
                modules[i].OnTick(this);
        }

        /// <summary>
        /// 到达目标时调用——先让模块尝试拦截（引导飞行重定向），
        /// 无模块拦截时走原版ImpactSomething。
        /// </summary>
        protected override void ImpactSomething()
        {
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i].OnPreImpact(this))
                    return; // 模块已拦截（如引导飞行重定向）
            }
            base.ImpactSomething();
        }

        /// <summary>
        /// Impact处理——first-handler-wins模式。
        /// 有模块处理（如爆炸）则跳过base.Impact，否则走原版单体伤害。
        /// 最后通知所有模块OnPostImpact。
        /// </summary>
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            // first-handler-wins：第一个返回true的模块处理Impact
            bool handled = false;
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i].OnImpact(this, hitThing))
                {
                    handled = true;
                    break;
                }
            }

            // 无模块处理时走原版Impact
            if (!handled)
                base.Impact(hitThing, blockedByShield);

            // 通知所有模块（注意：爆炸模块可能已Destroy，需检查Spawned）
            if (Spawned || handled)
            {
                for (int i = 0; i < modules.Count; i++)
                    modules[i].OnPostImpact(this, hitThing);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref modules, "bdpModules", LookMode.Deep);
            if (modules == null)
                modules = new List<IBDPProjectileModule>();
        }
    }
}
