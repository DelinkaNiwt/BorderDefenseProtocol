using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.AreaExplosion
{
    /// <summary>
    /// 范围爆炸远程模块。
    /// </summary>
    /// <remarks>
    /// 参与两个阶段：
    ///
    /// 1. Preview（预览）阶段 — 绘制爆炸覆盖范围预览。
    /// 2. Impact（终结结算）阶段 — 在实际落点提交范围爆炸计划。
    ///
    /// 真实爆炸执行回落到原版 `GenExplosion.DoExplosion`。
    /// </remarks>
    public sealed class AreaExplosionModule :
        IRangedAttackModuleRuntime,
        IPreviewStageModule,
        IImpactStageModule
    {
        /// <summary>
        /// 当前模块固定使用的预览颜色。
        /// </summary>
        private static readonly Color DefaultAreaColor = new Color(0.85f, 0.85f, 0.85f, 1f);

        /// <summary>
        /// 当前运行时实例绑定的配置快照。
        /// </summary>
        private AreaExplosionConfig config;

        /// <summary>
        /// 初始化当前模块实例。
        /// </summary>
        /// <param name="context">模块初始化上下文。</param>
        void IRangedAttackModuleRuntime.Initialize(RangedAttackModuleRuntimeContext context)
        {
            config = ResolveConfigSnapshot(context);
        }

        /// <summary>
        /// 参与瞄准预览阶段：绘制爆炸覆盖范围。
        /// </summary>
        /// <param name="record">当前预览记录。</param>
        void IPreviewStageModule.Contribute(PreviewRecord record)
        {
            if (record == null || !record.Target.IsValid)
            {
                return;
            }

            float radius = ResolveExplosionRadius();
            if (!(radius > 0f))
            {
                return;
            }

            Map map = record.Pawn != null
                ? record.Pawn.MapHeld
                : Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            List<IntVec3> previewCells = BuildPreviewCells(record.Target.Cell, map, radius);
            if (previewCells == null || previewCells.Count == 0)
            {
                return;
            }

            record.UseVanillaFieldRadius = false;
            record.DrawItems.Add(new PreviewDrawItem
            {
                Kind = PreviewDrawItemKind.CellGroup,
                Color = DefaultAreaColor
            });

            PreviewDrawItem drawItem = record.DrawItems[record.DrawItems.Count - 1];
            for (int i = 0; i < previewCells.Count; i++)
            {
                drawItem.Cells.Add(previewCells[i]);
            }
        }

        /// <summary>
        /// 参与命中后终结结算阶段：在实际落点提交范围爆炸计划。
        /// </summary>
        /// <param name="context">Impact 阶段上下文。</param>
        /// <param name="contribution">Impact 阶段贡献。</param>
        void IImpactStageModule.Contribute(in ImpactStageContext context, ImpactContribution contribution)
        {
            if (contribution == null || context.Projectile == null)
            {
                return;
            }

            float radius = ResolveExplosionRadius();
            if (!(radius > 0f))
            {
                return;
            }

            // 默认抑制基线单体命中，避免同一发同时出现单体子弹伤害与爆炸伤害叠加。
            contribution.SuppressBaselineImpact = config != null && config.SuppressBaselineImpact;
            contribution.ProducesAttackTargetEvents = true;
            contribution.HasAreaEffect = true;
            contribution.OverrideAreaEffect = new AreaEffectPlan
            {
                DamageDef = ResolveDamageDef(context.Projectile),
                Radius = radius,
                DamageAmount = ResolveDamageAmount(context.Projectile),
                ArmorPenetration = ResolveArmorPenetration(context.Projectile),
                Center = context.Projectile.Position,
                Instigator = context.Launcher,
                Weapon = context.SourceThing,
                SemanticContext = context.SemanticContext
            };
        }

        /// <summary>
        /// 读取当前配置快照。
        /// </summary>
        /// <param name="context">模块初始化上下文。</param>
        /// <returns>当前实例独享的配置副本。</returns>
        private static AreaExplosionConfig ResolveConfigSnapshot(RangedAttackModuleRuntimeContext context)
        {
            if (context != null && context.Config is AreaExplosionConfig typedConfig)
            {
                return typedConfig.CloneTyped();
            }

            return new AreaExplosionConfig();
        }

        /// <summary>
        /// 读取当前模块生效的爆炸半径。
        /// </summary>
        /// <returns>大于 0 时表示有效半径。</returns>
        private float ResolveExplosionRadius()
        {
            return config != null ? config.ExplosionRadius : 0f;
        }

        /// <summary>
        /// 解析当前爆炸使用的伤害类型。
        /// </summary>
        /// <param name="projectile">当前投射物。</param>
        /// <returns>配置优先，否则回退投射物伤害类型，再回退原版 Bomb。</returns>
        private DamageDef ResolveDamageDef(Projectile projectile)
        {
            if (config != null && config.DamageDef != null)
            {
                return config.DamageDef;
            }

            if (projectile != null && projectile.DamageDef != null)
            {
                return projectile.DamageDef;
            }

            return DamageDefOf.Bomb;
        }

        /// <summary>
        /// 解析当前爆炸使用的伤害量。
        /// </summary>
        /// <param name="projectile">当前投射物。</param>
        /// <returns>配置有效值优先，否则回退投射物伤害量。</returns>
        private float ResolveDamageAmount(Projectile projectile)
        {
            if (config != null && config.DamageAmount > 0f)
            {
                return config.DamageAmount;
            }

            return projectile != null ? projectile.DamageAmount : 0f;
        }

        /// <summary>
        /// 解析当前爆炸使用的护甲穿透。
        /// </summary>
        /// <param name="projectile">当前投射物。</param>
        /// <returns>配置有效值优先，否则回退投射物护甲穿透。</returns>
        private float ResolveArmorPenetration(Projectile projectile)
        {
            if (config != null && config.ArmorPenetration >= 0f)
            {
                return config.ArmorPenetration;
            }

            return projectile != null ? projectile.ArmorPenetration : 0f;
        }

        /// <summary>
        /// 生成爆炸覆盖格预览。
        /// </summary>
        /// <param name="center">当前瞄准中心格。</param>
        /// <param name="map">当前地图。</param>
        /// <param name="radius">当前爆炸半径。</param>
        /// <returns>用于预览的覆盖格集合。</returns>
        private List<IntVec3> BuildPreviewCells(IntVec3 center, Map map, float radius)
        {
            if (map == null || !(radius > 0f))
            {
                return new List<IntVec3>();
            }

            DamageDef previewDamageDef = config != null && config.DamageDef != null
                ? config.DamageDef
                : DamageDefOf.Bomb;
            DamageWorker worker = previewDamageDef != null ? previewDamageDef.Worker : null;
            if (worker == null)
            {
                return new List<IntVec3>();
            }

            List<IntVec3> result = new List<IntVec3>();
            foreach (IntVec3 cell in worker.ExplosionCellsToHit(center, map, radius))
            {
                result.Add(cell);
            }

            return result;
        }
    }

    /// <summary>
    /// 范围爆炸模块配置。
    /// </summary>
    public sealed class AreaExplosionConfig : RangedModuleConfigNode
    {
        /// <summary>
        /// 当前爆炸半径。必须大于 0 才会真正生成爆炸与预览。
        /// </summary>
        public float ExplosionRadius;

        /// <summary>
        /// 当前爆炸使用的伤害类型。留空时回退到当前投射物伤害类型。
        /// </summary>
        public DamageDef DamageDef;

        /// <summary>
        /// 当前爆炸使用的伤害量。小于等于 0 时回退到当前投射物伤害量。
        /// </summary>
        public float DamageAmount;

        /// <summary>
        /// 当前爆炸使用的护甲穿透。小于 0 时回退到当前投射物护甲穿透。
        /// </summary>
        public float ArmorPenetration = -1f;

        /// <summary>
        /// 是否抑制基线单体命中。
        /// 默认打开，避免同一发弹体同时产生单体直击伤害与范围爆炸伤害叠加。
        /// </summary>
        public bool SuppressBaselineImpact = true;

        /// <summary>
        /// 生成一份强类型配置副本。
        /// </summary>
        /// <returns>当前配置的独立深复制。</returns>
        public AreaExplosionConfig CloneTyped()
        {
            return new AreaExplosionConfig
            {
                ExplosionRadius = ExplosionRadius,
                DamageDef = DamageDef,
                DamageAmount = DamageAmount,
                ArmorPenetration = ArmorPenetration,
                SuppressBaselineImpact = SuppressBaselineImpact
            };
        }

        /// <summary>
        /// 生成配置副本。
        /// </summary>
        /// <returns>当前配置的独立深复制。</returns>
        public override RangedModuleConfigNode Clone()
        {
            return CloneTyped();
        }
    }
}
