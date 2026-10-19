using System.Collections.Generic;
using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 远程间接攻击许可解析器看到的只读上下文。
    /// Core 只发布运行时事实，不要求解析器知道具体芯片或浮空业务。
    /// </summary>
    public sealed class IndirectRangedAttackPermissionContext
    {
        /// <summary>
        /// 由 Core 内部建立当前目标感知的许可上下文。
        /// </summary>
        internal IndirectRangedAttackPermissionContext(
            Pawn pawn,
            string resultId,
            IReadOnlyList<string> declaredPermissionKeys,
            LocalTargetInfo target,
            bool supportsIndirectTargeting,
            bool requiresDirectTargetLineOfSight)
        {
            Pawn = pawn;
            Map = pawn != null ? pawn.Map : null;
            ResultId = resultId;
            DeclaredPermissionKeys = declaredPermissionKeys;
            Target = target;
            SupportsIndirectTargeting = supportsIndirectTargeting;
            RequiresDirectTargetLineOfSight = requiresDirectTargetLineOfSight;
            PermissionContext = new MapPermissionContext(
                pawn,
                resultId,
                declaredPermissionKeys);
        }

        /// <summary>当前攻击 Pawn。</summary>
        public Pawn Pawn { get; private set; }

        /// <summary>当前攻击 Pawn 所在地图。</summary>
        public Map Map { get; private set; }

        /// <summary>当前正式表达结果标识。</summary>
        public string ResultId { get; private set; }

        /// <summary>当前正式表达显式声明的许可键。</summary>
        public IReadOnlyList<string> DeclaredPermissionKeys { get; private set; }

        /// <summary>当前具体目标。</summary>
        public LocalTargetInfo Target { get; private set; }

        /// <summary>当前正式结果是否具备远程间接攻击能力。</summary>
        public bool SupportsIndirectTargeting { get; private set; }

        /// <summary>当前正式结果是否声明射手到目标必须直射。</summary>
        public bool RequiresDirectTargetLineOfSight { get; private set; }

        /// <summary>当前 Pawn、地图和显式许可声明的统一读取上下文。</summary>
        public MapPermissionContext PermissionContext { get; private set; }

        /// <summary>
        /// 当前目标是否处于屋顶下。
        /// 这是目标后置事实，不是许可来源自行推断的状态。
        /// </summary>
        public bool TargetIsRoofed
        {
            get
            {
                return Target.IsValid
                    && Map != null
                    && Target.Cell.Roofed(Map);
            }
        }
    }
}
