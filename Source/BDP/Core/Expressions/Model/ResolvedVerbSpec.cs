using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// BDP 自己拥有的正式运行时 Verb 规格。
    /// 它只收口运行时真正需要消费的战斗字段，不再把可变运行时真值寄托在 Verse 私有字段上。
    /// </summary>
    public sealed class ResolvedVerbSpec
    {
        /// <summary>
        /// 当前规格的基准宿主 VerbProps 模板。
        /// 它只服务边界层生成宿主表面，不再作为运行时真值源。
        /// </summary>
        public VerbProperties SurfaceTemplate { get; set; }

        /// <summary>
        /// 当前规格声明的 Verb 类型。
        /// </summary>
        public Type VerbClass { get; set; }

        /// <summary>
        /// 当前规格的有效射程。
        /// </summary>
        public float Range { get; set; }

        /// <summary>
        /// 当前规格的最小射程。
        /// </summary>
        public float MinRange { get; set; }

        /// <summary>
        /// 当前规格的暖机时长。
        /// </summary>
        public float WarmupTime { get; set; }

        /// <summary>
        /// 当前规格声明的 burst 发射数。
        /// </summary>
        public int BurstShotCount { get; set; }

        /// <summary>
        /// 当前规格声明的 burst 内发射间隔。
        /// </summary>
        public int TicksBetweenBurstShots { get; set; }

        /// <summary>
        /// 当前规格的强制偏移半径。
        /// 它作为 BDP 自己的正式真值存在，不再反射写回 Verse 私有字段。
        /// </summary>
        public float ForcedMissRadius { get; set; }

        /// <summary>
        /// 当前规格的触距精度。
        /// </summary>
        public float AccuracyTouch { get; set; }

        /// <summary>
        /// 当前规格的近距离精度。
        /// </summary>
        public float AccuracyShort { get; set; }

        /// <summary>
        /// 当前规格的中距离精度。
        /// </summary>
        public float AccuracyMedium { get; set; }

        /// <summary>
        /// 当前规格的远距离精度。
        /// </summary>
        public float AccuracyLong { get; set; }

        /// <summary>
        /// 当前规格的冷却时间。
        /// </summary>
        public float DefaultCooldownTime { get; set; }

        /// <summary>
        /// 当前规格是否要求与目标保持直射合法。
        /// 它是 dual 分侧裁定时必须读取的正式真值。
        /// </summary>
        public bool RequireLineOfSight { get; set; }

        /// <summary>
        /// 当前规格是否必须满足射手到语义目标的直射 LOS。
        /// 它只服务 dual 分侧准入，不代表攻击内部不会另行使用 LOS。
        /// </summary>
        public bool RequiresDirectTargetLineOfSight { get; set; }

        /// <summary>
        /// 当前规格在 burst 过程中丢失直射时是否应停止后续发射。
        /// 它保留作者声明，供下游运行时协议显式消费。
        /// </summary>
        public bool StopBurstWithoutLos { get; set; }

        /// <summary>
        /// 当前规格使用的默认投射物。
        /// </summary>
        public ThingDef ProjectileDef { get; set; }

        /// <summary>
        /// 当前规格的中性投射物属性覆盖。
        /// 为 null = 不覆盖，沿用投射物 Def 原生属性。
        /// </summary>
        public ProjectileOverrides ProjectileOverrides { get; set; }

        /// <summary>
        /// 当前规格绑定的近战 Tool。
        /// </summary>
        public Tool Tool { get; set; }

        /// <summary>
        /// 当前规格保留的全部近战 Tool。
        /// </summary>
        public IReadOnlyList<Tool> DeclaredTools { get; set; }

        /// <summary>
        /// 当前规格保留的全部近战运行时表面。
        /// </summary>
        public IReadOnlyList<MeleeToolSurface> DeclaredMeleeToolSurfaces { get; set; }

        /// <summary>
        /// 当前规格绑定的 Maneuver。
        /// </summary>
        public ManeuverDef Maneuver { get; set; }
    }
}
